using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;


namespace pluginVerilog
{
    public class CodeDrawStyle : CodeEditor2.CodeEditor.CodeDrawStyle
    {
        public CodeDrawStyle()
        {
            colors = new Color[16]
            {
                Avalonia.Media.Color.FromRgb(212,212,212),     // Normal
                    Avalonia.Media.Color.FromRgb(150,150,150),     // inactivated
                    Avalonia.Media.Colors.DarkGray,                  // 2
                    Avalonia.Media.Color.FromRgb(255,50,50),       // Resister
                    Avalonia.Media.Color.FromRgb(86,156,214),      // keyword
                    Avalonia.Media.Color.FromRgb(106,153,85),      // Comment
                    Avalonia.Media.Color.FromRgb(78,201,176),      // identifier
                    Avalonia.Media.Color.FromRgb(255,94,194),      // Parameter
                    Avalonia.Media.Color.FromRgb(206,145,120),     // number
                    Avalonia.Media.Color.FromRgb(255,150,200),     // Net
                    Avalonia.Media.Color.FromRgb(200,255,100),     // highlighted comment
                    Avalonia.Media.Color.FromRgb(255,200,200),     // Variable
                    Avalonia.Media.Colors.Black,                     // 12
                    Avalonia.Media.Colors.Black,                     // 13
                    Avalonia.Media.Colors.Black,                     // 14
                    Avalonia.Media.Colors.Black                      // 15
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

        public enum ColorType : byte
        {
            Normal = 0,
            Comment = 5,
            Register = 3,
            Net = 9,
            Variable = 11,
            Paramater = 7,
            Keyword = 4,
            Identifier = 6,
            Number = 8,
            Inactivated = 1,
            HighLightedComment = 10
        }

        public override Color[] MarkColor
        {
            get
            {
                return new Avalonia.Media.Color[8]
                    {
                        Avalonia.Media.Color.FromArgb(200,255,120,120),    // 0 error
                        Avalonia.Media.Color.FromArgb(200,255,150,100), // 1 warning
                        Avalonia.Media.Color.FromArgb(200,20,255,20), // 2 notice
                        Avalonia.Media.Color.FromArgb(200,106,176,224), // 3 hint
                        Avalonia.Media.Colors.Red, // 4
                        Avalonia.Media.Colors.Red, // 5
                        Avalonia.Media.Colors.Red, // 6
                        Avalonia.Media.Color.FromArgb(128,(int)(52*2),(int)(58*2),(int)(64*2))  // 7
                    };
            }
        }

        //public override ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum[] MarkStyle
        //{
        //    get
        //    {
        //        return new ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum[8]
        //            {
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.wave,    // 0
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.wave,    // 1
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.wave_inv,
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.wave,
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.underLine,
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.underLine,
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.underLine,
        //                ajkControls.CodeTextbox.CodeTextbox.MarkStyleEnum.fill
        //            };
        //    }
        //}

    }
}
