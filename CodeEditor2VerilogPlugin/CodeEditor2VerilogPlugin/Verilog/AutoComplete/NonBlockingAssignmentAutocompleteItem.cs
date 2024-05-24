using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class NonBlockingAssignmentAutoCompleteItem : CodeEditor2.CodeEditor.AutocompleteItem
    {
        public NonBlockingAssignmentAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color)
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
            string appendText = " #P_DELAY";
            if (currentChar != '\r' && currentChar != '\n')
            {
                appendText = "";
            }

            codeDocument.Replace(headIndex, length, ColorIndex, Text + appendText );
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length + appendText.Length);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + Text.Length,headIndex + Text.Length);
        }

    }
}
