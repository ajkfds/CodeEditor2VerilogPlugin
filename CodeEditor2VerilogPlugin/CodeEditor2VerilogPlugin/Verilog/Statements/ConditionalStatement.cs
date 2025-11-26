using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ConditionalStatement : IStatement
    {
        protected ConditionalStatement() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            foreach(ConditionStatementPair pair in ConditionStatementPairs)
            {
                pair.ConditionalExpression.DisposeSubReference(true);
                pair.Statement.DisposeSubReference();
            }
        }

        public List<ConditionStatementPair> ConditionStatementPairs = new List<ConditionStatementPair>();

        public struct ConditionStatementPair
        {
            public ConditionStatementPair(Expressions.Expression conditionalExpression, IStatement statement)
            {
                ConditionalExpression = conditionalExpression;
                Statement = statement;
            }
            public Expressions.Expression ConditionalExpression;
            public IStatement Statement;
        }

        /*
        A.6.6 Conditional statements
        conditional_statement   ::= if (expression ) statement_or_null[ else statement_or_null]
                                    | if_else_if_statement
        if_else_if_statement    ::= if (expression ) statement_or_null { else if (expression) statement_or_null } [ else statement_or_null]

        function_conditional_statement  ::= if (expression ) function_statement_or_null[ else function_statement_or_null]
                                            | function_if_else_if_statement
        function_if_else_if_statement   ::= if (expression ) function_statement_or_null { else if (expression) function_statement_or_null } [ else function_statement_or_null]
        */
        public static async Task<ConditionalStatement> ParseCreate(WordScanner word, NameSpace nameSpace,string? statement_label)
        {
            System.Diagnostics.Debug.Assert(word.Text == "if");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // if

            ConditionalStatement conditionalStatement = new ConditionalStatement() { Name = "" };
            if(statement_label != null) { conditionalStatement.Name = statement_label; }

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext(); // (

            Expressions.Expression conditionExpression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (conditionExpression == null)
            {
                word.AddError("illegal conditional expression");
                return null;
            }

            if (word.GetCharAt(0) != ')')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext(); // )

            IStatement statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
            conditionalStatement.ConditionStatementPairs.Add(new ConditionStatementPair(conditionExpression, statement));

            while (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // else

                if (word.Text == "if")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext(); // else

                    if (word.GetCharAt(0) != '(')
                    {
                        word.AddError("( expected");
                        return null;
                    }
                    word.MoveNext(); // (

                    conditionExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (conditionExpression == null)
                    {
                        word.AddError("illegal conditional expression");
                        return null;
                    }
                    if (word.GetCharAt(0) != ')')
                    {
                        word.AddError("( expected");
                        return null;
                    }
                    word.MoveNext(); // )

                    statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
                    conditionalStatement.ConditionStatementPairs.Add(new ConditionStatementPair(conditionExpression, statement));
                }
                else
                {
                    statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
                    conditionalStatement.ConditionStatementPairs.Add(new ConditionStatementPair(null, statement));
                    break;
                }
            }
            return conditionalStatement;
        }
    }
}
