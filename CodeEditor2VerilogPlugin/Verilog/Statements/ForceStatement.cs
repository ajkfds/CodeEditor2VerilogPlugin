using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.Statements
{
    public class ForceStatement : IStatement
    {
        /*
        procedural_continuous_assignments ::=
            assign variable_assignment 
            | deassign variable_lvalue 
            | force variable_assignment 
            | force net_assignment 
            | release variable_lvalue 
            | release net_lvalue
        */
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public Expressions.Expression? LValue;
        public Expressions.Expression? Value;

        protected ForceStatement() { }
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
            LValue.DisposeSubReference(true);
            Value.DisposeSubReference(true);
        }
        public static ForceStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "force") System.Diagnostics.Debugger.Break();
            IndexReference beginIndexReference = word.CreateIndexReference();
            ForceStatement ret = new ForceStatement() { BeginIndexReference = beginIndexReference };
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            ret.LValue = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);
            if (ret.LValue == null)
            {
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }

            if (word.Text != "=")
            {
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }
            word.MoveNext();

            ret.Value = Expressions.Expression.ParseCreate(word, nameSpace);

            ret.LastIndexReference = word.CreateIndexReferenceBefore();
            if (word.Text != ";")
            {
                word.AddError("; required");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
            }
            else
            {
                word.MoveNext();
            }

            return ret;
        }
    }
}
