namespace pluginVerilog.Data.VerilogCommon
{
    /// <summary>
    /// The editor-facing view of a parse result's buffer.
    ///
    /// <para>
    /// The parser colours, marks and folds through the UI-free
    /// <see cref="SystemVerilogCore.Documents.IVerilogTextDocument"/> contract, so
    /// it no longer names the plugin's concrete <c>CodeDocument</c>. The data
    /// layer still needs that type: it copies the colour table out of the parse
    /// result (<c>CopyColorMarkFrom</c>), formats with
    /// <c>GetIndentString</c>, and the semantic-token adapter walks
    /// <c>TextColors</c>.
    /// </para>
    ///
    /// <para>
    /// Those consumers narrow here instead of on the parse result. Keeping the
    /// narrowing in one place is what lets <c>Verilog/ParsedDocument.cs</c> stop
    /// referencing the editor assembly, which is the last thing standing between
    /// the parser and a straight <c>git mv</c> into the UI-free project.
    /// </para>
    ///
    /// <para>
    /// Returns null for a throw-away buffer (macro expansion, hint text,
    /// autocomplete probe): those are plain text documents, and nothing
    /// displays them.
    /// </para>
    /// </summary>
    internal static class ParsedDocumentCodeDocumentExtensions
    {
        /// <summary>
        /// The parse result's buffer as the plugin's concrete document, or null
        /// when it is a contract-only buffer.
        /// </summary>
        public static CodeEditor.CodeDocument? EditorCodeDocument(
            this Verilog.ParsedDocument parsedDocument) =>
            parsedDocument.VerilogDocument as CodeEditor.CodeDocument;
    }
}
