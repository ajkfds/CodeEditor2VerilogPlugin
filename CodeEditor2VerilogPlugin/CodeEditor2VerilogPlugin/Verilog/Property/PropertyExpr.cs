using pluginVerilog.Verilog.Sequence;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Property
{
    public class PropertyExpr
    {
        /*
        property_expr ::=
              sequence_expr
            | strong ( sequence_expr )
            | weak ( sequence_expr )
            | ( property_expr )
            | not property_expr
            | property_expr or property_expr
            | property_expr and property_expr
            | sequence_expr |-> property_expr
            | sequence_expr |=> property_expr
            | if ( expression_or_dist ) property_expr [ else property_expr ]
            | case ( expression_or_dist ) property_case_item { property_case_item } endcase
            | sequence_expr #-# property_expr
            | sequence_expr #=# property_expr
            | nexttime property_expr
            | nexttime [ constant _expression ] property_expr
            | s_nexttime property_expr
            | s_nexttime [ constant_expression ] property_expr
            | always property_expr
            | always [ cycle_delay_const_range_expression ] property_expr
            | s_always [ constant_range ] property_expr
            | s_eventually property_expr
            | eventually [ constant_range ] property_expr
            | s_eventually [ cycle_delay_const_range_expression ] property_expr
            | property_expr until property_expr
            | property_expr s_until property_expr
            | property_expr until_with property_expr
            | property_expr s_until_with property_expr
            | property_expr implies property_expr
            | property_expr iff property_expr
            | accept_on ( expression_or_dist ) property_expr
            | reject_on ( expression_or_dist ) property_expr
            | sync_accept_on ( expression_or_dist ) property_expr
            | sync_reject_on ( expression_or_dist ) property_expr
            | property_instance
            | clocking_event property_expr
        property_case_item ::=
            expression_or_dist { , expression_or_dist } : property_expr ;
            | default [ : ] property_expr ;
        assertion_variable_declaration ::=
            var_data_type list_of_variable_decl_assignments ;
        property_instance ::=
            ps_or_hierarchical_property_identifier [ ( [ property_list_of_arguments ] ) ]
        property_list_of_arguments ::=
            [property_actual_arg] { , [property_actual_arg] } { , . identifier ( [property_actual_arg] ) }
            | . identifier ( [property_actual_arg] ) { , . identifier ( [property_actual_arg] ) }
        property_actual_arg ::=
            property_expr
            | sequence_actual_arg         
         */

        public List<object> SubExpressions { get; set; } = new List<object>();

        public static PropertyExpr? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            PropertyExpr propertyExpr = new PropertyExpr();
            
            while (!word.Eof)
            {
                // Check for strong/weak wrappers
                if (word.Text == "strong" || word.Text == "weak")
                {
                    var wrapper = ParseStrongWeak(word, nameSpace);
                    if (wrapper != null)
                    {
                        propertyExpr.SubExpressions.Add(wrapper);
                        continue;
                    }
                }

                // Check for clocking event
                if (word.Text == "@")
                {
                    var clockingEvent = ParseClockingEvent(word, nameSpace);
                    if (clockingEvent != null)
                    {
                        propertyExpr.SubExpressions.Add(clockingEvent);
                        continue;
                    }
                }

                // Check for temporal operators: not, nexttime, s_nexttime, always, s_always, eventually, s_eventually
                var temporalOp = PropertyOperator.ParseCreate(word, nameSpace);
                if (temporalOp != null)
                {
                    propertyExpr.SubExpressions.Add(temporalOp);
                    continue;
                }

                // Check for implication operators: |->, |=>, #-#, #=#
                var implicationOp = ParseImplicationOperator(word);
                if (implicationOp != null)
                {
                    propertyExpr.SubExpressions.Add(implicationOp);
                    continue;
                }

                // Check for if-else
                if (word.Text == "if")
                {
                    var ifElse = ParseIfElse(word, nameSpace);
                    if (ifElse != null)
                    {
                        propertyExpr.SubExpressions.Add(ifElse);
                        continue;
                    }
                }

                // Check for case statement
                if (word.Text == "case")
                {
                    var caseExpr = ParseCaseExpr(word, nameSpace);
                    if (caseExpr != null)
                    {
                        propertyExpr.SubExpressions.Add(caseExpr);
                        continue;
                    }
                }

                // Check for accept_on, reject_on, sync_accept_on, sync_reject_on
                var acceptReject = ParseAcceptRejectOn(word, nameSpace);
                if (acceptReject != null)
                {
                    propertyExpr.SubExpressions.Add(acceptReject);
                    continue;
                }

                // Check for binary operators: and, or, iff, until, s_until, until_with, s_until_with, implies
                var binaryOp = ParseBinaryOperator(word);
                if (binaryOp != null)
                {
                    propertyExpr.SubExpressions.Add(binaryOp);
                    continue;
                }

                // Check for parenthesized property expression
                if (word.Text == "(")
                {
                    var parenExpr = ParseParenthesizedPropertyExpr(word, nameSpace);
                    if (parenExpr != null)
                    {
                        propertyExpr.SubExpressions.Add(parenExpr);
                        continue;
                    }
                }

                // Parse sequence expression (base case)
                var sequenceExpr = SequenceExpr.ParseCreate(word, nameSpace);
                if (sequenceExpr != null)
                {
                    propertyExpr.SubExpressions.Add(sequenceExpr);
                    continue;
                }

                // No more valid property expression elements
                break;
            }

            if (propertyExpr.SubExpressions.Count == 0) return null;
            return propertyExpr;
        }

        private static object? ParseStrongWeak(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "strong" && word.Text != "weak") return null;
            string op = word.Text;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            var innerExpr = ParseCreate(word, nameSpace);

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            return new StrongWeakWrapper { Operator = op, InnerExpression = innerExpr };
        }

        private static object? ParseClockingEvent(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "@") return null;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                // Parse event_expression
                Expressions.Expression? expr = Expressions.Expression.ParseCreate(word, nameSpace);
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                return expr;
            }
            else
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                string identifier = word.Text;
                word.MoveNext();
                return identifier;
            }
        }

        private static object? ParseImplicationOperator(WordScanner word)
        {
            string op = word.Text;
            switch (op)
            {
                case "|->":
                case "|=>":
                case "#-#":
                case "#=#":
                    word.MoveNext();
                    return new PropertyOperator(op, 1);
            }
            return null;
        }

        private static object? ParseIfElse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "if") return null;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            var condition = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            var thenExpr = ParseCreate(word, nameSpace);

            object? elseExpr = null;
            if (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                elseExpr = ParseCreate(word, nameSpace);
            }

            return new IfElseExpr { Condition = condition, ThenExpression = thenExpr, ElseExpression = elseExpr };
        }

        private static object? ParseCaseExpr(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "case") return null;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            var caseExpr = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            // Parse case items
            List<CaseItem> caseItems = new List<CaseItem>();
            while (!word.Eof && word.Text != "endcase")
            {
                var caseItem = ParseCaseItem(word, nameSpace);
                if (caseItem != null)
                {
                    caseItems.Add(caseItem);
                }
                else
                {
                    break;
                }
            }

            if (word.Text == "endcase")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return new CaseExpr { Expression = caseExpr, CaseItems = caseItems };
        }

        private static CaseItem? ParseCaseItem(WordScanner word, NameSpace nameSpace)
        {
            List<Expressions.Expression> values = new List<Expressions.Expression>();
            
            while (!word.Eof && word.Text != "default" && word.Text != "endcase")
            {
                var value = Expressions.Expression.ParseCreate(word, nameSpace);
                if (value == null) return null;
                values.Add(value);

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
                break;
            }

            if (word.Text == ":")
            {
                word.MoveNext();
            }

            var propExpr = ParseCreate(word, nameSpace);

            if (word.Text == ";")
            {
                word.MoveNext();
            }

            if (word.Text == "default")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == ":")
                {
                    word.MoveNext();
                }
                var defaultExpr = ParseCreate(word, nameSpace);
                if (word.Text == ";")
                {
                    word.MoveNext();
                }
                return new CaseItem { IsDefault = true, DefaultExpression = defaultExpr };
            }

            return new CaseItem { Values = values, PropertyExpression = propExpr };
        }

        private static object? ParseAcceptRejectOn(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "accept_on":
                case "reject_on":
                case "sync_accept_on":
                case "sync_reject_on":
                    string op = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    if (word.Text != "(")
                    {
                        word.AddError("( expected");
                        return null;
                    }
                    word.MoveNext();

                    var condition = Expressions.Expression.ParseCreate(word, nameSpace);

                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }

                    var innerExpr = ParseCreate(word, nameSpace);

                    return new AcceptRejectOnExpr { Operator = op, Condition = condition, InnerExpression = innerExpr };
            }
            return null;
        }

        private static object? ParseBinaryOperator(WordScanner word)
        {
            switch (word.Text)
            {
                case "and":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new PropertyOperator("and", 5);
                case "or":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new PropertyOperator("or", 4);
                case "iff":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new PropertyOperator("iff", 3);
                case "until":
                case "s_until":
                case "until_with":
                case "s_until_with":
                case "implies":
                    string op = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return new PropertyOperator(op, 2);
            }
            return null;
        }

        private static object? ParseParenthesizedPropertyExpr(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "(") return null;
            word.MoveNext();

            var innerExpr = ParseCreate(word, nameSpace);

            if (word.Text == ")")
            {
                word.MoveNext();
            }

            return innerExpr;
        }
    }

    public class StrongWeakWrapper
    {
        public string Operator { get; set; } = "";
        public PropertyExpr? InnerExpression { get; set; }
    }

    public class IfElseExpr
    {
        public Expressions.Expression? Condition { get; set; }
        public PropertyExpr? ThenExpression { get; set; }
        public object? ElseExpression { get; set; }
    }

    public class CaseExpr
    {
        public Expressions.Expression? Expression { get; set; }
        public List<CaseItem> CaseItems { get; set; } = new List<CaseItem>();
    }

    public class CaseItem
    {
        public List<Expressions.Expression> Values { get; set; } = new List<Expressions.Expression>();
        public PropertyExpr? PropertyExpression { get; set; }
        public bool IsDefault { get; set; } = false;
        public PropertyExpr? DefaultExpression { get; set; }
    }

    public class AcceptRejectOnExpr
    {
        public string Operator { get; set; } = "";
        public Expressions.Expression? Condition { get; set; }
        public PropertyExpr? InnerExpression { get; set; }
    }
}
