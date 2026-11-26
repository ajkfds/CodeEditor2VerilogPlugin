using Avalonia.Input;
using CodeEditor2.Views;
using pluginVerilog.CoreBridge;
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// Runs a <see cref="SnippetProvider"/> snippet in the desktop editor.
    ///
    /// <para>
    /// The snippet logic lives in the snippet classes of this folder
    /// (<see cref="AlwaysFFSnippet.Build"/> and friends, UI free) so the
    /// desktop editor and the VS Code extension insert exactly the same text
    /// from one implementation. This class is only the <b>host</b>: it inserts
    /// <see cref="SnippetAdapter.SnippetEntry.PlainText"/>, turns
    /// <see cref="SnippetAdapter.SnippetEntry.Fields"/> into editor highlights
    /// and lets the user tab through them.
    /// </para>
    ///
    /// <para>
    /// Keys (same as the original hand written snippets): Enter / Down confirm
    /// the field and move on, Escape / Up abort, and moving the caret to
    /// another line aborts (the user went somewhere else).
    /// </para>
    ///
    /// <para>
    /// A field with several candidates opens the editor's selection popup; a
    /// <b>mirror</b> field (<see cref="SnippetField.IsMirror"/>) is not asked
    /// at all - its text is copied from the field it mirrors once the user
    /// confirmed it, which is what the old alwaysFF background task did.
    /// </para>
    /// </summary>
    public class SnippetRunner : CodeEditor2.Snippets.InteractiveSnippet
    {
        private readonly string kind;
        private string? argument;

        private CodeEditor2.CodeEditor.CodeDocument? document;
        private SnippetProvider.SnippetEntry? entry;
        private int? checkLine;

        /// <summary>
        /// Range of the field currently being asked, or -1 when none is.
        /// <see cref="Caret_PositionChanged"/> accepts a caret anywhere inside
        /// it, because the snippet itself parks the caret on the highlight start
        /// and that must not be mistaken for the user leaving the field.
        /// </summary>
        private int currentFieldStart = -1;
        private int currentFieldLast = -1;

        private TaskCompletionSource<string>? fieldDone;
        private TaskCompletionSource<string>? choiceMade;

        /// <summary>
        /// An Enter that arrived while no field was waiting.
        ///
        /// <para>
        /// The wait source is only created after the highlight has been selected
        /// and after the candidate popup has closed, so an Enter pressed in that
        /// window used to be swallowed: the caret sat on the highlight, the key
        /// was marked handled (no newline) and nothing advanced until the user
        /// pressed Enter a second time. Latching the request makes every Enter
        /// advance, whatever the timing.
        /// </para>
        /// </summary>
        private bool confirmPending;

        public SnippetRunner(string kind, string? argument = null) : base(kind)
        {
            this.kind = kind;
            this.argument = argument;
        }

        public override async Task ApplyAsync()
        {
            CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (file == null) return;
            document = file.CodeDocument;
            if (document == null) return;

            SnippetProvider.SnippetEntry? resolved = await ResolveEntryAsync(file);
            if (resolved == null) return;
            entry = resolved;

            int caret = document.CaretIndex;
            string indent = GetIndent(document, entry.Kind == SnippetProvider.SnippetKind.Replace ? entry.Start : caret);

            if (entry.Kind == SnippetProvider.SnippetKind.Replace)
            {
                document.Replace(entry.Start, entry.End - entry.Start, 0, entry.PlainText);
            }
            else
            {
                document.Replace(caret, 0, 0, entry.PlainText);
            }

            if (!entry.HasFields)
            {
                CodeEditor2.Controller.CodeEditor.RequestReparsePost();
                return;
            }

            // One highlight per field, in document order. Mirrors get a
            // highlight too so their position is tracked, but they are never
            // asked - their text is copied from the field they mirror.
            CodeEditor2.Controller.CodeEditor.PostClearHighlight();
            int fieldStart = entry.Kind == SnippetProvider.SnippetKind.Replace ? entry.Start : caret;
            List<int> askOrder = new();
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                SnippetField field = entry.Fields[i];
                CodeEditor2.Controller.CodeEditor.AppendHighlight(
                    fieldStart + field.Start, fieldStart + field.Start + field.Length);
                if (!field.IsMirror) askOrder.Add(i);
            }

            await base.ApplyAsync();
            if (askOrder.Count == 0)
            {
                CodeEditor2.Controller.CodeEditor.RequestReparsePost();
                return;
            }

            await RunAsync(askOrder);
        }

        /// <summary>
        /// Asks the adapter for the snippet. When it answers with candidates
        /// (e.g. which module to instantiate) the user picks one here and the
        /// adapter is asked again - the same two step flow the VS Code
        /// extension uses (<c>showQuickPick</c> then re-request).
        /// </summary>
        private async Task<SnippetProvider.SnippetEntry?> ResolveEntryAsync(CodeEditor2.Data.TextFile file)
        {
            if (document == null) return null;
            if (file is not Data.IVerilogRelatedFile verilogFile) return null;
            Verilog.ParsedDocument? parsedDocument = verilogFile.VerilogParsedDocument;
            if (parsedDocument == null) return null;

            int caret = document.CaretIndex;
            int selectionStart = document.SelectionStart;
            int selectionEnd = document.SelectionLast;

            SnippetProvider.SnippetResult result = SnippetProvider.GetSnippets(
                verilogFile, parsedDocument, caret, kind, argument, selectionStart, selectionEnd);

            if (result.Snippets.Count == 0 && result.Candidates.Count > 0)
            {
                string? picked = await PickCandidateAsync(result.Candidates);
                if (string.IsNullOrEmpty(picked)) return null;
                argument = picked;
                result = SnippetProvider.GetSnippets(
                    verilogFile, parsedDocument, caret, kind, argument, selectionStart, selectionEnd);
            }

            if (result.Snippets.Count == 0)
            {
                if (!string.IsNullOrEmpty(result.Message))
                {
                    CodeEditor2.Controller.AppendLog(kind + ": " + result.Message, Avalonia.Media.Colors.Yellow);
                }
                return null;
            }
            return result.Snippets[0];
        }

        /// <summary>Shows the candidate list in the editor's popup and waits.</summary>
        private async Task<string?> PickCandidateAsync(List<string> candidates)
        {
            choiceMade = new TaskCompletionSource<string>();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items = new();
                foreach (string candidate in candidates)
                {
                    items.Add(new CandidateToolItem(candidate, choiceMade));
                }
                CodeEditor2.Controller.CodeEditor.ForceOpenCustomSelection(items);
            });
            string picked = await choiceMade.Task;
            choiceMade = null;
            return string.IsNullOrEmpty(picked) ? null : picked;
        }

        // ---- background walk over the fields --------------------------------

        private async Task RunAsync(List<int> askOrder)
        {
            try
            {
                foreach (int fieldIndex in askOrder)
                {
                    SnippetField field = entry!.Fields[fieldIndex];

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        CodeEditor2.Controller.CodeEditor.SelectHighlight(fieldIndex);
                    });

                    // A field with several candidates asks before the user types.
                    if (field.Candidates.Count > 1)
                    {
                        string? picked = await PickCandidateAsync(new List<string>(field.Candidates));
                        if (!string.IsNullOrEmpty(picked))
                        {
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                CodeEditor2.Controller.CodeEditor.GetHighlightPosition(fieldIndex, out int start, out int last);
                                if (start >= 0 && last >= start)
                                {
                                    document?.Replace(start, last - start, 0, picked);
                                }
                            });
                        }
                        // Choosing from the selector IS the confirmation of this
                        // field: the user already pressed Enter (or clicked) in
                        // the popup, so asking for a second Enter here made the
                        // snippet feel stuck. Moving straight on matches the old
                        // alwaysFF, which received AfterAutoCompleteHandled from
                        // the popup and treated it as "moveNext".
                        //
                        // An abort (Escape) still ends the snippet.
                        if (picked == null) return;
                        await SyncMirrorsAsync();
                        continue;
                    }

                    string? result = await WaitForFieldAsync(fieldIndex);
                    if (result != "moveNext") return;

                    // Keep the mirrored fields in step as soon as this field is
                    // confirmed (the old alwaysFF copied the reset this way).
                    await SyncMirrorsAsync();
                }

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    CodeEditor2.Controller.CodeEditor.RequestReparsePost();
                });
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog("##Exception " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                await CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippetAsync();
            }
        }

        /// <summary>
        /// Waits for the user to confirm the current field (Enter / Down / Tab)
        /// or abort it (Escape / Up).
        ///
        /// <para>
        /// Returns <c>moveNext</c> or <c>abort</c>. An Enter that arrived
        /// <b>before</b> the wait started is honoured instead of being lost:
        /// <see cref="confirmPending"/> latches it, because the field's
        /// highlight is already selected at that point (the caret sits on the
        /// highlight start) and the key is marked handled, so without the latch
        /// the user had to press Enter twice.
        /// </para>
        /// </summary>
        private async Task<string> WaitForFieldAsync(int fieldIndex)
        {
            fieldDone = new TaskCompletionSource<string>();

            // Take the line from the highlight itself, not from
            // document.SelectionStart: an empty highlight (a blank field such as
            // alwaysFF's branches) produces AvaloniaEdit's EmptySelection, which
            // does not update CodeDocument._selectionStart, so SelectionStart
            // keeps the PREVIOUS field's offset. The resulting wrong checkLine
            // made the snippet abort on the next caret movement.
            // GetHighlightPosition asserts the UI thread (it reads the text
            // view's renderer state), and this runs on the background walk, so
            // marshal it.
            int highlightStart = -1;
            int highlightLast = -1;
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                CodeEditor2.Controller.CodeEditor.GetHighlightPosition(
                    fieldIndex, out highlightStart, out highlightLast);
            });
            currentFieldStart = highlightStart;
            currentFieldLast = highlightLast;
            checkLine = document == null || highlightStart < 0
                ? null
                : document.GetLineAt(highlightStart);

            if (confirmPending)
            {
                confirmPending = false;
                checkLine = null;
                currentFieldStart = -1;
                currentFieldLast = -1;
                fieldDone = null;
                return "moveNext";
            }
            string result = await fieldDone.Task;
            checkLine = null;
            currentFieldStart = -1;
            currentFieldLast = -1;
            fieldDone = null;
            return result;
        }

        /// <summary>
        /// Records a confirmation (Enter / Down / Tab) for the current field.
        /// When no field is waiting yet the request is latched for the next
        /// <see cref="WaitForFieldAsync"/>.
        /// </summary>
        private void RequestMoveNext()
        {
            if (fieldDone != null)
            {
                fieldDone.TrySetResult("moveNext");
                return;
            }
            confirmPending = true;
        }

        /// <summary>
        /// Copies each mirror field's text from the field it mirrors, so e.g.
        /// the <c>if(~reset)</c> of an alwaysFF block follows the reset the user
        /// picked in the sensitivity list.
        /// </summary>
        private async Task SyncMirrorsAsync()
        {
            if (entry == null || document == null) return;

            // Highlights are appended in field order, so field i's highlight is
            // highlight i (mirrors included).
            Dictionary<int, int> highlightOfField = new();
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                highlightOfField[entry.Fields[i].Index] = i;
            }

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (document == null) return;
                // Walk backwards so replacing a mirror does not invalidate the
                // highlight positions of the fields before it.
                for (int i = entry.Fields.Count - 1; i >= 0; i--)
                {
                    SnippetField field = entry.Fields[i];
                    if (field.MirrorOf == null) continue;
                    if (!highlightOfField.TryGetValue(field.MirrorOf.Value, out int sourceHighlight)) continue;

                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(sourceHighlight, out int sourceStart, out int sourceLast);
                    if (sourceStart < 0 || sourceLast < sourceStart) continue;
                    string text = document.CreateString(sourceStart, sourceLast - sourceStart);
                    if (text.Length == 0) continue;

                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(i, out int mirrorStart, out int mirrorLast);
                    if (mirrorStart < 0) continue;
                    document.Replace(mirrorStart, Math.Max(0, mirrorLast - mirrorStart), 0, text);
                }
            });
        }

        /// <summary>
        /// A popup row that reports the picked candidate back to the runner.
        /// The editor's <c>ToolItem</c> has no "value", so the candidate name is
        /// carried here and completed on selection.
        /// </summary>
        private sealed class CandidateToolItem : CodeEditor2.CodeEditor.PopupMenu.ToolItem
        {
            private readonly string candidate;
            private readonly TaskCompletionSource<string>? completion;

            public CandidateToolItem(string candidate, TaskCompletionSource<string>? completion)
                : base(candidate)
            {
                this.candidate = candidate;
                this.completion = completion;
            }

            public override Task ApplyAsync()
            {
                completion?.TrySetResult(candidate);
                return Task.CompletedTask;
            }
        }

        private static string GetIndent(CodeEditor2.CodeEditor.CodeDocument document, int index)
        {
            if (index < 0) index = 0;
            if (index > document.Length) index = document.Length;
            int line = document.GetLineAt(index);
            int lineStart = document.GetLineStartIndex(line);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = lineStart; i < document.Length; i++)
            {
                char ch = document.GetCharAt(i);
                if (ch != ' ' && ch != '\t') break;
                sb.Append(ch);
            }
            return sb.ToString();
        }

        // ---- UI thread handlers --------------------------------------------

        public override void Aborted()
        {
            CodeEditor2.Controller.CodeEditor.PostClearHighlight();
            document = null;
            confirmPending = false;
            checkLine = null;
            currentFieldStart = -1;
            currentFieldLast = -1;
            fieldDone?.TrySetResult("abort");
            choiceMade?.TrySetResult("");
            base.Aborted();
        }

        public override void KeyDown(object? sender, KeyEventArgs e, PopupMenuView popupMenuView)
        {
            if (CodeEditor2.Controller.CodeEditor.IsPopupMenuOpened) return;

            if (e.Key == Key.Escape || e.Key == Key.Up)
            {
                // drop a latched Enter too: the snippet is ending
                confirmPending = false;
                fieldDone?.TrySetResult("abort");
                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                e.Handled = true;
            }
            // Key.Return and Key.Enter are distinct in Avalonia (Enter is the
            // numeric keypad key); the popup handles both, so the snippet must
            // too or the numpad Enter silently did nothing.
            else if (e.Key == Key.Return || e.Key == Key.Enter || e.Key == Key.Down || e.Key == Key.Tab)
            {
                // RequestMoveNext instead of fieldDone?.TrySetResult: the wait
                // source does not exist yet while the highlight is being
                // selected / the popup is closing, and an Enter in that window
                // used to be swallowed (the user had to press Enter twice).
                RequestMoveNext();
                e.Handled = true;
            }
        }

        public override void AfterAutoCompleteHandled(PopupMenuView popupMenuView)
        {
            // Only complete a field that is actually waiting. This hook also
            // fires for the ordinary code-completion popup (AutoCompleteItem
            // calls Controller.CodeEditor.AutoCompleteHandled), so latching here
            // would silently skip the field the user is editing.
            fieldDone?.TrySetResult("moveNext");
        }

        public override void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (checkLine == null) return;
            if (document == null) return;
            int? caret = CodeEditor2.Controller.CodeEditor.GetCaretPosition();
            if (caret == null) return;

            // Being anywhere inside the current field - including its very
            // start and the position just past its end - is legitimate: the
            // snippet itself puts the caret on the highlight start when it
            // selects a field, and a caret sitting exactly there used to look
            // like "the user went somewhere else". Only a caret outside the
            // range means the user left the field.
            if (currentFieldStart >= 0
                && (int)caret >= currentFieldStart
                && (int)caret <= currentFieldLast + 1)
            {
                return;
            }

            if (document.GetLineAt((int)caret) != checkLine)
            {
                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
            }
        }
    }
}
