using System;
using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CodeEditor2.CodeEditor.PopupMenu;
using System.Threading.Tasks;
using System.Text;

namespace pluginVerilog.CodeEditor
{
    public class AutoCompleteItem : CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem
    {
        public AutoCompleteItem(CompleteType type, string text, byte colorIndex, Color color) : base(text,colorIndex,color)
        {
            Type = type;
        }
        public AutoCompleteItem(CompleteType type, string text, byte colorIndex, Color color, string svgPath) : base(text,colorIndex,color,svgPath)
        {
            Type = type;
        }

        public CompleteType Type { get; init; }

        public enum CompleteType
        {
            Keyword,
            Task,
            Function,
            Variable
        }


    }
}
