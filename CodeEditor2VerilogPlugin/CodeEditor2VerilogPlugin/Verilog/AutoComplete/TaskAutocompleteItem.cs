﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class TaskAutocompleteItem : AutocompleteItem
    {
        public TaskAutocompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color)
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
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + Text.Length,headIndex + Text.Length);
        }
    }
}
