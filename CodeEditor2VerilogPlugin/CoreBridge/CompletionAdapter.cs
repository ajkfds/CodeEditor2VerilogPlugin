
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free completion adapter for LSP hosts.
    /// Drives the plugin's Verilog.CompletionContext (partial parse based
    /// autocomplete) on an already-parsed document and converts the result
    /// into plain (text, kind) entries so hosts without an Avalonia
    /// dispatcher (LSP server, unit tests) can serve textDocument/completion.
    /// </summary>
    public static class CompletionAdapter
    {
        public sealed class CompletionEntry
        {
            public string Text { get; init; } = "";
            /// <summary>LSP CompletionItemKind value.</summary>
            public int Kind { get; init; }
            public string Detail { get; init; } = "";
        }

        /// <summary>
        /// Returns completion items at the given index (caret position),
        /// or null when completion is unavailable (no parse result / out of
        /// range index).
        /// </summary>
        public static IReadOnlyList<CompletionEntry>? GetCompletionItems(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index
            )
        {
            if (file == null || parsedDocument == null) return null;
            if (index < 0) return null;

            // The CompletionContext partial parse reads the source text from
            // file.CodeDocument. In UI-less hosts FileCheckAsync has not run,
            // so the document is either absent or empty: create it (via the
            // protected factory) and load the current text. The text comes
            // from the parse result's own document (ParseEngine filled it
            // with the same text).
            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = file.CodeDocument;
            if (codeDocument == null) return null;
            if (parsedDocument.CodeDocument != null && codeDocument.Length != parsedDocument.CodeDocument.Length)
            {
                codeDocument.CopyTextOnlyFrom(parsedDocument.CodeDocument);
            }
            if (index > codeDocument.Length) return null;

            // runs the real CompletionContext (partial parse of the region
            // around the caret, UI-free; logs are posted to a dispatcher that
            // may not run in headless hosts, which is harmless)
            Verilog.CompletionContext? context =
                Data.VerilogCommon.AutoComplete.GetAutoCompleteItems(file, parsedDocument, index);
            if (context == null) return null;

            List<CompletionEntry> entries = new List<CompletionEntry>();
            HashSet<string> seen = new HashSet<string>();
            foreach (CodeEditor2.CodeEditor.PopupMenu.ToolItem item in context.AutoCompleteItems)
            {
                if (item == null) continue;
                string text = item.Text;
                if (string.IsNullOrEmpty(text)) continue;
                if (!seen.Add(text)) continue; // reject duplicated entries

                // LSP CompletionItemKind: 3=Function, 6=Variable, 9=Module, 14=Keyword
                int kind = 14;
                string detail = "";
                if (item is Data.VerilogCommon.AutoCompleteItem acItem)
                {
                    switch (acItem.Type)
                    {
                        case Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword:
                            kind = 14;
                            break;
                        case Data.VerilogCommon.AutoCompleteItem.CompleteType.Function:
                            kind = 3;
                            break;
                        case Data.VerilogCommon.AutoCompleteItem.CompleteType.Task:
                            kind = 3;
                            break;
                        case Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject:
                            kind = 6;
                            break;
                        case Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace:
                            kind = 9;
                            break;
                    }
                    detail = acItem.Type.ToString();
                }

                entries.Add(new CompletionEntry { Text = text, Kind = kind, Detail = detail });
            }
            return entries;
        }
    }
}
