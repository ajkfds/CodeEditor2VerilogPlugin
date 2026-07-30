using Avalonia.Media;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon
{
    public class AutoCompleteItem : CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem
    {
        //public AutoCompleteItem(string text, byte colorIndex, Color color) : base(text,colorIndex,color)
        //{
        //}
        public AutoCompleteItem(string text, byte colorIndex, Color color, string svgPath) : base(text, colorIndex, color, svgPath)
        {
        }

        // autocomplete item作成時に取得したheadindex, lengthの場所を更新する
        //public override System.Threading.Tasks.Task ApplyAsync()
        //{
        //    if (codeDocument == null) return System.Threading.Tasks.Task.CompletedTask;
        //    int prevIndex = codeDocument.CaretIndex;
        //    if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
        //    {
        //        prevIndex--;
        //    }
        //    if (codeDocument.GetCharAt(prevIndex) == '.')
        //    {
        //        int index = codeDocument.CaretIndex;
        //        codeDocument.Replace(index, 0, ColorIndex, Text);
        //        CodeEditor2.Controller.CodeEditor.SetCaretPosition(index + Text.Length);
        //    }
        //    else
        //    {
        //        // delete after last .
        //        codeDocument.Replace(headIndex, length, ColorIndex, Text);
        //        CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
        //    }
        //    CodeEditor2.Controller.CodeEditor.AutoCompleteHandled();
        //    return System.Threading.Tasks.Task.CompletedTask;
        //}

        public override async Task ApplyAsync()
        {
            if (codeDocument == null) return;
            Data.IVerilogRelatedFile? file = codeDocument.TextFile as Data.IVerilogRelatedFile;
            if(file == null) return;
            Verilog.ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;


            Data.VerilogCommon.AutoComplete.GetAutoCompleteTarget(
                file, parsedDocument, codeDocument.CaretIndex,
                out Verilog.NameSpace? nameSpace,
                out Verilog.INamedElement? namedElement,
                out string cantidate,
                out int cantidateIndex);

            // delete after last .
            codeDocument.Replace(cantidateIndex, cantidate.Length, ColorIndex, Text);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(cantidateIndex + Text.Length);

            CodeEditor2.Controller.CodeEditor.AutoCompleteHandled();
            return;
        }
    }
}
