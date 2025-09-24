using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ForeverStatement : IStatement
    {
        protected ForeverStatement() { }

        public void DisposeSubReference()
        {
            if(Statement != null) Statement.DisposeSubReference();
        }

        public IStatement? Statement;
        //A.6.8 Looping statements
        //function_loop_statement ::= forever function_statement          
        //                            | repeat(expression ) function_statement
        //                            | while (expression ) function_statement
        //                            | for (variable_assignment ;  expression ; variable_assignment ) function_statement
        //loop_statement   ::= forever statement
        //                            | repeat (expression ) statement
        //                            | while (expression ) statement
        //                            | for (variable_assignment ; expression ; variable_assignment ) statement

        /* # SystemVerilog
        loop_statement ::=    "forever" statement_or_null 
                            | "repeat" "(" expression ")" statement_or_null 
                            | "while" "(" expression ")" statement_or_null 
                            | "for" "(" [ for_initialization ] ";" [ expression ] ";" [ for_step ] ")" statement_or_null 
                            | "do" statement_or_null "while" "(" expression ")" ";"
                            | "foreach" "(" ps_or_hierarchical_array_identifier "[" loop_variables "]" ")" statement
        */

        public static ForeverStatement ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ForeverStatement foreverStatement = new ForeverStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            foreverStatement.Statement = Statements.ParseCreateStatement(word, nameSpace);

            return foreverStatement;
        }
    }

    public class DoStatement : IStatement
    {
        protected DoStatement() { }

        public void DisposeSubReference()
        {
            if (Statement != null) Statement.DisposeSubReference();
        }

        public IStatement? Statement;
        public Expression? Condition;
        /* # SystemVerilog
        loop_statement ::=    "do" statement_or_null "while" "(" expression ")" ";"
                            | ...
        */

        public static DoStatement ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "do") throw new Exception();
            if (!word.SystemVerilog) word.AddError("SystemVerilog expression");

            DoStatement doStatement = new DoStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            doStatement.Statement = Statements.ParseCreateStatement(word, nameSpace);

            if(word.Eof || word.Text != "while")
            {
                word.AddError("while required");
                word.SkipToKeyword(";");
                return doStatement;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            if(word.Eof || word.Text != "(")
            {
                word.AddError("(expression) required");
                word.SkipToKeyword(";");
                return doStatement;
            }
            word.MoveNext();

            doStatement.Condition = Expression.ParseCreate(word, nameSpace);
            if (doStatement.Condition == null) word.AddError("expression required");

            if (word.Eof || word.Text != ")")
            {
                word.AddError(") required");
                word.SkipToKeyword(";");
                return doStatement;
            }
            word.MoveNext();

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; requited");
            }

                return doStatement;
        }
    }
    public class RepeatStatement : IStatement
    {
        protected RepeatStatement() { }

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            Statement.DisposeSubReference();
        }

        public Expressions.Expression Expression;
        public IStatement Statement;
        //A.6.8 Looping statements
        //function_loop_statement ::= forever function_statement          
        //                            | repeat(expression ) function_statement
        //                            | while (expression ) function_statement
        //                            | for (variable_assignment ;  expression ; variable_assignment ) function_statement
        //loop_statement   ::= forever statement
        //                            | repeat (expression ) statement
        //                            | while (expression ) statement
        //                            | for (variable_assignment ; expression ; variable_assignment ) statement
        public static RepeatStatement ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            RepeatStatement repeatStatement = new RepeatStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            repeatStatement.Expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.GetCharAt(0) != ')')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            repeatStatement.Statement = Statements.ParseCreateStatement(word, nameSpace);

            return repeatStatement;
        }
    }

    public class WhileStatememt : IStatement
    {
        protected WhileStatememt() { }

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            if(Statement!=null) Statement.DisposeSubReference();
        }

        public Expressions.Expression Expression;
        public IStatement? Statement;
        //A.6.8 Looping statements
        //function_loop_statement ::= forever function_statement          
        //                            | repeat(expression ) function_statement
        //                            | while (expression ) function_statement
        //                            | for (variable_assignment ;  expression ; variable_assignment ) function_statement
        //loop_statement   ::= forever statement
        //                            | repeat (expression ) statement
        //                            | while (expression ) statement
        //                            | for (variable_assignment ; expression ; variable_assignment ) statement
        public static WhileStatememt ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WhileStatememt whileStatement = new WhileStatememt();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            whileStatement.Expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.GetCharAt(0) != ')')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();

            whileStatement.Statement = Statements.ParseCreateStatement(word, nameSpace);

            return whileStatement;
        }
    }

    public class ForStatememt : NameSpace, IStatement
    {
        protected ForStatememt(BuildingBlocks.BuildingBlock buildingBlock,NameSpace nameSpace) : base(buildingBlock, nameSpace)
        {

        }

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            Statement.DisposeSubReference();
        }


        public IStatement Statement;

        public DataObjects.VariableAssignment VariableAssignment;
        public Expressions.Expression Expression;
        public DataObjects.VariableAssignment VariableUpdate;

        //A.6.8 Looping statements
        //function_loop_statement ::= forever function_statement          
        //                            | repeat(expression ) function_statement
        //                            | while (expression ) function_statement
        //                            | for (variable_assignment ;  expression ; variable_assignment ) function_statement
        //loop_statement   ::= forever statement
        //                            | repeat (expression ) statement
        //                            | while (expression ) statement
        //                            | for (variable_assignment ; expression ; variable_assignment ) statement

        // ## SystemVerilog2017
        // for ( [ for_initialization ] ; [ expression ] ; [ for_step ] ) statement_or_null

        // for_initialization           ::=   list_of_variable_assignments
        //                                  | for_variable_declaration { , for_variable_declaration }

        // for_variable_declaration     ::= [ "var" ] data_type variable_identifier = expression { , variable_identifier = expression }
        // for_step                     ::=   for_step_assignment { , for_step_assignment }
        // for_step_assignment          ::=   operator_assignment
        //                                  | inc_or_dec_expression 
        //                                  | function_subroutine_call
        // loop_variables               ::=   [index_variable_identifier] { , [index_variable_identifier] }

        // operator_assignment          ::= variable_lvalue assignment_operator expression
        // assignment_operator          ::= = | += | -= | *= | /= | %= | &= | |= | ^= | <<= | >>= | <<<= | >>>=

        public static ForStatememt? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ForStatememt forStatement = new ForStatememt(nameSpace.BuildingBlock, nameSpace) {
                BeginIndexReference = word.CreateIndexReference(), 
                DefinitionReference = word.CrateWordReference(), 
                Name = "", 
                Parent = nameSpace,
                Project = word.Project 
            };

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            /*
            loop_statement ::=    for ( [ for_initialization ] ; [ expression ] ; [ for_step ] ) statement_or_null 
                                | ...
            for_initialization ::=    list_of_variable_assignments 
                                    | for_variable_declaration { , for_variable_declaration } 
            for_variable_declaration ::=
                        [ var ] data_type variable_identifier = expression { , variable_identifier = expression }

            for_step ::= for_step_assignment { , for_step_assignment }

            for_step_assignment ::=       operator_assignment 
                                        | inc_or_dec_expression 
                                        | function_subroutine_call 
            loop_variables ::= [ index_variable_identifier ] { , [ index_variable_identifier ] } 
             */


            if (word.Text == "(")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("( expected");
                return null;
            }

            // for_initialization
            if(!Verilog.DataObjects.Variables.Variable.ParseDeclaration(word, forStatement)) // define index parameter
            {
                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, forStatement);
                if(expression == null)
                {
                    word.AddError("illegal for_initialization");
                    return null;
                }

                switch (word.Text)
                {
                    case "=":
                        BlockingAssignment.ParseCreate(word, nameSpace, expression);
                        break;
                    case "<=":
                        NonBlockingAssignment.ParseCreate(word, nameSpace, expression);
                        break;
                    default:
                        word.AddError("illegal for_initialization");
                        return null;
                }
                if (word.Text == ";")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                    return null;
                }
            }

            // expression
            forStatement.Expression = Expressions.Expression.ParseCreate(word, forStatement);

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                return null;
            }

            // for_step
            IncOrDecExpression? incOrDecExpression = IncOrDecExpression.ParseCreate(word, forStatement,false);
            if(incOrDecExpression != null)
            {

            }
            else
            {
                DataObjects.VariableAssignment? assign = Verilog.DataObjects.VariableAssignment.ParseCreate(word, forStatement,false);
                if (assign == null)
                {
                    forStatement.Expression = Expressions.Expression.ParseCreate(word, forStatement);
                }
            }


            if (word.GetCharAt(0) != ')')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext();


            forStatement.Statement = Statements.ParseCreateStatement(word, forStatement);
            return forStatement;
        }


    }

    public class ForeachStatement : IStatement
    {
        protected ForeachStatement() { }

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            Statement.DisposeSubReference();
        }

        public Expressions.Expression Expression;
        public IStatement? Statement;

        // "foreach" "(" ps_or_hierarchical_array_identifier [ loop_variables ] ")" statement
        public static ForeachStatement ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ForeachStatement foreachStatement = new ForeachStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                return foreachStatement;
            }
            word.MoveNext();

            foreachStatement.Expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if(word.Text!="[")
            {
                word.AddError("[ expected");
                word.SkipToKeyword(";");
                return foreachStatement;
            }

            word.MoveNext(); //"["


            if (word.Text != ")")
            {
                word.AddError(") expected");
                return null;
            }
            word.MoveNext();

            foreachStatement.Statement = Statements.ParseCreateStatement(word, nameSpace);

            return foreachStatement;
        }
    }


}
