using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static pluginVerilog.Verilog.DataObjects.AssignmentPatternWithKey;

namespace pluginVerilog.Verilog.DataObjects
{
    public class AssignmentPattern : Expressions.Expression
    {
        /*
        assignment_pattern ::=
              '{ expression { , expression } }
            | '{ structure_pattern_key : expression { , structure_pattern_key : expression } }
            | '{ array_pattern_key : expression { , array_pattern_key : expression } }
            | '{ constant_expression { expression { , expression } } }
        
        structure_pattern_key ::= member_identifier | assignment_pattern_key
        
        array_pattern_key ::= constant_expression | assignment_pattern_key
        
        assignment_pattern_key ::= simple_type | "default"

        assignment_pattern_expression ::=
            [ assignment_pattern_expression_type ] assignment_pattern

        assignment_pattern_expression_type ::=
              ps_type_identifier
            | ps_parameter_identifier
            | integer_atom_type
            | type_reference

        constant_assignment_pattern_expression ::= assignment_pattern_expression
         
        simple_type ::= integer_type | non_integer_type | ps_type_identifier | ps_parameter_identifier
         */

        protected AssignmentPattern()
        {

        }
        public static new AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, false);
        }
        public static AssignmentPattern ParseCreate(WordScanner word,NameSpace nameSpace, bool lValue)
        {
            AssignmentPattern assignmentPattern;

            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Text != "{") throw new Exception();
            word.MoveNext();

            if (word.NextText == ":")
            {
                if(lValue == true)
                {
                    word.AddError("assignment pattern cannot used for left side of assignment");
                }
                assignmentPattern = AssignmentPatternWithKey.parseCreate(word, nameSpace);
            }
            else
            {
                assignmentPattern = AssignmentPatternWithoutKey.parseCreate(word, nameSpace);
            }

            if (word.Text == "}")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("} required");
            }
            return assignmentPattern;
        }

    }

    public class AssignmentPatternWithKey : AssignmentPattern
    {
        public List<KeyExpression> KeyExpressions = new List<KeyExpression>();
        public class KeyExpression
        {
            public required string Key;
            public required WordReference KeyReference;
            public required Expressions.Expression Expression;
        }
        public static AssignmentPatternWithKey parseCreate(WordScanner word, NameSpace nameSpace)
        {
            AssignmentPatternWithKey assignmentPattern = new AssignmentPatternWithKey();

            while(!word.Eof & word.Text != "}")
            {
                string key = word.Text;
                WordReference keyReference = word.GetReference();
                if (key == "default") word.Color(CodeDrawStyle.ColorType.Keyword);

                word.MoveNext();

                if (word.Text != ":")
                {
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }
                word.MoveNext();

                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);

                if (expression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }
                KeyExpression keyExpression = new KeyExpression() { Key = key, KeyReference = keyReference, Expression = expression };

                assignmentPattern.KeyExpressions.Add(keyExpression);

                if (word.Text != ",") break;
                word.MoveNext();
            }

            return assignmentPattern;
        }

    }

    public class AssignmentPatternWithoutKey : AssignmentPattern
    {
        List<Expressions.Expression> Expressions = new List<Expressions.Expression>();

        public static AssignmentPatternWithoutKey parseCreate(WordScanner word, NameSpace nameSpace)
        {
            AssignmentPatternWithoutKey assignmentPattern = new AssignmentPatternWithoutKey();

            while (!word.Eof & word.Text != "}")
            {
                string key = word.Text;
                WordReference keyReference = word.GetReference();

                Expressions.Expression? expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);

                if (expression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }

                assignmentPattern.Expressions.Add(expression);

                if (word.Text != ",") break;
                word.MoveNext();
            }

            return assignmentPattern;
        }

    }
}
