using CodeEditor2.CodeEditor.CodeComplete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class CaseStatement : IStatement
    {
        protected CaseStatement() { }
        public Expressions.Expression Expression;
        public List<CaseItem> CaseItems = new List<CaseItem>();
        public bool IsInsideMode { get; set; } = false;
        public bool IsMatchesMode { get; set; } = false;

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            foreach (CaseItem caseItem in CaseItems)
            {
                caseItem.DisposeSubRefrence();
            }
        }
        public static CaseStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            /*
            case_statement ::=  case ( expression ) case_item { case_item } endcase          
                                | casez ( expression ) case_item { case_item } endcase
                                | casex ( expression ) case_item { case_item } endcase
            case_item ::=       expression { , expression } : statement_or_null 
                                | default [ : ] statement_or_null
            function_case_statement ::= case ( expression ) function_case_item { function_case_item } endcase      
                                        | casez ( expression ) function_case_item { function_case_item } endcase   
                                        | casex ( expression ) function_case_item { function_case_item } endcase
            function_case_item ::=      expression { , expression } : function_statement_or_null
                                        | default [ : ] function_statement_or_null  
            */

            /* SystemVerilog
            case_statement ::=  [ unique_priority ] case_keyword "(" case_expression ")"           case_item         { case_item }         "endcase"
                              | [ unique_priority ] case_keyword "(" case_expression ")" "matches" case_pattern_item { case_pattern_item } "endcase"
                              | [ unique_priority ] case         "(" case_expression ")" "inside"  case_inside_item  { case_inside_item }  "endcase"

            case_keyword    ::= "case" | "casez" | "casex"
            case_expression ::= expression 
            case_item       ::= case_item_expression { , case_item_expression } : statement_or_null 
                              | "default" [ : ] statement_or_null 
            case_pattern_item ::= pattern  [ &&& expression ] : statement_or_null 
                                | "default" [ : ] statement_or_null 
            case_inside_item  ::= open_range_list : statement_or_null 
                                | "default" [ : ] statement_or_null 
            case_item_expression ::= expression  
            open_range_list ::= open_value_range { , open_value_range }
            open_value_range ::= value_range | expression
            value_range ::= expression | expression : expression
             
             */
            switch (word.Text)
            {
                case "case":
                case "casez":
                case "casex":
                    break;
                default:
                    word.AddError("illegal case statement");
                    return null;
            }
            CaseStatement caseStatement = new CaseStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.GetCharAt(0) == '(')
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("( expected");
            }
            caseStatement.Expression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (word.GetCharAt(0) == ')')
            {
                word.MoveNext();
            }
            else
            {
                word.AddError(") expected");
            }

            if (word.Text == "matches")
            {
                caseStatement.IsMatchesMode = true;
                word.AddSystemVerilogError();
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (word.Text == "inside")
            {
                caseStatement.IsInsideMode = true;
                word.AddSystemVerilogError();
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            while (!word.Eof && word.Text != "endcase" && word.Text != "endmodule" && word.Text != "endfunction")
            {
                CaseItem caseItem = CaseItem.ParseCreate(word, nameSpace, caseStatement.IsInsideMode);
                if (caseItem == null)
                {
                    break;
                }
                caseStatement.CaseItems.Add(caseItem);
            }

            if (word.Text != "endcase")
            {
                word.AddError("illegal case statement");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            return caseStatement;
        }

        public class CaseItem
        {
            protected CaseItem() { }
            public List<Expressions.Expression> Expressions = new List<Expressions.Expression>();
            public IStatement Statement;

            public void DisposeSubRefrence()
            {
                foreach (pluginVerilog.Verilog.Expressions.Expression expression in Expressions)
                {
                    expression.DisposeSubReference(true);
                }
                Statement.DisposeSubReference();
            }

            /// <summary>
            /// Parse a case item. In inside mode, supports open_range_list which can include range expressions like [5:6].
            /// </summary>
            public static CaseItem ParseCreate(WordScanner word, NameSpace nameSpace, bool isInsideMode = false)
            {
                //            case_item ::=       expression { , expression } : statement_or_null 
                //                                | default[ : ] statement_or_null
                //            case_inside_item ::= open_range_list : statement_or_null
                //                                | "default" [ : ] statement_or_null
                //            open_range_list ::= open_value_range { , open_value_range }
                //            open_value_range ::= value_range | expression
                //            value_range ::= expression | expression : expression
                CaseItem caseItem = new CaseItem();

                if (word.Text == "default")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if (word.GetCharAt(0) == ':')
                    {
                        word.MoveNext();
                    }
                    caseItem.Statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                    return caseItem;
                }

                if (isInsideMode)
                {
                    // Parse open_range_list for inside mode
                    // open_value_range can be: expression, or expression:expression (range)
                    caseItem.Expressions = ParseOpenRangeList(word, nameSpace);
                    if (caseItem.Expressions.Count == 0)
                    {
                        word.AddError("illegal open range item");
                        return null;
                    }
                }
                else
                {
                    // Parse regular case item (expression list)
                    Expressions.Expression expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null)
                    {
                        word.AddError("illegal expression item");
                        return null;
                    }
                    caseItem.Expressions.Add(expression);

                    while (!word.Eof && word.GetCharAt(0) == ',')
                    {
                        word.MoveNext();
                        expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expression == null)
                        {
                            word.AddError("illegal expression item");
                            return null;
                        }
                        caseItem.Expressions.Add(expression);
                    }
                }

                if (word.GetCharAt(0) == ':')
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError(": exptected");
                    return null;
                }

                caseItem.Statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                return caseItem;
            }

            /// <summary>
            /// Parse open_range_list for case inside mode.
            /// open_range_list ::= open_value_range { , open_value_range }
            /// open_value_range ::= value_range | expression
            /// value_range ::= expression | expression : expression
            /// </summary>
            private static List<Expressions.Expression> ParseOpenRangeList(WordScanner word, NameSpace nameSpace)
            {
                List<Expressions.Expression> expressions = new List<Expressions.Expression>();

                while (!word.Eof && word.Text != "endcase" && word.Text != "endmodule" && word.Text != "endfunction")
                {
                    if (word.GetCharAt(0) == ',')
                    {
                        word.MoveNext();
                        continue;
                    }

                    // Check for colon first (this is the statement separator, not part of range)
                    if (word.GetCharAt(0) == ':')
                    {
                        break;
                    }

                    // Regular expression - check if it's a range [expr:expr]
                    if (word.Text == "[")
                    {
                        word.MoveNext(); // Move past '['
                        Expressions.Expression? rangeStart = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                        if (rangeStart == null)
                        {
                            word.AddError("illegal range expression");
                            word.SkipToKeyword("]");
                            if (word.Text == "]") word.MoveNext();
                            continue;
                        }

                        if (word.Text == ":")
                        {
                            word.MoveNext(); // Move past ':'
                            Expressions.Expression? rangeEnd = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                            if (rangeEnd == null)
                            {
                                word.AddError("illegal range expression");
                                word.SkipToKeyword("]");
                                if (word.Text == "]") word.MoveNext();
                                continue;
                            }

                            // Create AbsoluteRangeExpression from Verilog/Expressions/RangeExpression.cs
                            Verilog.Expressions.RangeExpression rangeExpr = new Verilog.Expressions.AbsoluteRangeExpression(rangeStart, rangeEnd);
                            expressions.Add(rangeExpr);

                            if (word.Text == "]")
                            {
                                word.MoveNext(); // Move past ']'
                            }
                            else
                            {
                                word.AddError("] expected");
                            }
                        }
                        else
                        {
                            // Single index in brackets [expr]
                            // This is treated as [expr:expr] for inside matching
                            Verilog.Expressions.RangeExpression rangeExpr = new Verilog.Expressions.AbsoluteRangeExpression(rangeStart, rangeStart);
                            expressions.Add(rangeExpr);

                            if (word.Text == "]")
                            {
                                word.MoveNext(); // Move past ']'
                            }
                            else
                            {
                                word.AddError("] expected");
                            }
                        }
                    }
                    else
                    {
                        // Regular expression (not starting with '[')
                        Expressions.Expression? expr = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expr == null)
                        {
                            break;
                        }
                        expressions.Add(expr);
                    }

                    // Exit if we hit a colon (statement separator)
                    if (word.GetCharAt(0) == ':')
                    {
                        break;
                    }
                }

                return expressions;
            }
        }
    }
}
