using CodeEditor2.CodeEditor.CodeComplete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ConditionalStatement : IStatement
    {
        protected ConditionalStatement() { }
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            foreach (ConditionStatementPair pair in ConditionStatementPairs)
            {
                pair.ConditionalExpression.DisposeSubReference(true);
                pair.Statement.DisposeSubReference();
            }
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
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
        public static ConditionalStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label, List<string>? clockDomains = null)
        {
            System.Diagnostics.Debug.Assert(word.Text == "if");
            IndexReference beginIndex = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // if

            // EOF just after "if": suggest condition expression candidates
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            ConditionalStatement conditionalStatement = new ConditionalStatement() { Name = "", BeginIndexReference = beginIndex };
            if (statement_label != null) { conditionalStatement.Name = statement_label; }

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext(); // (

            // EOF just after "if(": suggest condition expression candidates
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            Expressions.Expression? conditionExpression = Expressions.Expression.ParseCreate(word, nameSpace);
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

            IStatement? statement = Statements.ParseCreateStatementOrNull(word, nameSpace, clockDomains);
            if (statement == null)
            {
                word.AddError("illegal conditional expression");
                return null;
            }
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

                    // EOF just after "else if(": suggest condition expression candidates
                    if (word.CompletionContext != null && word.Eof)
                    {
                        word.CompletionContext.AppendExpression();
                        return null;
                    }

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

                    statement = Statements.ParseCreateStatementOrNull(word, nameSpace, clockDomains);
                    if (statement != null) conditionalStatement.ConditionStatementPairs.Add(new ConditionStatementPair(conditionExpression, statement));
                }
                else
                {
                    statement = Statements.ParseCreateStatementOrNull(word, nameSpace, clockDomains);
                    if (statement != null) conditionalStatement.ConditionStatementPairs.Add(new ConditionStatementPair(null, statement));
                    break;
                }
            }
            // the if/else chain ends with the last sub-statement: adopt its region end
            // (e.g. "end" of a begin..end block) instead of the next token position
            StatementRegionUtility.SetLastIndexReference(conditionalStatement, statement, word);
            return conditionalStatement;
        }
    }
}
