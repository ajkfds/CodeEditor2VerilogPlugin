﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class InterfaceAutocompleteItem : CodeEditor2.CodeEditor.AutocompleteItem
    {
        public InterfaceAutocompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text, colorIndex, color)
        {
        }
        public override void Apply(CodeEditor2.CodeEditor.CodeDocument codeDocument)
        {
            CodeEditor.CodeDocument? vCodeDocument = codeDocument as CodeEditor.CodeDocument;
            if (vCodeDocument == null) return;

            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            char currentChar = codeDocument.GetCharAt(codeDocument.CaretIndex);
            if (currentChar != '\r' && currentChar != '\n') return;
            string indent = vCodeDocument.GetIndentString(prevIndex);

            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);
            codeDocument.Replace(headIndex, length, ColorIndex, Text + ";\r\n" + indent + "endinterface");
            codeDocument.CaretIndex = headIndex + Text.Length;
            codeDocument.SetSelection(headIndex + Text.Length, headIndex + Text.Length);
        }
    }
}
