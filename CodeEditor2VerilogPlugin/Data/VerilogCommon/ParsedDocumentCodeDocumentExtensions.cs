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

        /// <summary>
        /// Prepares the file's working buffer for a partial parse and returns it,
        /// or null when no buffer could be created.
        ///
        /// <para>
        /// The autocomplete / signature-help adapters drive the parser's partial
        /// parse, which reads the source text from <c>file.CodeDocument</c>. In a
        /// host that never ran <c>FileCheckAsync</c> (the language server, the
        /// tests) that document is absent, and when the host hands the parser its
        /// own buffer there is no concrete plugin document to copy from either.
        /// The text therefore comes from the parse result through the UI-free
        /// contract, so this works for both hosts.
        /// </para>
        ///
        /// <para>
        /// Shared by <c>CompletionAdapter</c> and <c>SignatureHelpAdapter</c>:
        /// they used to carry the same preparation code, which is exactly the
        /// kind of copy that drifts.
        /// </para>
        /// </summary>
        public static CodeEditor2.CodeEditor.CodeDocument? PrepareCompletionBuffer(
            this Verilog.ParsedDocument parsedDocument,
            Data.VerilogFile file)
        {
            if (parsedDocument == null || file == null) return null;

            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = file.CodeDocument;
            if (codeDocument == null) return null;

            string parsedText = parsedDocument.VerilogDocument.CreateString();
            if (codeDocument.Length != parsedText.Length)
            {
                codeDocument.CopyTextOnlyFrom(new CodeEditor.CodeDocument(parsedText));
            }
            return codeDocument;
        }
    }
}
