namespace pluginVerilog.Data
{
    // NOTE: this interface does NOT derive from
    // SystemVerilogCore.Documents.IVerilogDocumentFile.
    //
    // Both interfaces would contribute RelativePath / Name, and an interface
    // cannot disambiguate the two by re-declaring them (CS0229 at every call
    // site that goes through this type). The parser-facing view is supplied by
    // a small adapter instead: see
    // CodeEditor2.CodeEditor.CodeDocument's IVerilogTextDocument.File, which
    // wraps the TextFile. Keeping the two hierarchies apart also avoids
    // changing the meaning of any existing member.
    public interface IVerilogRelatedFile : CodeEditor2.Data.ITextFile
    {
        Verilog.ParsedDocument? VerilogParsedDocument { get; }

        ProjectProperty ProjectProperty { get; }
        bool SystemVerilog { get; }

        string AbsolutePath { get; }
        void CheckDirty();
    }
}
