using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Nets
{
    /// <summary>
    /// SystemVerilog Drive Strength
    /// IEEE 1800-2017
    /// 
    /// drive_strength ::= 
    ///     ( strength0 , strength1 )
    ///   | ( strength1 , strength0 )
    ///   | ( strength0 , highz1 )
    ///   | ( strength1 , highz0 )
    ///   | ( highz0 , strength1 )
    ///   | ( highz1 , strength0 )
    /// 
    /// strength0 ::= supply0 | strong0 | pull0 | weak0
    /// strength1 ::= supply1 | strong1 | pull1 | weak1
    /// </summary>
    public class DriveStrength
    {
        /// <summary>
        /// Strength value for low (0) state
        /// </summary>
        public StrengthValueEnum Strength0 { get; }

        /// <summary>
        /// Strength value for high (1) state
        /// </summary>
        public StrengthValueEnum Strength1 { get; }

        /// <summary>
        /// Strength value enumeration
        /// </summary>
        public enum StrengthValueEnum
        {
            Supply0,
            Supply1,
            Strong0,
            Strong1,
            Pull0,
            Pull1,
            Weak0,
            Weak1,
            HighZ0,
            HighZ1
        }

        private DriveStrength(StrengthValueEnum strength0, StrengthValueEnum strength1)
        {
            Strength0 = strength0;
            Strength1 = strength1;
        }

        /// <summary>
        /// Parse drive strength specification
        /// </summary>
        public static DriveStrength? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            /*
            drive_strength ::=
                ( strength0 , strength1 )
              | ( strength1 , strength0 )
              | ( strength0 , highz1 )
              | ( strength1 , highz0 )
              | ( highz0 , strength1 )
              | ( highz1 , strength0 )

            strength0 ::= supply0 | strong0 | pull0 | weak0
            strength1 ::= supply1 | strong1 | pull1 | weak1
            */

            if (word.Text != "(") return null;

            List<string> cantidates = new List<string> {
                "supply0","strong0","pull0","weak0",
                "supply1","strong1","pull1","weak1",
                "highz0","highz1"
            };
            // will not proceed word if this is not drive strength
            if (!cantidates.Contains(word.NextText)) return null;

            word.MoveNext();

            StrengthValueEnum? strength0 = ParseStrengthValue(word);
            if (strength0 == null)
            {
                // Could be highz0 or highz1
                if (word.Text == "highz0")
                {
                    strength0 = StrengthValueEnum.HighZ0;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else if (word.Text == "highz1")
                {
                    strength0 = StrengthValueEnum.HighZ1;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
            }

            if (word.Text != ",")
            {
                word.AddError(", expected");
                if (word.Text == ")") word.MoveNext();
                return null;
            }
            word.MoveNext();

            StrengthValueEnum? strength1 = ParseStrengthValue(word);
            if (strength1 == null)
            {
                // Could be highz0 or highz1
                if (word.Text == "highz0")
                {
                    strength1 = StrengthValueEnum.HighZ0;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else if (word.Text == "highz1")
                {
                    strength1 = StrengthValueEnum.HighZ1;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
            }

            if (word.Text != ")")
            {
                word.AddError(") expected");
                return null;
            }
            word.MoveNext();

            if (strength0 == null || strength1 == null) return null;

            return new DriveStrength((StrengthValueEnum)strength0, (StrengthValueEnum)strength1);
        }

        private static StrengthValueEnum? ParseStrengthValue(WordScanner word)
        {
            StrengthValueEnum? ret;
            switch (word.Text)
            {
                case "supply0":
                    ret = StrengthValueEnum.Supply0;
                    break;
                case "supply1":
                    ret = StrengthValueEnum.Supply1;
                    break;
                case "strong0":
                    ret = StrengthValueEnum.Strong0;
                    break;
                case "strong1":
                    ret = StrengthValueEnum.Strong1;
                    break;
                case "pull0":
                    ret = StrengthValueEnum.Pull0;
                    break;
                case "pull1":
                    ret = StrengthValueEnum.Pull1;
                    break;
                case "weak0":
                    ret = StrengthValueEnum.Weak0;
                    break;
                case "weak1":
                    ret = StrengthValueEnum.Weak1;
                    break;
                default:
                    return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return ret;
        }

        public string CreateString()
        {
            string s0 = Strength0.ToString().Replace("HighZ", "highz");
            string s1 = Strength1.ToString().Replace("HighZ", "highz");
            return "(" + s0 + ", " + s1 + ")";
        }
    }
}
