using CodeEditor2.CodeEditor.CodeComplete;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public enum ImmediateAssertionKind
    {
        Simple,       // assert (expr) action_block
        DeferredZero, // assert #0 (expr) action_block
        Final,        // assert final (expr) action_block
    }

    public class ImmidiateAssertionStatement : IStatement
    {
        /*
        IEEE 1800-2017 SystemVerilog

        immediate_assertion_statement ::=
              simple_immediate_assert_statement
            | simple_immediate_assume_statement
            | simple_immediate_cover_statement
            | deferred_immediate_assert_statement
            | deferred_immediate_assume_statement
            | deferred_immediate_cover_statement

        simple_immediate_assert_statement ::=
            assert ( expression ) action_block

        simple_immediate_assume_statement ::=
            assume ( expression ) action_block

        simple_immediate_cover_statement ::=
            cover ( expression ) statement_or_null

        deferred_immediate_assert_statement ::=
              assert #0 ( expression ) action_block
            | assert final ( expression ) action_block

        deferred_immediate_assume_statement ::=
              assume #0 ( expression ) action_block
            | assume final ( expression ) action_block

        deferred_immediate_cover_statement ::=
              cover #0 ( expression ) statement_or_null
            | cover final ( expression ) statement_or_null

        action_block ::=
            [ statement ] [ else statement ]
        */

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Assertion kind: Simple, DeferredZero (#0), or Final
        /// </summary>
        public ImmediateAssertionKind Kind { get; protected set; } = ImmediateAssertionKind.Simple;

        public void DisposeSubReference()
        {
            ConditionalExpression?.DisposeSubReference(true);
            Statement?.DisposeSubReference();
            ElseStatement?.DisposeSubReference();
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

        public Expressions.Expression? ConditionalExpression;
        public IStatement? Statement;
        public IStatement? ElseStatement;
        public static ImmidiateAssertionStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            System.Diagnostics.Debug.Assert(word.Text == "assert" || word.Text == "assume" || word.Text == "cover");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            ImmidiateAssertionStatement assertion = new ImmidiateAssertionStatement() { Name = "" };
            if (statement_label != null) { assertion.Name = statement_label; }

            // Check for deferred immediate assertion: #0 or final
            if (word.Text == "#")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // #
                if (word.Text == "0")
                {
                    word.Color(CodeDrawStyle.ColorType.Number);
                    word.MoveNext(); // 0
                    assertion.Kind = ImmediateAssertionKind.DeferredZero;
                }
                else
                {
                    word.AddError("#0 expected");
                    return assertion;
                }
            }
            else if (word.Text == "final")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // final
                assertion.Kind = ImmediateAssertionKind.Final;
            }

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return assertion;
            }
            word.MoveNext(); // (

            Expressions.Expression? conditionExpression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (conditionExpression == null)
            {
                word.AddError("illegal conditional expression");
                return assertion;
            }
            assertion.ConditionalExpression = conditionExpression;

            if (word.GetCharAt(0) != ')')
            {
                word.AddError(") expected");
                return assertion;
            }
            word.MoveNext(); // )

            // action_block ::= [ statement ] [ else statement ]
            IStatement? statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
            assertion.Statement = statement;

            // Handle else clause (only for assert and assume)
            if (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // else

                IStatement? elseStatement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                assertion.ElseStatement = elseStatement;
            }

            return assertion;
        }
    }
}
