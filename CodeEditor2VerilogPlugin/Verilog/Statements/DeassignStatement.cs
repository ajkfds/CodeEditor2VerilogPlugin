using SystemVerilogCore.Documents;


namespace pluginVerilog.Verilog.Statements
{
    public class DeassignStatement : IStatement
    {
        protected DeassignStatement() { }
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; }
        public VerilogColorType ColorType => VerilogColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /*
        procedural_continuous_assignments ::=
            assign variable_assignment 
            | deassign variable_lvalue 
            | force variable_assignment 
            | force net_assignment 
            | release variable_lvalue 
            | release net_lvalue
        */
        public Expressions.Expression LValue;

        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
        }


        public static DeassignStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "deassign") System.Diagnostics.Debugger.Break();
            IndexReference beginIndexReference = word.CreateIndexReference();
            DeassignStatement ret = new DeassignStatement() { BeginIndexReference = beginIndexReference };
            word.Color(VerilogColorType.Keyword);
            word.MoveNext();

            ret.LValue = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);

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
