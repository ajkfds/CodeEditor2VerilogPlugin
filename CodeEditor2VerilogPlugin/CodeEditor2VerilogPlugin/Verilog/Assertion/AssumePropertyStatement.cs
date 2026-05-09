using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class AssumePropertyStatement : Statements.IStatement
    {
        protected AssumePropertyStatement() { }

        /*
        assume_property_statement ::=
            "assume" "property" "(" property_spec ")" action_block

        action_block ::=
            [ statement ] [ else statement ]

        property_spec ::=
            [clocking_event ] [ "disable" "iff" "(" expression_or_dist ")" ] property_expr
        */

        public static async Task<AssumePropertyStatement> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "assume" || word.NextText != "property")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // assume

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // property

            AssumePropertyStatement assumePropertyStatement = new AssumePropertyStatement();

            if (word.Eof || word.Text != "(")
            {
                return await ExitTask(word, nameSpace, assumePropertyStatement);
            }
            word.MoveNext();

            PropertySpec propertySpec = await PropertySpec.ParseCreate(word, nameSpace);

            if (word.Eof || word.Text != ")")
            {
                return await ExitTask(word, nameSpace, assumePropertyStatement);
            }
            word.MoveNext();

            // action_block ::= [ statement ] [ else statement ]
            if (word.Text != "else")
            {
                assumePropertyStatement.PassStatement = await Statements.Statements.ParseCreateStatementOrNull(word, nameSpace);
            }

            if (word.Text != "else") return assumePropertyStatement;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // else

            assumePropertyStatement.ElseStatement = await Statements.Statements.ParseCreateStatementOrNull(word, nameSpace);

            return assumePropertyStatement;
        }

        private static async Task<AssumePropertyStatement> ExitTask(WordScanner word, NameSpace nameSpace, AssumePropertyStatement statement)
        {
            word.AddError("illegal assume property statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return statement;
        }

        public Statements.IStatement? PassStatement { get; set; }
        public Statements.IStatement? ElseStatement { get; set; }
        public PropertySpec? PropertySpec { get; set; }

        public string Name { get; set; } = "";

        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Keyword;

        public NamedElements NamedElements => new NamedElements();

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return null;
        }

        public void DisposeSubReference()
        {
        }
    }
}
