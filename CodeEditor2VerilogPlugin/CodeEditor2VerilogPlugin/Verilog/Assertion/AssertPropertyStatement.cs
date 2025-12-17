using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class AssertPropertyStatement : Statements.IStatement
    {
        protected AssertPropertyStatement() { }


        public static async Task<AssertPropertyStatement> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            // assert_property_statement::= "assert" "property" ( property_spec ) action_block
            if (word.Text != "assert" || word.NextText != "property")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            AssertPropertyStatement assertPropertyStatement = new AssertPropertyStatement();

            if (word.Eof || word.Text != "(") return await exitTask(word, nameSpace, assertPropertyStatement);
            word.MoveNext();

            PropertySpec propertySpec = await PropertySpec.ParseCreate(word, nameSpace);
            word.SkipToKeyword(")");

            if (word.Eof || word.Text != ")") return await exitTask(word, nameSpace, assertPropertyStatement);
            word.MoveNext();

            assertPropertyStatement.PassStatement = await Statements.Statements.ParseCreateStatement(word, nameSpace);

            if (word.Text != "else") return assertPropertyStatement;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            assertPropertyStatement.ElseStatement = await Statements.Statements.ParseCreateStatement(word, nameSpace);
            return assertPropertyStatement;
        }
        public Statements.IStatement? PassStatement { set; get; }
        public Statements.IStatement? ElseStatement { set; get; }
        private static async Task<AssertPropertyStatement> exitTask(WordScanner word, NameSpace nameSpace, AssertPropertyStatement assertPropertyStatement)
        {
            word.AddError("iilegal property statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return assertPropertyStatement;
        }



        public string Name { get; set; } = "";
        
        public CodeDrawStyle.ColorType ColorType
        {
            get
            {
                return CodeDrawStyle.ColorType.Keyword;
            }
        }

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
