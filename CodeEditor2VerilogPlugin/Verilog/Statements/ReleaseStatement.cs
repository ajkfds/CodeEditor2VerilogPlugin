using SystemVerilogCore.Documents;


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
        public VerilogColorType ColorType => VerilogColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public Expressions.Expression Value;


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
            word.Color(VerilogColorType.Keyword);
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
