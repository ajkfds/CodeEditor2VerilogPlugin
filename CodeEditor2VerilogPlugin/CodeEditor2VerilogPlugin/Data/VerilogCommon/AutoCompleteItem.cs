using Avalonia.Media;
using CodeEditor2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon
{
    public class AutoCompleteItem: CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem
    {
        //public AutoCompleteItem(string text, byte colorIndex, Color color) : base(text,colorIndex,color)
        //{
        //}
        public AutoCompleteItem(string text, byte colorIndex, int headIndex,int length, Color color, string svgPath) : base(text, colorIndex, color, svgPath)
        {
            this.headIndex = headIndex;
            this.length = length;
        }

        private int headIndex = -1;
        private int length = -1;

        public override void Apply()
        {
            if (codeDocument == null) return;
            codeDocument.Replace(headIndex, length, ColorIndex, Text);
            Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
        }


    }
}
