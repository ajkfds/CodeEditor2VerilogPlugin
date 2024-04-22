using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class TaskAutocompleteItem : CodeEditor2.CodeEditor.AutocompleteItem
    {
        public TaskAutocompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color)
        {
        }

        public override void Apply(CodeEditor2.CodeEditor.CodeDocument codeDocument)
        {
            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);

            char currentChar = codeDocument.GetCharAt(codeDocument.CaretIndex);
//            string appendText = ";\r\nbegin\r\nend\r\nendtask";
            string appendText = "\r\nendtask";
            if (currentChar != '\r' && currentChar != '\n')
            {
                appendText = "";
            }

            codeDocument.Replace(headIndex, length, ColorIndex, Text + appendText);
            codeDocument.CaretIndex = headIndex + Text.Length;
            codeDocument.SetSelection(headIndex + Text.Length,headIndex + Text.Length);
        }
    }
}
