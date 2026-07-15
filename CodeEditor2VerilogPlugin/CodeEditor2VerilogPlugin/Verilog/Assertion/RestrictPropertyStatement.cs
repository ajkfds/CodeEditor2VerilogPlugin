using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class RestrictPropertyStatement : Statements.IStatement
    {
        protected RestrictPropertyStatement() { }

        /*
        restrict_property_statement ::=
            "restrict" "property" "(" property_spec ")" ;

        property_spec ::=
            [clocking_event ] [ "disable" "iff" "(" expression_or_dist ")" ] property_expr
        */

        public static RestrictPropertyStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "restrict" || word.NextText != "property")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // restrict

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // property

            RestrictPropertyStatement restrictPropertyStatement = new RestrictPropertyStatement();

            if (word.Eof || word.Text != "(")
            {
                return ExitTask(word, nameSpace, restrictPropertyStatement);
            }
            word.MoveNext();

            PropertySpec propertySpec = PropertySpec.ParseCreate(word, nameSpace);

            if (word.Eof || word.Text != ")")
            {
                return ExitTask(word, nameSpace, restrictPropertyStatement);
            }
            word.MoveNext();

            if (word.Text != ";")
            {
                word.AddError("; expected");
            }
            else
            {
                word.MoveNext();
            }

            return restrictPropertyStatement;
        }

        private static RestrictPropertyStatement ExitTask(WordScanner word, NameSpace nameSpace, RestrictPropertyStatement statement)
        {
            word.AddError("illegal restrict property statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return statement;
        }

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
