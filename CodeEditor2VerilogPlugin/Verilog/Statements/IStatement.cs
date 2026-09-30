namespace pluginVerilog.Verilog.Statements
{
    public interface IStatement : INamedElement, Items.IDocumentRegeion
    {
        void DisposeSubReference();
        IndexReference BeginIndexReference { get; init; }
        IndexReference? LastIndexReference { get; set; }
    }
}
