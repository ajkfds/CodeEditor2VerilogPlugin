using Avalonia.Media;


namespace pluginVerilog
{
    public class CodeDrawStyle : CodeEditor2.CodeEditor.CodeDrawStyle
    {
        public CodeDrawStyle()
        {
            colors = new Color[16]
            {
                Avalonia.Media.Color.FromRgb(212,212,212),     //  0 : Normal
                Avalonia.Media.Color.FromRgb(150,150,150),     //  1 : Inactivated
                Avalonia.Media.Colors.DarkGray,                //  2 :
                Avalonia.Media.Color.FromRgb(255,50,50),       //  3 : Register
                Avalonia.Media.Color.FromRgb(86,156,214),      //  4 : Keyword
                Avalonia.Media.Color.FromRgb(106,153,85),      //  5 : Comment
                Avalonia.Media.Color.FromRgb(78,201,176),      //  6 : Identifier
                Avalonia.Media.Color.FromRgb(255,94,194),      //  7 : Parameter
                Avalonia.Media.Color.FromRgb(206,145,120),     //  8 : Number
                Avalonia.Media.Color.FromRgb(255,220,220),     //  9 : Net
                Avalonia.Media.Color.FromRgb(200,255,100),     // 10 : highlighted comment
                Avalonia.Media.Color.FromRgb(255,200,200),     // 11 : Variable
                Avalonia.Media.Color.FromRgb(200,255,100),     // 12 : Comment annotation
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
                    ZOrder = 7
                },
                // 1 warning
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,255,250,150), // yellow
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = -1,
                    DecorationWidth = 4,
                    Thickness = 2,
                    ZOrder = 6
                },
                // 2 notice
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,20,255,20),   // green
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = 0.5,
                    DecorationWidth = 4,
                    Thickness = 2,
                    ZOrder = 5
                },
                // 3 hint
                new MarkDetail{
                    Color = Avalonia.Media.Color.FromArgb(200,106,176,224), // cyan
                    Style = CodeEditor2.CodeEditor.CodeDrawStyle.MarkDetail.MarkStyleEnum.WaveLine,
                    DecorationHeight = -0.5,
                    DecorationWidth = 4,
                    Thickness = 2,
                    ZOrder = 4
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
