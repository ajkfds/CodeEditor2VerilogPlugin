using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Views;
using pluginVerilog.Verilog.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AjkAvaloniaLibs.Libs.Icons;
using static pluginVerilog.Tool.ParseHierarchy;

namespace pluginVerilog.Verilog.Snippets
{
    public class AlwaysFFSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AlwaysFFSnippet() : base("alwaysFF")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        private CodeEditor2.CodeEditor.CodeDocument? document;

        // initial value for {n}
        private List<string> initials = new List<string> { "clock", "reset_x", "reset_x", "" };
        private List<string> clocks = new List<string> { };
        private List<string> resets = new List<string> { };
        private Dictionary<string, Port> ports = new Dictionary<string, Port>();

        private int? checkLine = null;
        public override void Apply()
        {
            List<int> startIndexes = new List<int>();
            List<int> lastIndexes = new List<int>();


            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.Apply");

            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            document = file.CodeDocument;
            if (document == null) return;

            ParsedDocument? parsedDocument = file.ParsedDocument as ParsedDocument;


            string indent = "";
            if (document.GetCharAt(document.GetLineStartIndex(document.GetLineAt(document.CaretIndex))) == '\t')
            {
                indent = "\t";
            }

            string replaceText =
                indent + "always @(posedge {0} or negedge {1})\r\n" +
                indent + "begin\r\n" +
                indent + "\tif(~{2}) begin\r\n" +
                indent + "\t\t{3}\r\n" +
                indent + "\tend else begin\r\n" +
                indent + "\t\t\r\n" +
                indent + "\tend\r\n" +
                indent + "end";


            int index = document.CaretIndex;

            if (parsedDocument != null)
            {
                BuildingBlocks.BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(index);
                IPortNameSpace? portNameSpace = buildingBlock as IPortNameSpace;
                if (portNameSpace != null)
                {
                    foreach (DataObjects.Port port in portNameSpace.PortsList)
                    {
                        if (port.DataObject != null && port.DataObject.SyncContext.IsClock)
                        {
                            if (!clocks.Contains(port.Name)) clocks.Add(port.Name);
                        }
                        if (port.DataObject != null && port.DataObject.SyncContext.IsReset)
                        {
                            if (!clocks.Contains(port.Name)) resets.Add(port.Name);
                        }
                        if (!ports.ContainsKey(port.Name))
                        {
                            ports.Add(port.Name, port);
                        }
                    }
                }

                if (clocks.Count > 0) initials[0] = clocks[0];
                if (resets.Count > 0)
                {
                    initials[1] = resets[0];
                    initials[2] = resets[0];
                }

                // replace {n} to initials[n]
                for (int i = 0; i < initials.Count; i++)
                {
                    string target = "{" + i.ToString() + "}";
                    if (!replaceText.Contains(target)) break;
                    startIndexes.Add(index + replaceText.IndexOf(target));
                    lastIndexes.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
                    replaceText = replaceText.Replace(target, initials[i]);
                }

                // update text
                document.Replace(index, 0, 0, replaceText);
                CodeEditor2.Controller.CodeEditor.SetCaretPosition(startIndexes[0]);
                CodeEditor2.Controller.CodeEditor.SetSelection(startIndexes[0], lastIndexes[0]);

                // set highlights for {n} texts
                CodeEditor2.Controller.CodeEditor.ClearHighlight();
                for (int i = 0; i < startIndexes.Count; i++)
                {
                    CodeEditor2.Controller.CodeEditor.AppendHighlight(startIndexes[i], lastIndexes[i]);
                }

                base.Apply();

                // run async task
                System.Threading.Tasks.Task.Run(RunAsync);
            }
        }

        // backrtound thread ------------------------------------------------------

