using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    /// <summary>
    /// overload_declaration (IEEE 1800-2017 A.2.1.2)
    ///
    /// overload_declaration ::= "function" "new" { "," } ... | 
    ///   overload_declaration ::= "function" binary_operator "with" function_identifier ";"
    ///   binary_operator ::= "+" | "-" | "*" | "/" | "%" | "==" | "!=" | "<" | "<=" | ">" | ">=" | "&&" | "||" | "===" | "!==" | "&" | "|" | "^" | "<<" | ">>"
    ///
    /// Declares an overload of a built-in binary operator for a given type.
    /// The declaration binds the operator to an existing function; the parser only
    /// consumes it and records the bound function name.
    /// </summary>
    public class OverloadDeclaration
    {
        public string Operator { get; set; } = "";
        public string FunctionIdentifier { get; set; } = "";

        private static readonly HashSet<string> BinaryOperators = new HashSet<string>
        {
            "+", "-", "*", "/", "%", "==", "!=", "<", "<=", ">", ">=",
            "&&", "||", "===", "!==", "&", "|", "^", "<<", ">>"
        };

        public static bool IsBinaryOperator(string text)
        {
            return text != null && BinaryOperators.Contains(text);
        }

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "function") return false;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            OverloadDeclaration overload = new OverloadDeclaration();

            // binary_operator
            overload.Operator = word.Text;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "with")
            {
                word.AddError("with expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("function identifier expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            overload.FunctionIdentifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
            }

            return true;
        }
    }
}
