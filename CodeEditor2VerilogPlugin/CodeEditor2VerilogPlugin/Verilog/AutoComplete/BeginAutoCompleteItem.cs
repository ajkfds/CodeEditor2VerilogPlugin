using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class BeginAutoCompleteItem : AutocompleteItem
    {
        public BeginAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text,colorIndex,color, "CodeEditor2/Assets/Icons/gear.svg")
        {
        }
 
        public override void Apply()
        {
            if (codeDocument == null) return;
            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            char currentChar = codeDocument.GetCharAt(codeDocument.CaretIndex);
            if (currentChar != '\r' && currentChar != '\n') return;

            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);
            codeDocument.Replace(headIndex, length, ColorIndex, Text+" end");
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + Text.Length,headIndex + Text.Length);
        }
    }
}
