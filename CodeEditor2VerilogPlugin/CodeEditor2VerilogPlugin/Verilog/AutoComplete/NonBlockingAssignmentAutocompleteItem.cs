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
        public NonBlockingAssignmentAutoCompleteItem() : base("<=", CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Plugin.ThemeColor, "CodeEditor2/Assets/Icons/screwdriver.svg")
        {
        }

        public override void Apply()
        {
            if (codeDocument == null) return;
            CodeEditor.CodeDocument? document = (codeDocument as CodeEditor.CodeDocument);
            if (document == null) return;

            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            int headIndex, length;

            document.GetWord(prevIndex, out headIndex, out length);
            string indent = document.GetIndentString(prevIndex);
            string cr = document.NewLine;

            char currentChar = document.GetCharAt(document.CaretIndex);

            string appendText = "<= #P_DELAY[]";

            //if (currentChar != '\r' && currentChar != '\n')
            //{
            //    appendText = "";
            //}
            int selectStart = appendText.IndexOf("[");
            int selectLast = appendText.IndexOf("]");
            appendText = appendText.Replace("[", "");
            appendText = appendText.Replace("]", "");

            document.Replace(headIndex, length, ColorIndex, appendText);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + selectStart);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + selectStart, headIndex + selectLast - 2);
        }

    }
}
