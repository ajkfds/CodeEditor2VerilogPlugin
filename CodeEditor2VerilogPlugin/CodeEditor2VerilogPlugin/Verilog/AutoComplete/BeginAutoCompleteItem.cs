﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class BeginAutoCompleteItem : CodeEditor2.CodeEditor.AutocompleteItem
    {
        public BeginAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color) : base(text,colorIndex,color)
        {
        }
        public BeginAutoCompleteItem(string text, byte colorIndex, Avalonia.Media.Color color, Avalonia.Media.IImage icon, AjkAvaloniaLibs.Libs.Icons.ColorStyle iconColorStyle) : base(text,colorIndex,color,icon,iconColorStyle)
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

            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);
            codeDocument.Replace(headIndex, length, ColorIndex, Text+" end");
            codeDocument.CaretIndex = headIndex + Text.Length;
            codeDocument.SelectionStart = headIndex + Text.Length;
            codeDocument.SelectionLast = headIndex + Text.Length;
        }
    }
}
