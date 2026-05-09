using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ExpectPropertyStatement : IStatement
    {
        protected ExpectPropertyStatement() { }

        /*
        expect_property_statement ::=
            "expect" "(" property_spec ")" action_block

        action_block ::=
            [ statement ] [ else statement ]

        property_spec ::=
            [clocking_event ] [ "disable" "iff" "(" expression_or_dist ")" ] property_expr
        */

        public static async Task<ExpectPropertyStatement> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "expect")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // expect

            ExpectPropertyStatement expectPropertyStatement = new ExpectPropertyStatement();
            if (statement_label != null)
            {
                expectPropertyStatement.Name = statement_label;
            }

            if (word.Eof || word.Text != "(")
            {
                return await ExitTask(word, nameSpace, expectPropertyStatement);
            }
            word.MoveNext();

            // Parse property_spec using the SVA PropertySpec parser
            var propertySpec = await Assertion.PropertySpec.ParseCreate(word, nameSpace);

            if (word.Eof || word.Text != ")")
            {
                return await ExitTask(word, nameSpace, expectPropertyStatement);
            }
            word.MoveNext();

            // action_block ::= [ statement ] [ else statement ]
            expectPropertyStatement.PassStatement = await Statements.ParseCreateStatementOrNull(word, nameSpace);

            if (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // else

                expectPropertyStatement.ElseStatement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
            }

            return expectPropertyStatement;
        }

        private static async Task<ExpectPropertyStatement> ExitTask(WordScanner word, NameSpace nameSpace, ExpectPropertyStatement statement)
        {
            word.AddError("illegal expect property statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return statement;
        }

        public Assertion.PropertySpec? PropertySpec { get; set; }
        public IStatement? PassStatement { get; set; }
        public IStatement? ElseStatement { get; set; }

        public string Name { get; set; } = "";

        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Keyword;

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
            PassStatement?.DisposeSubReference();
            ElseStatement?.DisposeSubReference();
        }
    }
}
