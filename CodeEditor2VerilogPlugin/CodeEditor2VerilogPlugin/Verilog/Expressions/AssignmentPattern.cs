using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static pluginVerilog.Verilog.Expressions.AssignmentPatternWithKeys;

namespace pluginVerilog.Verilog.Expressions
{
    public class AssignmentPattern : Primary
    {
        /*
        assignment_pattern ::=    "'{" expression { , expression } "}"
                                | "'{" structure_pattern_key : expression { , structure_pattern_key ":" expression } "}"
                                | "'{" array_pattern_key ":" expression { "," array_pattern_key ":" expression } "}"
                                | "'{" constant_expression "{" expression { , expression } "}" "}"

        structure_pattern_key   ::= member_identifier | assignment_pattern_key
        array_pattern_key       ::= constant_expression | assignment_pattern_key
        assignment_pattern_key  ::= simple_type | "default"

        assignment_pattern_expression ::=       [ assignment_pattern_expression_type ] assignment_pattern
        assignment_pattern_expression_type ::=    ps_type_identifier
                                                | ps_parameter_identifier
                                                | integer_atom_type
                                                | type_reference
        constant_assignment_pattern_expression ::= assignment_pattern_expression

        assignment_pattern_net_lvalue       ::=  "'{" net_lvalue {, net_lvalue } "}"
        assignment_pattern_variable_lvalue  ::=  "'{" variable_lvalue {, variable_lvalue } "}"
        */
        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue)
        {
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Text != "{") throw new Exception();
            word.MoveNext();

            AssignmentPattern assignmentPattern;
            if (word.NextText == ":")
            {
                assignmentPattern = AssignmentPatternWithKeys.parseCreate(word, nameSpace);
            }
            else
            {
                assignmentPattern = AsssignmentPatternWithValues.parseCreate(word, nameSpace);
            }


                while (word.Text != "}" & !word.Eof)
                {
                    word.MoveNext();
                }
            if (word.Text != "}")
            {
                word.AddError("illegal assignment pattern");
            }
            else
            {
                word.MoveNext();
            }

            return assignmentPattern;
        }

        public void CheckPattern()
        {

        }
    }

    public class AssignmentPatternWithKeys : AssignmentPattern
    {
        public List<KeyExpressionPair> Items = new List<KeyExpressionPair>();
        internal static AssignmentPatternWithKeys parseCreate(WordScanner word, NameSpace nameSpace)
        {
            var assignmentPattern = new AssignmentPatternWithKeys();
            while (!word.Eof)
            {
                if (word.NextText != ":") break;
                var item = new KeyExpressionPair() { Key = word.Text,KeyReference= word.CrateWordReference() };
                if (word.Text == "default") word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text != ":") throw new Exception();
                word.MoveNext();

                Expression? expression = Expression.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    word.AddError("illegal assignment pattern");
                    break;
                }
                item.Expression = expression;
                assignmentPattern.Items.Add(item);
                if (word.Text == "}") break;
            }
            return assignmentPattern;
        }
        public class KeyExpressionPair()
        {
            public string Key;
            public WordReference KeyReference;
            public Expression Expression;
        }
    }

    public class AsssignmentPatternWithValues : AssignmentPattern
    {
        public List<Expression> Items = new List<Expression>();

        internal static AsssignmentPatternWithValues parseCreate(WordScanner word, NameSpace nameSpace)
        {
            var assignmentPattern = new AsssignmentPatternWithValues();
            while (!word.Eof)
            {
                Expression? expression = Expression.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    word.AddError("illegal assignment pattern");
                    break;
                }
                assignmentPattern.Items.Add(expression);
                if (word.Text == "}") break;
            }
            return assignmentPattern;
        }

    }
}
