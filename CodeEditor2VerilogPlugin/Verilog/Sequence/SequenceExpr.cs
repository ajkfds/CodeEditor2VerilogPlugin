using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Sequence
{
    public class SequenceExpr : pluginVerilog.Verilog.Property.PropertyPrimary
    {
        /*
        sequence_expr ::=
              cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | sequence_expr cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | expression_or_dist [ boolean_abbrev ]
            | sequence_instance [ sequence_abbrev ]
            | ( sequence_expr {, sequence_match_item } ) [ sequence_abbrev ]
            | sequence_expr "and" sequence_expr
            | sequence_expr "intersect" sequence_expr
            | sequence_expr "or" sequence_expr
            | "first_match (" sequence_expr {, sequence_match_item} ")"
            | expression_or_dist "throughout" sequence_expr
            | sequence_expr "within" sequence_expr
            | clocking_event sequence_expr
        cycle_delay_range ::=
              "##" constant_primary
            | "## [" cycle_delay_const_range_expression "]"
            | "##[*]"
            | "##[+]"
        clocking_event ::=
              "@" identifier
            | "@ (" event_expression ")"
        expression_or_dist ::=
            expression [ "dist {" dist_list "}" ]
        boolean_abbrev ::=
              consecutive_repetition
            | non_consecutive_repetition
            | goto_repetition
        consecutive_repetition ::=
            　[* const_or_range_expression ]
            | [*]
            | [+]
        non_consecutive_repetition ::= 
            [= const_or_range_expression ]
        goto_repetition ::= 
            [-> const_or_range_expression ]
        const_or_range_expression ::=
            constant_expression
            | cycle_delay_const_range_expression
        cycle_delay_const_range_expression ::=
            constant_expression : constant_expression
            | constant_expression : $
        */

        public List<object> SubExpressions { get; set; } = new List<object>();

        public static new SequenceExpr? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SequenceExpr sequenceExpr = new SequenceExpr();
            
            while (!word.Eof)
            {
                // Check for clocking event first (e.g., @clk)
                if (word.Text == "@")
                {
                    var clockingEvent = ParseClockingEvent(word, nameSpace);
                    if (clockingEvent != null)
                    {
                        sequenceExpr.SubExpressions.Add(clockingEvent);
                        continue;
                    }
                }

                // Check for cycle delay range (##, ##[...], ##[*], ##[+])
                var cycleDelay = ParseCycleDelayRange(word, nameSpace);
                if (cycleDelay != null)
                {
                    sequenceExpr.SubExpressions.Add(cycleDelay);
                    continue;
                }

                // Check for first_match(...)
                if (word.Text == "first_match")
                {
                    var firstMatch = ParseFirstMatch(word, nameSpace);
                    if (firstMatch != null)
                    {
                        sequenceExpr.SubExpressions.Add(firstMatch);
                        continue;
                    }
                }

                // Check for parenthesized expression: ( sequence_expr {, sequence_match_item } )
                if (word.Text == "(")
                {
                    var parenExpr = ParseParenthesizedExpression(word, nameSpace);
                    if (parenExpr != null)
                    {
                        sequenceExpr.SubExpressions.Add(parenExpr);
                        // Check for boolean abbreviation after parentheses
                        var boolAbbrev = ParseBooleanAbbreviation(word, nameSpace);
                        if (boolAbbrev != null)
                        {
                            sequenceExpr.SubExpressions.Add(boolAbbrev);
                        }
                        continue;
                    }
                }

                // Check for binary operators: and, or, intersect, throughout, within
                var binaryOp = SequenceBinaryOperator.ParseCreate(word, nameSpace);
                if (binaryOp != null)
                {
                    sequenceExpr.SubExpressions.Add(binaryOp);
                    continue;
                }

                // Check for repetition operators: [*], [=], [->]
                var repetition = SequenceRepetition.ParseCreate(word, nameSpace);
                if (repetition != null)
                {
                    sequenceExpr.SubExpressions.Add(repetition);
                    continue;
                }

                // Check for expression_or_dist (expression with optional dist clause)
                var distExpr = DistExpression.ParseCreate(word, nameSpace);
                if (distExpr != null)
                {
                    sequenceExpr.SubExpressions.Add(distExpr);
                    // Check for boolean abbreviation after expression
                    var boolAbbrev = ParseBooleanAbbreviation(word, nameSpace);
                    if (boolAbbrev != null)
                    {
                        sequenceExpr.SubExpressions.Add(boolAbbrev);
                    }
                    continue;
                }

                // Check for sequence_instance (identifier reference) when expression fails
                // sequence_instance ::= ps_or_hierarchical_sequence_identifier [ ( [ sequence_list_of_arguments ] ) ]
                var sequenceInstance = SequenceInstance.ParseCreate(word, nameSpace);
                if (sequenceInstance != null)
                {
                    sequenceExpr.SubExpressions.Add(sequenceInstance);
                    // Check for sequence abbreviation after sequence instance
                    var seqAbbrev = ParseSequenceAbbreviation(word, nameSpace);
                    if (seqAbbrev != null)
                    {
                        sequenceExpr.SubExpressions.Add(seqAbbrev);
                    }
                    continue;
                }

                // No more valid sequence expression elements
                break;
            }

            if (sequenceExpr.SubExpressions.Count == 0) return null;
            return sequenceExpr;
        }

        /// <summary>
        /// Parse clocking event: @identifier or @(event_expression)
        /// </summary>
        private static object? ParseClockingEvent(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "@") return null;
            var eventControl = Statements.EventControl.ParseCreate(word, nameSpace);
            return eventControl;


/*
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();

                var eventControl = Statements.EventControl.ParseCreate(word, nameSpace);
                if (eventControl != null) 

                // Parse event_expression (simplified - just parse until closing paren)
 //               Expression? expr = Expression.ParseCreate(word, nameSpace);
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                return eventControl;
//                return expr;
            }
            else
            {
                // Simple identifier
                word.Color(CodeDrawStyle.ColorType.Identifier);
                string identifier = word.Text;
                word.MoveNext();
                return identifier;
            }
*/
        }

        /// <summary>
        /// Parse cycle delay range: ##, ##[...], ##[*], ##[+]
        /// </summary>
        private static object? ParseCycleDelayRange(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "##") return null;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            CycleDelayRange range = new CycleDelayRange();

            if (word.Text == "[")
            {
                word.MoveNext();

                if (word.Text == "*")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    range.Type = CycleDelayRangeType.RepeatZeroOrMore;
                }
                else if (word.Text == "+")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    range.Type = CycleDelayRangeType.RepeatOneOrMore;
                }
                else
                {
                    // Parse constant_expression or range
                    Expression? startExpr = Expression.ParseCreate(word, nameSpace);
                    range.StartExpression = startExpr;

                    if (word.Text == ":")
                    {
                        word.MoveNext();
                        Expression? endExpr = null;
                        if (word.Text == "$")
                        {
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            range.IsDollarEnd = true;
                        }
                        else
                        {
                            endExpr = Expression.ParseCreate(word, nameSpace);
                            range.EndExpression = endExpr;
                        }
                        range.Type = CycleDelayRangeType.Range;
                    }
                    else
                    {
                        range.Type = CycleDelayRangeType.SingleValue;
                    }
                }

                if (word.Text == "]")
                {
                    word.MoveNext();
                }
            }
            else
            {
                // Just ## with implicit 1
                range.Type = CycleDelayRangeType.ImplicitOne;
            }

            return range;
        }

        /// <summary>
        /// Parse first_match(...)
        /// </summary>
        private static SequenceExpr? ParseFirstMatch(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "first_match") return null;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            // Parse first sequence expression
            SequenceExpr? firstSeqExpr = ParseCreate(word, nameSpace);
            if (firstSeqExpr == null)
            {
                word.AddError("sequence expression expected");
                if (word.Text == ")") word.MoveNext();
                return null;
            }

            SequenceExpr firstMatchExpr = new SequenceExpr();
            firstMatchExpr.SubExpressions.Add(new FirstMatchMarker());
            foreach (var item in firstSeqExpr.SubExpressions)
            {
                firstMatchExpr.SubExpressions.Add(item);
            }

            // Parse additional sequence expressions and sequence_match_items
            while (word.Text == ",")
            {
                word.MoveNext();
                
                // Try to parse as sequence_match_item first
                var matchItem = SequenceMatchItem.ParseCreate(word, nameSpace);
                if (matchItem != null)
                {
                    firstMatchExpr.SubExpressions.Add(matchItem);
                    continue;
                }

                // Otherwise, parse as sequence expression
                SequenceExpr? innerExpr = ParseCreate(word, nameSpace);
                if (innerExpr == null) break;
                foreach (var item in innerExpr.SubExpressions)
                {
                    firstMatchExpr.SubExpressions.Add(item);
                }
            }

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            return firstMatchExpr;
        }

        /// <summary>
        /// Parse parenthesized sequence expression: ( sequence_expr {, sequence_match_item } )
        /// </summary>
        private static SequenceExpr? ParseParenthesizedExpression(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "(") return null;
            word.MoveNext();

            SequenceExpr parenExpr = new SequenceExpr();
            bool hasContent = false;

            // Parse first sequence expression
            SequenceExpr? innerExpr = ParseCreate(word, nameSpace);
            if (innerExpr != null)
            {
                foreach (var item in innerExpr.SubExpressions)
                {
                    parenExpr.SubExpressions.Add(item);
                }
                hasContent = true;
            }

            // Parse additional items (sequence_match_items separated by commas)
            while (word.Text == ",")
            {
                word.MoveNext();
                
                // Try to parse as sequence_match_item
                var matchItem = SequenceMatchItem.ParseCreate(word, nameSpace);
                if (matchItem != null)
                {
                    parenExpr.SubExpressions.Add(matchItem);
                    hasContent = true;
                    continue;
                }

                // Otherwise, parse as another sequence expression
                SequenceExpr? additionalExpr = ParseCreate(word, nameSpace);
                if (additionalExpr != null)
                {
                    foreach (var item in additionalExpr.SubExpressions)
                    {
                        parenExpr.SubExpressions.Add(item);
                    }
                    hasContent = true;
                }
                else
                {
                    break;
                }
            }

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            if (!hasContent) return null;
            return parenExpr;
        }

        /// <summary>
        /// Parse sequence abbreviation: [* const_or_range_expression]
        /// sequence_abbrev ::= consecutive_repetition
        /// </summary>
        private static SequenceRepetition? ParseSequenceAbbreviation(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "[") return null;
            word.MoveNext();

            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition(RepetitionType.Consecutive, null);
                }
            }
            else if (word.Text == "+")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition(RepetitionType.ConsecutiveAtLeastOne, null);
                }
            }
            else
            {
                // Could be a range expression like [1:5]
                Expression? startExpr = Expression.ParseCreate(word, nameSpace);
                if (startExpr != null && word.Text == ":")
                {
                    word.MoveNext();
                    Expression? endExpr = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == "]")
                    {
                        word.MoveNext();
                        return new SequenceRepetition(RepetitionType.Consecutive, startExpr, endExpr);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parse boolean abbreviation: [*], [+], [=], [->
        /// </summary>
        private static SequenceRepetition? ParseBooleanAbbreviation(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "[") return null;
            word.MoveNext();

            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition(RepetitionType.Consecutive, null);
                }
            }
            else if (word.Text == "+")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition(RepetitionType.ConsecutiveAtLeastOne, null);
                }
            }
            else if (word.Text == "=")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                Expression? count = Expression.ParseCreate(word, nameSpace);
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition(RepetitionType.NonConsecutive, count);
                }
            }
            else if (word.Text == "-")
            {
                word.MoveNext();
                if (word.Text == ">")
                {
                    word.MoveNext();
                    Expression? count = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == "]")
                    {
                        word.MoveNext();
                        return new SequenceRepetition(RepetitionType.Goto, count);
                    }
                }
            }
            else
            {
                // Could be a range expression like [1:5]
                Expression? startExpr = Expression.ParseCreate(word, nameSpace);
                if (startExpr != null && word.Text == ":")
                {
                    word.MoveNext();
                    Expression? endExpr = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == "]")
                    {
                        word.MoveNext();
                        return new SequenceRepetition(RepetitionType.Consecutive, startExpr, endExpr);
                    }
                }
            }

            // Restore position if parsing failed
            // Note: This is a simplified recovery - a full implementation would need better state management
            return null;
        }

        public class CycleDelayRange
        {
            public CycleDelayRangeType Type { get; set; } = CycleDelayRangeType.ImplicitOne;
            public Expression? StartExpression { get; set; }
            public Expression? EndExpression { get; set; }
            public bool IsDollarEnd { get; set; } = false;
        }

        public enum CycleDelayRangeType
        {
            ImplicitOne,
            SingleValue,
            Range,
            RepeatZeroOrMore,
            RepeatOneOrMore
        }
    }

    /// <summary>
    /// Binary operators for sequence expressions: and, or, intersect, throughout, within
    /// </summary>
    public class SequenceBinaryOperator
    {
        public string Operator { get; set; } = "";
        public byte Precedence { get; set; }

        public static SequenceBinaryOperator? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "and":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new SequenceBinaryOperator { Operator = "and", Precedence = 1 };
                case "or":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new SequenceBinaryOperator { Operator = "or", Precedence = 0 };
                case "intersect":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new SequenceBinaryOperator { Operator = "intersect", Precedence = 2 };
                case "throughout":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new SequenceBinaryOperator { Operator = "throughout", Precedence = 4 };
                case "within":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new SequenceBinaryOperator { Operator = "within", Precedence = 3 };
            }
            return null;
        }
    }

    /// <summary>
    /// Repetition operators: [*], [+], [=], [->
    /// </summary>
    public class SequenceRepetition
    {
        public RepetitionType Type { get; set; }
        public Expression? Count { get; set; }
        public Expression? EndCount { get; set; } // For range like [1:5]

        public static SequenceRepetition? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "[") return null;
            word.MoveNext();

            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition { Type = RepetitionType.Consecutive };
                }
            }
            else if (word.Text == "+")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition { Type = RepetitionType.ConsecutiveAtLeastOne };
                }
            }
            else if (word.Text == "=")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                Expression? count = Expression.ParseCreate(word, nameSpace);
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new SequenceRepetition { Type = RepetitionType.NonConsecutive, Count = count };
                }
            }
            else if (word.Text == "-")
            {
                word.MoveNext();
                if (word.Text == ">")
                {
                    word.MoveNext();
                    Expression? count = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == "]")
                    {
                        word.MoveNext();
                        return new SequenceRepetition { Type = RepetitionType.Goto, Count = count };
                    }
                }
            }
            else
            {
                // Could be a range expression like [1:5]
                Expression? startExpr = Expression.ParseCreate(word, nameSpace);
                if (startExpr != null && word.Text == ":")
                {
                    word.MoveNext();
                    Expression? endExpr = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == "]")
                    {
                        word.MoveNext();
                        return new SequenceRepetition { Type = RepetitionType.Consecutive, Count = startExpr, EndCount = endExpr };
                    }
                }
            }

            // Reset position if parsing failed - this is a limitation
            return null;
        }

        public SequenceRepetition() { }
        public SequenceRepetition(RepetitionType type, Expression? count, Expression? endCount = null)
        {
            Type = type;
            Count = count;
            EndCount = endCount;
        }
    }

    public enum RepetitionType
    {
        Consecutive,
        ConsecutiveAtLeastOne,
        NonConsecutive,
        Goto
    }

    /// <summary>
    /// Marker for first_match(...) construct
    /// </summary>
    public class FirstMatchMarker { }
}
