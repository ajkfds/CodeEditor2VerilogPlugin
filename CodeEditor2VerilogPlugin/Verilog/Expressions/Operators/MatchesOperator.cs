using pluginVerilog.Verilog.DataObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    /// <summary>
    /// SystemVerilog 12.6 pattern matching operator.
    ///   expression_or_dist ::= expression | expression "dist" "{" dist_list "}"
    ///   expression_or_dist matches pattern (used in if / conditional expressions)
    /// The right-hand side is a pattern (constant_expression | assignment_pattern | tagged_pattern).
    /// </summary>
    public class MatchesOperator : Operator
    {
        protected MatchesOperator(WordScanner word, string text, byte precedence, NameSpace nameSpace) : base(text, precedence)
        {
            if (word.Text != "matches") throw new Exception();
            Reference = word.GetReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // pattern ::= constant_expression | assignment_pattern | tagged_pattern
            Expression? pattern = CasePatternParser.ParsePattern(word, nameSpace);
            if (pattern != null)
            {
                Pattern = pattern;
            }
        }

        public Expression? Pattern;

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Primary?.DisposeSubReference(false);
            Pattern?.DisposeSubReference(false);
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (Primary != null) Primary.AppendLabel(label);
            label.AppendText(" matches ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            Pattern?.AppendLabel(label);
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            Primary?.AppendString(stringBuilder);
            stringBuilder.Append(" matches ");
            Pattern?.AppendString(stringBuilder);
        }

        public override void AppendRefrencedDataObjects(List<DataObject> referencedObjects)
        {
            Primary?.AppendRefrencedDataObjects(referencedObjects);
            Pattern?.AppendRefrencedDataObjects(referencedObjects);
        }

        public static new MatchesOperator? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "matches") return null;
            return new MatchesOperator(word, "matches", 8, nameSpace);
        }

        public delegate void OperatedAction(MatchesOperator op);
        public static OperatedAction? Operated;

        public MatchesOperator Operate(Primary primary, bool prototype)
        {
            Primary = primary;
            // matches yields a 1-bit boolean result
            Constant = primary.Constant;
            Value = null;
            BitWidth = 1;
            if (Primary.Reference != null)
            {
                Reference = Primary.Reference;
            }
            if (Operated != null) Operated(this);
            return this;
        }
    }
}
