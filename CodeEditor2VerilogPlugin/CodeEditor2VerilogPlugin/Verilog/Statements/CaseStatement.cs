using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class CaseStatement : IStatement
    {
        protected CaseStatement() { }
        public Expressions.Expression Expression;
        public List<CaseItem> CaseItems = new List<CaseItem>();

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            foreach(CaseItem caseItem in CaseItems)
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
             
             */
            switch (word.Text)
            {
                case "case":
                    break;
                case "casez":
                    break;
                case "casex":
                    break;
                default:
                    word.AddError("illegal case statement");
                    return null;
            }
            CaseStatement caseStatement = new CaseStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.GetCharAt(0) == '(')
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

            if(word.Text == "matches" || word.Text == "inside")
            {
                word.AddSystemVerilogError();
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }


            while (!word.Eof && word.Text != "endcase" && word.Text != "endmodule" && word.Text != "endfunction")
            {
                CaseItem caseItem = CaseItem.ParseCreate(word, nameSpace);
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
                foreach(pluginVerilog.Verilog.Expressions.Expression expression in Expressions)
                {
                    expression.DisposeSubReference(true);
                }
                Statement.DisposeSubReference();
            }
            public static CaseItem ParseCreate(WordScanner word,NameSpace nameSpace)
            {
                //            case_item ::=       expression { , expression } : statement_or_null 
                //                                | default[ : ] statement_or_null
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

                Expressions.Expression expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                if(expression == null)
                {
                    word.AddError("illegal expression item");
                    return null;
                }
                caseItem.Expressions.Add(expression);

                while(!word.Eof && word.GetCharAt(0) == ',')
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

                if (word.GetCharAt(0) == ':')
                {
                    word.MoveNext();
                }else{
                    word.AddError(": exptected");
                    return null;
                }

                caseItem.Statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                return caseItem;
            }
        }
    }
}
