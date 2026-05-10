using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class CoverPropertyStatement : Statements.IStatement
    {
        protected CoverPropertyStatement() { }

        /*
        cover_property_statement ::=
            "cover" "property" "(" property_spec ")" statement_or_null
        */

        public static async Task<CoverPropertyStatement> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "cover" || word.NextText != "property")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // cover

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // property

            CoverPropertyStatement coverPropertyStatement = new CoverPropertyStatement();

            if (word.Eof || word.Text != "(")
            {
                return await ExitTask(word, nameSpace, coverPropertyStatement);
            }
            word.MoveNext();

            PropertySpec propertySpec = await PropertySpec.ParseCreate(word, nameSpace);

            if (word.Eof || word.Text != ")")
            {
                return await ExitTask(word, nameSpace, coverPropertyStatement);
            }
            word.MoveNext();

            // cover property statement does not have action_block, only statement_or_null
            coverPropertyStatement.CoverStatement = await Statements.Statements.ParseCreateStatementOrNull(word, nameSpace);

            return coverPropertyStatement;
        }

        private static async Task<CoverPropertyStatement> ExitTask(WordScanner word, NameSpace nameSpace, CoverPropertyStatement statement)
        {
            word.AddError("illegal cover property statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return statement;
        }

        public Statements.IStatement? CoverStatement { get; set; }
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
