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

        /// <summary>
        /// Creates a parser for this file, typed as the UI-free contract.
        ///
        /// <para>
        /// The parser used to call the editor's <c>CreateDocumentParser</c>
        /// directly when it had to parse a file on demand (the
        /// parameter-overridden instance and the <c>@scope</c> target file).
        /// That single type reference kept the parser bound to the editor
        /// assembly for parse-mode purposes. This factory returns the contract
        /// instead, so the parser can drive a parse without naming the editor.
        /// </para>
        ///
        /// <para>
        /// Implementations delegate to the editor-facing
        /// <c>CreateDocumentParser</c>; returning null means "this file has
        /// nothing to parse in this mode" (e.g. a header file during a
        /// background parse, which is parsed together with its parent).
        /// </para>
        /// </summary>
        /// <param name="parseMode">Why the document is being parsed.</param>
        /// <param name="token">Cancellation token, or null.</param>
        SystemVerilogCore.Parsing.IVerilogDocumentParser? CreateVerilogDocumentParser(
            SystemVerilogCore.Parsing.VerilogParseMode parseMode,
            System.Threading.CancellationToken? token);

        /// <summary>
        /// Accepts a parse result produced through
        /// <see cref="CreateVerilogDocumentParser"/>.
        ///
        /// <para>
        /// The editor-facing <c>AcceptParsedDocumentAsync</c> takes the editor's
        /// <c>DocumentParser</c> because it copies colour marks out of the
        /// parser's document - a host type. The parser only knows the contract,
        /// so this overload takes the contract and the implementation (which
        /// lives in the data layer and may name the editor) narrows it back.
        /// </para>
        /// </summary>
        System.Threading.Tasks.Task AcceptVerilogParsedDocumentAsync(
            SystemVerilogCore.Parsing.IVerilogDocumentParser parser);
    }
}
