using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CodeEditor2.CodeEditor;


namespace pluginVerilog
{
    public class CodeDrawStyle : CodeEditor2.CodeEditor.CodeDrawStyle
    {
        public CodeDrawStyle()
        {
            colors = new Color[16]
            {
                Avalonia.Media.Color.FromRgb(212,212,212),         // Normal
                    Avalonia.Media.Color.FromRgb(150,150,150),     // inactivated
                    Avalonia.Media.Colors.DarkGray,                // 2
                    Avalonia.Media.Color.FromRgb(255,50,50),       // Resister
                    Avalonia.Media.Color.FromRgb(86,156,214),      // keyword
                    Avalonia.Media.Color.FromRgb(106,153,85),      // Comment
                    Avalonia.Media.Color.FromRgb(78,201,176),      // identifier
                    Avalonia.Media.Color.FromRgb(255,94,194),      // Parameter
                    Avalonia.Media.Color.FromRgb(206,145,120),     // number
                    Avalonia.Media.Color.FromRgb(255,150,200),     // Net
                    Avalonia.Media.Color.FromRgb(200,255,100),     // highlighted comment
                    Avalonia.Media.Color.FromRgb(255,200,200),     // Variable
                    Avalonia.Media.Color.FromRgb(200,255,100),           // comment annotation
//                    Avalonia.Media.Color.FromRgb(86,123,65),      // comment annotation
                    Avalonia.Media.Colors.Black,                   // 13
                    Avalonia.Media.Colors.Black,                   // 14
                    Avalonia.Media.Colors.Black                    // 15
            };

            markStyle = new CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail[]
            {
                // 0 error
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,255,120,120), // red
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 1,
                    DecorationWidth = 4,
                    Thickness = 2,
                },
                // 1 warning
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,255,250,150), // yellow
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 2,
                    DecorationWidth = 4,
                    Thickness = 1,
                },
                // 2 notice
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,20,255,20),   // green
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 1.5,
                    DecorationWidth = 6,
                    Thickness = 2,
                },
                // 3 hint
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,106,176,224), // cyan
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = -1.2,
                    DecorationWidth = 3,
                    Thickness = 2,
                },
                // 4
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromRgb(  0,255,  0),  // green
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 4,
                    DecorationWidth = 4
                },
                // 5
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromRgb(  0,255,  0),  // green
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 4,
                    DecorationWidth = 4
                },
                // 6
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromRgb(  0,255,  0),  // green
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 4,
                    DecorationWidth = 4
                },
                // 7
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromRgb(  0,255,  0),
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 4,
                    DecorationWidth = 4
                },
            };


        }

        public static byte ColorIndex(ColorType colorType)
        {
            return (byte)colorType;
        }

        public Color Color(ColorType index)
        {
            return colors[(int)index];
        }

        public Color GetColor(ColorType colorType)
        {
            return colors[(byte)colorType];
        }
        public enum ColorType : byte
        {
            Normal = 0,
            Comment = 5,
            Register = 3,
            Net = 9,
            Variable = 11,
            Parameter = 7,
            Keyword = 4,
            Identifier = 6,
            Number = 8,
            Inactivated = 1,
            HighLightedComment = 10,
            CommentAnnotation = 12
        }


    }
}
