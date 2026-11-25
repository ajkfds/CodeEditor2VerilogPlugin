using SystemVerilogCore.Documents;


namespace pluginVerilog.Verilog.Statements
{
    public class DisableStatement : IStatement
    {
        protected DisableStatement() { }

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; }
        public VerilogColorType ColorType => VerilogColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
        }


        public string Identifier { get; protected set; }

        // disable_statement::= (From Annex A - A.6.5)  
        //                    disable hierarchical_task_identifier; 
        //                    | disable hierarchical_block_identifier;
        public static DisableStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            DisableStatement disableStatement = new DisableStatement() { BeginIndexReference = word.CreateIndexReference() };
            word.Color(VerilogColorType.Keyword);
            word.MoveNext();

            word.Color(VerilogColorType.Identifier);
            word.MoveNext();

            disableStatement.LastIndexReference = word.CreateIndexReferenceBefore();
            if (word.Text != ";")
            {
                word.AddError("; required");
                return null;
            }
            word.MoveNext();

            return disableStatement;
        }
    }

}
