using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.Statements
{
    public class ReleaseStatement : IStatement
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

        public Expressions.Expression Value;
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
            Value.DisposeSubReference(true);
        }
        protected ReleaseStatement() { }
        public static ReleaseStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "release") System.Diagnostics.Debugger.Break();
            IndexReference beginIndexReference = word.CreateIndexReference();
            ReleaseStatement ret = new ReleaseStatement() { BeginIndexReference = beginIndexReference };
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            ret.Value = Expressions.Expression.ParseCreate(word, nameSpace);

            ret.LastIndexReference = word.CreateIndexReferenceBefore();
            if (word.Text != ";")
            {
                word.AddError("; required");
            }
            else
            {
                word.MoveNext();
            }

            return ret;
        }
    }
}
