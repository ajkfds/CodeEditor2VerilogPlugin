using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class GenerateAutoCompleteItem : AutocompleteItem
    {
        public GenerateAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color, "CodeEditor2/Assets/Icons/gear.svg")
        {
        }

        public override void Apply()
        {
            if (codeDocument == null) return;
            CodeEditor.CodeDocument? document = codeDocument as CodeEditor.CodeDocument;
            if (document == null) return;

            int prevIndex = document.CaretIndex;
            if (document.GetLineStartIndex(document.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            char currentChar = document.GetCharAt(document.CaretIndex);
            if (currentChar != '\r' && currentChar != '\n') return;
            string indent = document.GetIndentString(prevIndex);

            int headIndex, length;
            document.GetWord(prevIndex, out headIndex, out length);
            document.Replace(headIndex, length, ColorIndex, Text + "\r\n"+indent+"endgenerate");
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + Text.Length, headIndex + Text.Length);
        }
    }
}
