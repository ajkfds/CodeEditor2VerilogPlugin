using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.Statements;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Sequence
{
    /// <summary>
    /// Represents a sequence match item for use in first_match() and parenthesized sequence expressions.
    /// 
    /// sequence_match_item ::=
    ///     operator_assignment
    ///   | inc_or_dec_expression
    ///   | subroutine_call
    /// 
    /// operator_assignment ::= variable_lvalue = expression
    /// </summary>
    public class SequenceMatchItem
    {
        public SequenceMatchItemType Type { get; set; }
        public Expression? Expression { get; set; }
        public IncOrDecExpression? IncDecExpression { get; set; }
        public DataObjectReference? VariableReference { get; set; } // For operator_assignment

        public enum SequenceMatchItemType
        {
            OperatorAssignment,
            IncOrDecExpression,
            Expression
        }

        /// <summary>
        /// Parse a sequence match item
        /// </summary>
        public static SequenceMatchItem? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Eof) return null;

            // Try to parse inc_or_dec_expression first (++i, i--, etc.)
            var incDec = IncOrDecExpression.ParseCreate(word, nameSpace, true);
            if (incDec != null)
            {
                return new SequenceMatchItem
                {
                    Type = SequenceMatchItemType.IncOrDecExpression,
                    IncDecExpression = incDec
                };
            }

            // Try to parse operator_assignment: variable_lvalue = expression
            // This is typically a DataObjectReference followed by =
            var variableRef = Primary.ParseCreate(word, nameSpace, true) as DataObjectReference;
            if (variableRef != null && word.Text == "=")
            {
                word.MoveNext(); // =

                Expression? expr = Expression.ParseCreate(word, nameSpace);
                if (expr != null)
                {
                    return new SequenceMatchItem
                    {
                        Type = SequenceMatchItemType.OperatorAssignment,
                        VariableReference = variableRef,
                        Expression = expr
                    };
                }
            }

            // Otherwise, try to parse as an expression (function calls, etc.)
            var expr2 = Expression.ParseCreate(word, nameSpace);
            if (expr2 != null)
            {
                return new SequenceMatchItem
                {
                    Type = SequenceMatchItemType.Expression,
                    Expression = expr2
                };
            }

            return null;
        }
    }
}
