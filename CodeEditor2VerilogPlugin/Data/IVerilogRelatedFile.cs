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
    /// <summary>
    /// The editor's Verilog-related file.
    ///
    /// <para>
    /// It also implements the parser-facing
    /// <see cref="SystemVerilogCore.Documents.IVerilogSourceFile"/>, which is
    /// additive only (the instance-keyed parse lookup). Parse sites used to
    /// reach that lookup by casting to the concrete <c>VerilogFile</c>; the
    /// contract lets them work against any implementation.
    /// </para>
    ///
    /// <para>
    /// The contract deliberately does not re-declare <c>ID</c> /
    /// <c>RelativePath</c> / <c>Name</c> / <c>Project</c> / <c>ParsedDocument</c>:
    /// <see cref="CodeEditor2.Data.ITextFile"/> already declares them, and a
    /// second declaration would make every call site through this interface
    /// fail with CS0229 (ambiguous member).
    /// </para>
    /// </summary>
    public interface IVerilogRelatedFile : CodeEditor2.Data.ITextFile,
        SystemVerilogCore.Documents.IVerilogSourceFile
    {
        Verilog.ParsedDocument? VerilogParsedDocument { get; }

        // NOTE: `IVerilogSourceFile.GetInstancedParsedDocument` is inherited from
        // the contract and is left abstract here, so each implementation has to
        // provide it. It is not re-declared: an interface cannot give it a body,
        // and re-declaring it would only restate the same signature.

        // ---- SystemVerilogCore.Documents.IVerilogSourceFile ----------------
        // The contract carries the same values under names that do not collide
        // with ITextFile's (a second declaration of `CodeDocument` /
        // `Project` / `ParsedDocument` would make every call site through this
        // interface ambiguous). These forward to the members below.

        SystemVerilogCore.Documents.IVerilogTextDocument?
            SystemVerilogCore.Documents.IVerilogSourceFile.VerilogTextDocument => CodeDocument;

        /// <summary>
        /// Version of the buffer the parser will read. Same value as
        /// <c>CodeDocument.Version</c>; kept as a plain number so the contract
        /// does not depend on the editor's document type.
        /// </summary>
        ulong SystemVerilogCore.Documents.IVerilogSourceFile.VerilogTextDocumentVersion =>
            (ulong)(CodeDocument?.Version ?? 0);

        /// <summary>This file's own parse result, as the source file contract.</summary>
        SystemVerilogCore.Parsing.IParsedDocument?
            SystemVerilogCore.Documents.IVerilogSourceFile.VerilogParsedResult => VerilogParsedDocument;

        SystemVerilogCore.Projects.IVerilogProjectSettings?
            SystemVerilogCore.Documents.IVerilogSourceFile.ProjectSettings => ProjectProperty;

        SystemVerilogCore.Projects.IVerilogProject?
            SystemVerilogCore.Documents.IVerilogSourceFile.VerilogProject => Project;

        /// <summary>Unique id, as the source file contract.</summary>
        string SystemVerilogCore.Documents.IVerilogSourceFile.Id => ID;

        /// <summary>
        /// This file's children in the editor's item tree, as the source file
        /// contract. The parser records on-demand parsed instances here.
        /// </summary>
        SystemVerilogCore.Projects.IVerilogItemList?
            SystemVerilogCore.Documents.IVerilogSourceFile.ChildItems => Items;

        string SystemVerilogCore.Documents.IVerilogSourceFile.FileRelativePath => RelativePath;

        bool SystemVerilogCore.Documents.IVerilogSourceFile.IsSystemVerilog => SystemVerilog;

        /// <summary>
        /// Forwards to the editor-facing <see cref="CreateVerilogDocumentParser"/>
        /// declared below, so the parser can drive a parse through the contract.
        /// </summary>
        SystemVerilogCore.Parsing.IVerilogDocumentParser?
            SystemVerilogCore.Documents.IVerilogSourceFile.CreateVerilogDocumentParser(
                SystemVerilogCore.Parsing.VerilogParseMode parseMode,
                System.Threading.CancellationToken? token) =>
            CreateVerilogDocumentParser(parseMode, token);

        /// <summary>
        /// Forwards to the editor-facing
        /// <see cref="AcceptVerilogParsedDocumentAsync"/> declared below.
        /// </summary>
        System.Threading.Tasks.Task
            SystemVerilogCore.Documents.IVerilogSourceFile.AcceptVerilogParsedDocumentAsync(
                SystemVerilogCore.Parsing.IVerilogDocumentParser parser) =>
            AcceptVerilogParsedDocumentAsync(parser);

        /// <summary>
        /// Ensures this file has a buffer to parse, creating an empty one when it
        /// has none.
        ///
        /// <para>
        /// The editor creates the document while loading the file
        /// (<c>FileCheckAsync</c>), which a UI-less host never runs. A parse that
        /// needs the buffer (an instance parse) would otherwise fail; an empty
        /// document is enough because the parser is handed the text separately.
        /// </para>
        /// </summary>
        void CreateCodeDocumentForParse();

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
