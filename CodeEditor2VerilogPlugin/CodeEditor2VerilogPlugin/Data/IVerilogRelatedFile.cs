namespace pluginVerilog.Data
{
    public interface IVerilogRelatedFile : CodeEditor2.Data.ITextFile
    {

        Verilog.ParsedDocument? VerilogParsedDocument { get; }

        ProjectProperty ProjectProperty { get; }
        bool SystemVerilog { get; }

        string AbsolutePath { get; }
        void CheckDirty();
    }
}
