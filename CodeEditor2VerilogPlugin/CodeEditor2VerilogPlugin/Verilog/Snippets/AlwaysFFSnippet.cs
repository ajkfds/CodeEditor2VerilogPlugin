using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static pluginVerilog.Tool.ParseHierarchy;

namespace pluginVerilog.Verilog.Snippets
{
    public class AlwaysFFSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AlwaysFFSnippet() : base("alwaysFF")
        {
        }

        private CodeEditor2.CodeEditor.CodeDocument? document;

        // initial value for {n}
        private List<string> initials = new List<string> { "clock", "reset_x", "reset_x", "" };
        private List<string> clocks = new List<string> { };
        private List<string> resets = new List<string> { };
        public override void Apply()
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.Apply");

            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            document = file.CodeDocument;
            if(document == null) return;

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

            if(parsedDocument != null)
            {
                BuildingBlocks.BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(index);
                IPortNameSpace? portNameSpace = buildingBlock as IPortNameSpace;
                if (portNameSpace != null)
                {
                    foreach(DataObjects.Port port in portNameSpace.PortsList)
                    {
                        if (port.DataObject != null && port.DataObject.SyncContext.Data.Contains("clock"))
                        {
                            if(!clocks.Contains(port.Name)) clocks.Add(port.Name);
                        }
                        if (port.DataObject != null && port.DataObject.SyncContext.Data.Contains("reset"))
                        {
                            if (!clocks.Contains(port.Name)) resets.Add(port.Name);
                        }
                    }
                }
            }

            if (clocks.Count == 1) initials[0] = clocks[0];
            if(resets.Count==1)
            {
                initials[1] = resets[0];
                initials[2] = resets[0];
            }

            for (int i = 0; i < initials.Count; i++)
            {
                string target = "{" + i.ToString() + "}";
                if (!replaceText.Contains(target)) break;
                startIndexes.Add(index + replaceText.IndexOf(target));
                lastIndexes.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
                replaceText = replaceText.Replace(target, initials[i]);
            }

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
            System.Threading.Tasks.Task.Run(RunAsync);
        }

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
                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
            }
            return;
        }

        private TaskCompletionSource<string> _eventTcs;
        private async System.Threading.Tasks.Task runBackGround(CancellationToken token)
        {
            try
            {
                if (document == null) return;
                string result;

                // clock
                await Dispatcher.UIThread.InvokeAsync(() => {
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(0);    // move carlet to next highlight
                });

                if (clocks.Count != 1)
                {
                    // wait clock input
                    _eventTcs = new TaskCompletionSource<string>();
                    result = await _eventTcs.Task;
                    if (result != "moveNext") return;
                }

                await Dispatcher.UIThread.InvokeAsync(() => {
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(1);    // move carlet to next highlight
                });

                if (resets.Count != 1)
                {
                    // wait reset input
                    _eventTcs = new TaskCompletionSource<string>();
                    result = await _eventTcs.Task;
                    if (result != "moveNext") return;
                }

                // copy text from {1} to {2}
                await Dispatcher.UIThread.InvokeAsync(() => {
                    int start, last;
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(1, out start, out last);
                    string text = document.CreateString(start, last - start + 1);
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(2, out start, out last);
                    document.Replace(start, last - start + 1, 0, text);
                });

                // move next
                await Dispatcher.UIThread.InvokeAsync(() => {
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(3);    // move carlet to next highlight
                });
            }
            catch(Exception ex)
            {
                CodeEditor2.Controller.AppendLog("##Exception " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                Aborted();
            }
        }


        public override void Aborted()
        {
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            document = null;
            base.Aborted();
        }




        private List<int> startIndexes = new List<int>();
        private List<int> lastIndexes = new List<int>();

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
                    //bool moved;
                    //moveToNextHighlight(out moved);
                    //if (!moved) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    if (_eventTcs != null) _eventTcs.TrySetResult("moveNext");
                    //bool moved;
                    //moveToNextHighlight(out moved);
                    //if (!moved) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
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

        private void moveToNextHighlight(out bool moved)
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.moveToNextHighlight");
            moved = false;
            if (document == null) return;

            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
            if (i == -1) return;
            i++;
            if (i >= initials.Count)
            {
                Aborted();
                return;
            }

            CodeEditor2.Controller.CodeEditor.SelectHighlight(i);
            moved = true;
        }
    }
}

