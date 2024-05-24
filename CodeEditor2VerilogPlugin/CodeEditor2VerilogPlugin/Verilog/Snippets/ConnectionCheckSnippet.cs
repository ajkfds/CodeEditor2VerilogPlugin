using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;


namespace pluginVerilog.Verilog.Snippets
{
    ////public class ConnectionCheckSnippet : CodeEditor2.Snippets.InteractiveSnippet
    ////{
        //public ConnectionCheckSnippet() : base("connectionCheck")
        //{
        //}

        //private CodeDocument document;

        //public override void Apply(CodeDocument codeDocument)
        //{
        //    document = codeDocument;
        //    string indent = "\t";
        //    string replaceText =
        //        indent + "always @(posedge {0} or negedge {1})\r\n" +
        //        indent + "begin\r\n" +
        //        indent + "\tif(~{2}) begin\r\n" +
        //        indent + "\t\t{3}\r\n" +
        //        indent + "\tend else begin\r\n" +
        //        indent + "\t\t\r\n" +
        //        indent + "\tend\r\n" +
        //        indent + "end";


        //    int index = codeDocument.CaretIndex;

        //    for (int i = 0; i < initials.Count; i++)
        //    {
        //        string target = "{" + i.ToString() + "}";
        //        if (!replaceText.Contains(target)) break;
        //        startIndexs.Add(index + replaceText.IndexOf(target));
        //        lastIndexs.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
        //        replaceText = replaceText.Replace(target, initials[i]);
        //    }

        //    codeDocument.Replace(index, 0, 0, replaceText);
        //    CodeEditor2.Controller.CodeEditor.SetCaretPosition(startIndexs[0];
        //    codeDocument.SelectionStart = startIndexs[0];
        //    codeDocument.SelectionLast = lastIndexs[0] + 1;

        //    CodeEditor2.Controller.CodeEditor.ClearHighlight();
        //    for (int i = 0; i < startIndexs.Count; i++)
        //    {
        //        CodeEditor2.Controller.CodeEditor.AppendHighlight(startIndexs[i], lastIndexs[i]);
        //    }

        //    base.Apply(codeDocument);
        //}

        //private List<string> initials = new List<string> { "clock", "reset_x", "reset_x", "" };

        //public override void Aborted()
        //{
        //    CodeEditor2.Controller.CodeEditor.ClearHighlight();
        //    document = null;
        //    base.Aborted();
        //}

        //private List<int> startIndexs = new List<int>();
        //private List<int> lastIndexs = new List<int>();

        //public override void BeforeKeyDown(object sender, KeyEventArgs e, CodeEditor2.CodeEditor.AutoCompleteForm autoCompleteForm)
        //{
        //    if (autoCompleteForm == null || autoCompleteForm.Visible == false)
        //    {
        //        if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Escape)
        //        {
        //            bool moved;
        //            moveToNextHighlight(out moved);
        //            if (!moved) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        //            e.Handled = true;
        //        }
        //    }
        //}
        //public override void AfterKeyDown(object sender, KeyEventArgs e, CodeEditor2.CodeEditor.AutoCompleteForm autoCompleteForm)
        //{

        //}
        //public override void AfterAutoCompleteHandled(object sender, KeyEventArgs e, CodeEditor2.CodeEditor.AutoCompleteForm autoCompleteForm)
        //{
        //    if (e.Handled) // closed
        //    {
        //        int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
        //        switch (i)
        //        {
        //            case 0:
        //                CodeEditor2.Controller.CodeEditor.SelectHighlight(1);
        //                break;
        //            case 1:
        //                int start, last;
        //                CodeEditor2.Controller.CodeEditor.GetHighlightPosition(1, out start, out last);
        //                string text = document.CreateString(start, last - start + 1);
        //                CodeEditor2.Controller.CodeEditor.GetHighlightPosition(2, out start, out last);
        //                document.Replace(start, last - start + 1, 0, text);
        //                CodeEditor2.Controller.CodeEditor.SelectHighlight(3);
        //                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        //                CodeEditor2.Controller.CodeEditor.RequestReparse();
        //                break;
        //            case 2:
        //                CodeEditor2.Controller.CodeEditor.SelectHighlight(3);
        //                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        //                CodeEditor2.Controller.CodeEditor.RequestReparse();
        //                break;
        //            case 3:
        //                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        //                break;
        //            default:
        //                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        //                break;
        //        }
        //    }
        //}

        //private void moveToNextHighlight(out bool moved)
        //{
        //    moved = false;
        //    moved = false;
        //    int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
        //    if (i == -1) return;
        //    i++;
        //    if (i >= initials.Count) return;

        //    CodeEditor2.Controller.CodeEditor.SelectHighlight(i);
        //    moved = true;
        //}
//    }
}
