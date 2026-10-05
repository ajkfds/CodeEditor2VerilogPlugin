using System;
using System.Collections.Generic;
using System.Text;

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
        public static new AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue)
        {
            return ParseCreate(word, nameSpace, lValue, false);
        }

        /// <summary>
        /// patternMode : accept pattern elements (".v" pattern variables / nested tagged patterns)
        /// used in case ... matches patterns (SystemVerilog 12.6)
        /// </summary>
        public static AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue, bool patternMode)
        {
            AssignmentPattern assignmentPattern;

            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Text != "{") throw new Exception();
            word.MoveNext();

            if (word.NextText == ":")
            {
                if (lValue == true)
                {
                    word.AddError("assignment pattern cannot used for left side of assignment");
                }
                assignmentPattern = AssignmentPatternWithKey.parseCreate(word, nameSpace, patternMode);
            }
            else
            {
                assignmentPattern = AssignmentPatternWithoutKey.parseCreate(word, nameSpace, patternMode);
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

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("'{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            if (this is AssignmentPatternWithKey withKey)
            {
                bool first = true;
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    if (!first) label.AppendText(", ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    first = false;
                    if (keyExpression.Key == "default") label.AppendText("default", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    else label.AppendText(keyExpression.Key, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
                    label.AppendText(" : ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    keyExpression.Expression?.AppendLabel(label);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                bool first = true;
                foreach (var expression in withoutKey.Expressions)
                {
                    if (!first) label.AppendText(", ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    first = false;
                    expression?.AppendLabel(label);
                }
            }
            label.AppendText("}", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            AppendString(sb);
            return sb.ToString();
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append("'{");
            if (this is AssignmentPatternWithKey withKey)
            {
                bool first = true;
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    if (!first) stringBuilder.Append(", ");
                    first = false;
                    stringBuilder.Append(keyExpression.Key + " : ");
                    keyExpression.Expression?.AppendString(stringBuilder);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                bool first = true;
                foreach (var expression in withoutKey.Expressions)
                {
                    if (!first) stringBuilder.Append(", ");
                    first = false;
                    expression?.AppendString(stringBuilder);
                }
            }
            stringBuilder.Append("}");
        }

    /// <summary>
    /// repetition element of assignment pattern : '{ count { element } }
    /// e.g. '{4{1'b0}}
    /// </summary>
    public class RepeatedExpression : Expressions.Expression
    {
        public required Expressions.Expression Count;
        public required Expressions.Expression Element;

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("'{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            Count?.AppendLabel(label);
            label.AppendText("{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            Element?.AppendLabel(label);
            label.AppendText("}", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            AppendString(sb);
            return sb.ToString();
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append("'{");
            Count?.AppendString(stringBuilder);
            stringBuilder.Append("{");
            Element?.AppendString(stringBuilder);
            stringBuilder.Append("}");
        }

        public override void AppendRefrencedDataObjects(List<DataObject> referencedObjects)
        {
            Count?.AppendRefrencedDataObjects(referencedObjects);
            Element?.AppendRefrencedDataObjects(referencedObjects);
        }
    }

        public override void AppendRefrencedDataObjects(List<DataObject> referencedObjects)
        {
            if (this is AssignmentPatternWithKey withKey)
            {
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    keyExpression.Expression?.AppendRefrencedDataObjects(referencedObjects);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                foreach (var expression in withoutKey.Expressions)
                {
                    expression?.AppendRefrencedDataObjects(referencedObjects);
                }
            }
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
        public static AssignmentPatternWithKey parseCreate(WordScanner word, NameSpace nameSpace, bool patternMode = false)
        {
            AssignmentPatternWithKey assignmentPattern = new AssignmentPatternWithKey();

            while (!word.Eof & word.Text != "}")
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

               if (expression == null && patternMode)
               {
                   // key : pattern_value form in case pattern (e.g. default : .v)
                   expression = Verilog.Expressions.CasePatternParser.ParsePattern(word, nameSpace);
               }

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
        public List<Expressions.Expression> Expressions = new List<Expressions.Expression>();

        public static AssignmentPatternWithoutKey parseCreate(WordScanner word, NameSpace nameSpace, bool patternMode = false)
        {
            AssignmentPatternWithoutKey assignmentPattern = new AssignmentPatternWithoutKey();

            while (!word.Eof & word.Text != "}")
            {
                string key = word.Text;
                WordReference keyReference = word.GetReference();

                Expressions.Expression? expression = null;
               if (patternMode)
               {
                   // case pattern element : .v (pattern variable) / tagged ... / constant expression
                   expression = Verilog.Expressions.CasePatternParser.ParsePattern(word, nameSpace);
               }
               else
               {
                   expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
               }

               // assignment_pattern ::= '{ constant_expression { expression { , expression } } }
               // repetition form : e.g. '{4{1'b0}} / '{N{default}}
               if (!patternMode && expression != null && word.Text == "{")
               {
                   word.MoveNext();
                   Expressions.Expression? count = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                   if (count == null)
                   {
                       word.AddError("illegal repetition constant expression");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   if (word.Text != "}")
                   {
                       word.AddError("} required");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   word.MoveNext();
                   expression = new RepeatedExpression() { Count = count, Element = expression };
               }

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