        private static System.Threading.Tasks.Task? _currentTask;
        private static CancellationTokenSource? _cts;
        private async System.Threading.Tasks.Task RunAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = System.Threading.Tasks.Task.Run(async () =>
            {
                await runBackGround(token);
            }, token);

            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _currentTask = null;
                await CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippetAsync();
            }
            return;
        }

        private TaskCompletionSource<string> _eventTcs; // return from UI thread
        private async System.Threading.Tasks.Task runBackGround(CancellationToken token)
        {
            try
            {
                if (document == null) return;
                string result;

                // clock
                await CodeEditor2.Controller.CodeEditor.SelectHighlightAsync(0);    // move carlet to next highlight

                if (clocks.Count == 0)
                { // normal text input
                    _eventTcs = new TaskCompletionSource<string>(); // wait clock input
                    checkLine = document.GetLineAt(document.SelectionStart);
                    result = await _eventTcs.Task;
                    checkLine = null;
                    if (result != "moveNext") return;
                } else if (clocks.Count == 1)
                { // use default value

                }
                else
                { // select from few list
                    List<ToolItem> items = new List<ToolItem>();
                    foreach (var clock in clocks)
                    {
                        if (!ports.ContainsKey(clock)) continue;
                        AutocompleteItem? item = ports[clock].DataObject?.CreateAutoCompleteItem();
                        if (item != null)
                        {
                            item.Assign(document);
                            items.Add(item);
                        }
                    }

                    await CodeEditor2.Controller.CodeEditor.ForceOpenCustomSelectionAsync(items);

                    _eventTcs = new TaskCompletionSource<string>(); // wait clock input
                    checkLine = document.GetLineAt(document.SelectionStart);
                    result = await _eventTcs.Task;
                    checkLine = null;
                    if (result != "moveNext") return;
                }

                await CodeEditor2.Controller.CodeEditor.SelectHighlightAsync(1);    // move carlet to next highlight

                if (resets.Count == 0)
                { // normal text input
                    _eventTcs = new TaskCompletionSource<string>();
                    checkLine = document.GetLineAt(document.SelectionStart);
                    result = await _eventTcs.Task;
                    checkLine = null;
                    if (result != "moveNext") return;
                }else if (resets.Count == 1)
                { // use default value

                }
                else
                {
                    List<ToolItem> items = new List<ToolItem>();
                    foreach (var reset in resets)
                    {
                        if (!ports.ContainsKey(reset)) continue;
                        AutocompleteItem? item = ports[reset].DataObject?.CreateAutoCompleteItem();
                        if (item != null)
                        {
                            item.Assign(document);
                            items.Add(item);
                        }
                    }

                    await CodeEditor2.Controller.CodeEditor.ForceOpenCustomSelectionAsync(items);

                    _eventTcs = new TaskCompletionSource<string>(); // wait clock input
                    checkLine = document.GetLineAt(document.SelectionStart);
                    result = await _eventTcs.Task;
                    checkLine = null;
                    if (result != "moveNext") return;
                }

                // copy text from {1} to {2}
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    int start, last;
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(1, out start, out last);
                    string text = document.CreateString(start, last - start + 1);
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(2, out start, out last);
                    document.Replace(start, last - start + 1, 0, text);
                });

                // move next
                await CodeEditor2.Controller.CodeEditor.SelectHighlightAsync(3);    // move carlet to next highlight
            }
            catch(Exception ex)
            {
                CodeEditor2.Controller.AppendLog("##Exception " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                await CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippetAsync();
            }
        }

        // UI thread handler ------------------------------------------------------

        public override void Aborted()
        {
            if (_cts != null) _cts.Cancel();
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            document = null;
            base.Aborted();
        }


        public override void KeyDown(object? sender, KeyEventArgs e, PopupMenuView popupMenuView)
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.KeyDown");

            // overrider return & escape
            if (!CodeEditor2.Controller.CodeEditor.IsPopupMenuOpened)
            {
                if (e.Key == Key.Escape || e.Key == Key.Up )
                {
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    e.Handled = true;
                } 
                else if (e.Key == Key.Return)
                {
                    if (_eventTcs != null) _eventTcs.TrySetResult("moveNext");
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    if (_eventTcs != null) _eventTcs.TrySetResult("moveNext");
                    e.Handled = true;
                }
            }
        }
        public override void BeforeKeyDown(object? sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.BeforeKeyDown");
        }
        public override void AfterKeyDown(object? sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.AfterKeyDown");
        }
        public override void AfterAutoCompleteHandled(CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            if (document == null) return;
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.AfterAutoCompleteHandled");
            if (_eventTcs != null) _eventTcs.TrySetResult("moveNext");
        }

        // return @ carlet line changed
        public override void Caret_PositionChanged(object? sender, EventArgs e)
        {
            if (checkLine == null) return;
            if (document == null) return;

            int? carletPosition = CodeEditor2.Controller.CodeEditor.GetCaretPosition();
            if(carletPosition == null) return;
            int line = document.GetLineAt((int)carletPosition);
            CodeEditor2.Controller.AppendLog("line "+line.ToString()+"=="+checkLine.ToString());
            if (line != checkLine) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();

        }

    }
}

