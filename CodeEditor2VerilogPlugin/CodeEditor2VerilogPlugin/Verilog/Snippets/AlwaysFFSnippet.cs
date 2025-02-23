using Avalonia.Input;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override void Apply()
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.Apply");

            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            document = file.CodeDocument;

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

        }


        public override void Aborted()
        {
            System.Diagnostics.Debug.Print("## AlwaysFFSnippet.Aborted");

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
                if (e.Key == Key.Return || e.Key == Key.Escape)
                {
                    bool moved;
                    moveToNextHighlight(out moved);
                    if (!moved) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
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

            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
            switch (i)
            {
                case 0: // clock
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(1);    // move carlet to next highlight
                    break;
                case 1: // reset
                    // copy text from {1} to {2}
                    int start, last;
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(1, out start, out last);
                    string text = document.CreateString(start, last - start + 1);
                    CodeEditor2.Controller.CodeEditor.GetHighlightPosition(2, out start, out last);
                    document.Replace(start, last - start + 1, 0, text);
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(3);    // move carlet to next highlight
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    CodeEditor2.Controller.CodeEditor.RequestReparse();
                    break;
                case 2: // reset (skip this input)
                    CodeEditor2.Controller.CodeEditor.SelectHighlight(3);
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    CodeEditor2.Controller.CodeEditor.RequestReparse();
                    break;
                case 3:
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    break;
                default:
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    break;
            }
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

