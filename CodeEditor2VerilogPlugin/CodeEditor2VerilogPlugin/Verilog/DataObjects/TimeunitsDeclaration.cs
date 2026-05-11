using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Expressions;
using System;

namespace pluginVerilog.Verilog.DataObjects
{
    /// <summary>
    /// Timeunits Declaration
    /// IEEE 1800-2017 SystemVerilog
    /// 
    /// timeunits_declaration ::=
    ///     "timeunit" time_literal [ "/" time_precision ] ;
    ///   | "timeunit" time_literal ;
    ///   | "timeprecision" time_precision ;
    /// 
    /// time_literal ::= time_number unit
    /// time_precision ::= time_number
    /// 
    /// unit ::= s | ms | us | ns | ps | fs
    /// 
    /// The timeunits declaration specifies the time unit and precision for the design.
    /// </summary>
    public class TimeunitsDeclaration : INamedElement
    {
        public string Name { get; set; } = "timeunits";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Time unit value (e.g., 1, 10, 100)
        /// </summary>
        public Expression? TimeUnitValue { get; set; }

        /// <summary>
        /// Time unit (s, ms, us, ns, ps, fs)
        /// </summary>
        public string TimeUnit { get; set; } = "";

        /// <summary>
        /// Optional time precision value
        /// </summary>
        public Expression? TimePrecisionValue { get; set; }

        /// <summary>
        /// Optional time precision unit
        /// </summary>
        public string TimePrecisionUnit { get; set; } = "";

        /// <summary>
        /// Index reference for begin
        /// </summary>
        public IndexReference BeginIndexReference { get; set; }

        /// <summary>
        /// Index reference for end
        /// </summary>
        public IndexReference? LastIndexReference { get; set; }

        private static readonly string[] validUnits = { "s", "ms", "us", "ns", "ps", "fs" };

        public static TimeunitsDeclaration? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            TimeunitsDeclaration? result = null;

            if (word.Text == "timeunit")
            {
                result = ParseTimeunit(word, nameSpace);
            }
            else if (word.Text == "timeprecision")
            {
                result = ParseTimeprecision(word, nameSpace);
            }

            return result;
        }

        private static TimeunitsDeclaration ParseTimeunit(WordScanner word, NameSpace nameSpace)
        {
            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // timeunit

            TimeunitsDeclaration decl = new TimeunitsDeclaration
            {
                BeginIndexReference = beginReference
            };

            // Parse time unit value and unit
            if (!ParseTimeValueAndUnit(word, nameSpace, out decl.TimeUnitValue, out decl.TimeUnit))
            {
                word.AddError("illegal timeunit");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return decl;
            }

            // Optional / time_precision
            if (word.Text == "/")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (!ParseTimeValueAndUnit(word, nameSpace, out decl.TimePrecisionValue, out decl.TimePrecisionUnit))
                {
                    word.AddError("illegal timeprecision");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return decl;
                }
            }

            // Semicolon
            if (word.Text != ";")
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return decl;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            decl.LastIndexReference = word.CreateIndexReference();
            return decl;
        }

        private static TimeunitsDeclaration ParseTimeprecision(WordScanner word, NameSpace nameSpace)
        {
            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // timeprecision

            TimeunitsDeclaration decl = new TimeunitsDeclaration
            {
                BeginIndexReference = beginReference,
                Name = "timeprecision"
            };

            // Parse time precision value and unit
            if (!ParseTimeValueAndUnit(word, nameSpace, out decl.TimePrecisionValue, out decl.TimePrecisionUnit))
            {
                word.AddError("illegal timeprecision");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return decl;
            }

            // Semicolon
            if (word.Text != ";")
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return decl;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            decl.LastIndexReference = word.CreateIndexReference();
            return decl;
        }

        private static bool ParseTimeValueAndUnit(WordScanner word, NameSpace nameSpace, out Expression? value, out string unit)
        {
            value = null;
            unit = "";

            // Parse time value (number)
            value = Expressions.Expression.ParseCreate(word, nameSpace, false);
            if (value == null)
            {
                return false;
            }

            // Parse unit (s, ms, us, ns, ps, fs)
            if (!IsValidUnit(word.Text))
            {
                return false;
            }

            unit = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            return true;
        }

        private static bool IsValidUnit(string text)
        {
            foreach (string u in validUnits)
            {
                if (text == u) return true;
            }
            return false;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }

        public void DisposeSubReference()
        {
            TimeUnitValue?.DisposeSubReference(true);
            TimePrecisionValue?.DisposeSubReference(true);
        }
    }
}
