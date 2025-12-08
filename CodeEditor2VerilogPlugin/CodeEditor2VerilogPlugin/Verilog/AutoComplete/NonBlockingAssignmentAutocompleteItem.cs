using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class NonBlockingAssignmentAutoCompleteItem : AutocompleteItem
    {
        public NonBlockingAssignmentAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color, "CodeEditor2/Assets/Icons/gear.svg")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/screwdriver.svg",
                    Plugin.ThemeColor
                    );
        }

        public override void Apply()
        {
            if (codeDocument == null) return;
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
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + Text.Length + appendText.Length, headIndex + Text.Length + appendText.Length);
        }

    }
}
