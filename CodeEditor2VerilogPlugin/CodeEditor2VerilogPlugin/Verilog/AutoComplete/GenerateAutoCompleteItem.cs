using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class GenerateAutoCompleteItem : CodeEditor2.CodeEditor.AutocompleteItem
    {
        public GenerateAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color)
        {
        }

        public override void Apply(CodeEditor2.CodeEditor.CodeDocument codeDocument)
        {
            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            char currentChar = codeDocument.GetCharAt(codeDocument.CaretIndex);
            if (currentChar != '\r' && currentChar != '\n') return;
            string indent = (codeDocument as CodeEditor.CodeDocument).GetIndentString(prevIndex);

            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);
            codeDocument.Replace(headIndex, length, ColorIndex, Text + "\r\n"+indent+"endgenerate");
            codeDocument.CaretIndex = headIndex + Text.Length;
            codeDocument.SelectionStart = headIndex + Text.Length;
            codeDocument.SelectionLast = headIndex + Text.Length;
        }
    }
}
