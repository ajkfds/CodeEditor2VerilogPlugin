using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;

// The macro type and the file-reference data now live in the UI-free parser
// assembly.
using Macro = SystemVerilogCore.Verilog.Macro;
using VerilogRelatedFileReference = SystemVerilogCore.Verilog.VerilogRelatedFileReference;
using System.Threading;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// A parse result.
    ///
    /// <para>
    /// Implements the UI-free
    /// <see cref="SystemVerilogCore.Parsing.IParsedDocument"/> (key + version),
    /// not the editor's alias of it, so the parser does not reference the editor
    /// assembly for this. The editor's <c>IParsedDocument</c> derives from the
    /// same contract, so a <c>TextFile</c> still holds this object directly.
    /// </para>
    ///
    /// <para>
    /// <c>IParsedDocumentMessagesProvider</c> is the editor's narrow view of the
    /// message list (the Messages pane reads it); it stays implemented because
    /// the pane is an editor feature.
    /// </para>
    /// </summary>
    public class ParsedDocument : SystemVerilogCore.Parsing.IParsedDocument,
        SystemVerilogCore.Parsing.IParsedDocumentMessagesProvider
    {
        /// <summary>
        /// The parser's diagnostics, exposed to the editor's Messages pane.
        ///
        /// <para>
        /// The pane reads messages through this interface because the parser's
        /// <see cref="Message"/> is its own type: it no longer derives from
        /// <c>CodeEditor2.CodeEditor.ParsedDocument.Message</c> (that
        /// inheritance is what tied the parser to the editor assembly).
        /// </para>
        /// </summary>
        IReadOnlyList<object> SystemVerilogCore.Parsing.IParsedDocumentMessagesProvider.Messages
        {
            get
            {
                List<object> list = new List<object>(Messages.Count);
                foreach (Message message in Messages) list.Add(message);
                return list;
            }
        }

        // ---- members that used to be inherited ---------------------------
        // The parser used to derive from CodeEditor2.CodeEditor.ParsedDocument,
        // which supplied the key, version, parse mode, locked-document list and
        // the owning file/project. They are declared here now so the parser no
        // longer depends on that type. The editor still consumes them: it reads
        // Key to cache instance parses and ParseMode to decide whether to defer
        // work.

        /// <summary>Cache key of this parse result.</summary>
        public string Key { get; set; }

        /// <summary>Document version this result was produced from.</summary>
        public ulong Version { get; set; }

        /// <summary>Why this document was parsed.</summary>
        public readonly SystemVerilogCore.Parsing.VerilogParseMode ParseMode;

        // NOTE: `LockedDocument` used to live here (a List of the editor's
        // concrete CodeDocument). The parser appended to it on every parse but
        // nothing ever read it, so it only served to make the parser reference
        // the editor's document type. Removed from the contract and from here.

        /// <summary>
        /// The file this result was parsed from, as the UI-free contract, or
        /// null when it has been collected.
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Documents.IVerilogSourceFile? File
        {
            get
            {
                if (!fileRef.TryGetTarget(out SystemVerilogCore.Documents.IVerilogSourceFile? file)) return null;
                return file;
            }
        }

        /// <summary>
        /// Same object as <see cref="File"/>.
        ///
        /// <para>
        /// The name is kept because editor-facing call sites have always read
        /// <c>TextFile</c>; the type is the contract now, so those call sites
        /// narrow back to the editor's file type themselves. Parse-internal
        /// reads use <see cref="File"/>.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Documents.IVerilogSourceFile? TextFile => File;

        /// <summary>
        /// The project this file belongs to, as the UI-free contract.
        ///
        /// <para>
        /// This used to hand out the editor's concrete <c>Data.Project</c>
        /// (through <c>ProjectInterop.AsEditorProject</c>), which meant every
        /// parser site that only stored the project into a
        /// <see cref="NamedItem.Project"/> (already the contract type) still
        /// depended on the editor assembly. The contract carries everything the
        /// parser needs: the name, the item tree and the settings.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Projects.IVerilogProject? Project => File?.VerilogProject;

        /// <summary>
        /// Builds a parse result for a file.
        ///
        /// <para>
        /// The constructor takes the UI-free source file contract and stores it
        /// as the contract, so nothing here names the editor assembly. Editor
        /// call sites that need more than the contract offers (the item tree)
        /// narrow <see cref="File"/> back to their own type themselves.
        /// </para>
        /// </summary>
        public ParsedDocument(SystemVerilogCore.Documents.IVerilogSourceFile file, string key, IndexReference? indexReference, SystemVerilogCore.Parsing.VerilogParseMode parseMode)
        {
            Key = key;
            ParseMode = parseMode;

            SystemVerilogCore.Documents.IVerilogTextDocument? document = file.VerilogTextDocument;
            if (document == null) throw new Exception();
            verilogDocument = document;
            Version = file.VerilogTextDocumentVersion;

            SystemVerilog = file.IsSystemVerilog;

            fileRef = new WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile>(file);
            // Capture the project property now: parse-time lookups
            // (Macros / SystemFunctions / RuleSet / name spaces) all go through
            // it, and the weak file reference may be collected later.
            ProjectProperty = file.ProjectSettings as IVerilogParserSettings;
            if (indexReference == null)
            {
                IndexReference = IndexReference.Create(this, document, 0);
            }
            else
            {
                IndexReference = indexReference;
            }
        }

        //private ParsedDocument(Data.IVerilogRelatedFile file, string key, IndexReference? indexReference) : base((CodeEditor2.Data.TextFile)file, key, long.MaxValue, DocumentParser.ParseModeEnum.LoadParse)
        //{
        //    if (file is SystemVerilogFile || file is SystemVerilogHeaderFile) SystemVerilog = true;

        //    System.Diagnostics.Debug.Print("## parsed " + key);
        //    if (file == null)
        //    {
        //        System.Diagnostics.Debug.Print("## parsed null " + key);
        //    }
        //}

        // NOTE: a private getCodeDocument(file) helper used to live here. It had
        // no caller and named the plugin's concrete CodeDocument only to cast
        // the file's document into it, so it was dead code carrying a host type.
        // Removed rather than re-typed.

        /// <summary>
        /// Builds the cache key for a parse result.
        ///
        /// <para>
        /// Takes the UI-free source file contract: the key is just the project
        /// name plus the project-relative path (plus the module and its parameter
        /// overrides for an instance), and all three are on the contract. Parse
        /// sites therefore do not have to name the editor's file type.
        /// </para>
        /// </summary>
        public static string KeyGenerator(
            SystemVerilogCore.Documents.IVerilogSourceFile verilogRelatedFile,
            string? moduleName,
            Dictionary<string, Verilog.Expressions.Expression>? parameterOverrides
            )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(verilogRelatedFile.VerilogProject?.Name ?? "");
            sb.Append(System.IO.Path.DirectorySeparatorChar);

            sb.Append(verilogRelatedFile.FileRelativePath);
            if (moduleName == null) return sb.ToString(); // base Parse

            sb.Append(":");
            sb.Append(moduleName);
            if (parameterOverrides == null || parameterOverrides.Count == 0) return sb.ToString();

            sb.Append(":");
            bool firstItem = true;
            foreach (var pair in parameterOverrides)
            {
                if (!firstItem) sb.Append(",");
                firstItem = false;
                sb.Append(pair.Key);
                sb.Append("=");
                sb.Append(pair.Value.ConstantValueString());
            }
            return sb.ToString();
        }
        /// <summary>
        /// Builds a key by prefixing <paramref name="key"/> onto the file's key.
        /// Takes the UI-free source file contract, like the 3-argument overload.
        /// </summary>
        public static string KeyGenerator(
            string key,
            SystemVerilogCore.Documents.IVerilogSourceFile verilogRelatedFile,
            string? moduleName,
            Dictionary<string, Verilog.Expressions.Expression>? parameterOverrides
            )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(key);
            sb.Append(":");
            sb.Append(KeyGenerator(verilogRelatedFile, moduleName, parameterOverrides));
            return sb.ToString();
        }

        ~ParsedDocument()
        {
            //            System.Diagnostics.Debug.Print("### pasedDocument.Finalize " + id+"::"+ObjectID);
        }

        private System.WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile> fileRef = null!;

        [JsonIgnore]
        public IndexReference IndexReference;

        [JsonIgnore]
        public List<int> Indexes = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;

        public bool RestrictBaseParse { get; set; } = false;
        public Root Root { set; get; } = null!;

        /// <summary>
        /// The files this one pulled in with <c>include</c>, keyed by their
        /// include instance id.
        ///
        /// <para>
        /// Held as the UI-free contract: the parser only records and releases
        /// them, and re-parses them through their parse results. Editor-only work
        /// (item-tree nodes, colour-mark copies into the displayed document)
        /// narrows back to <c>Data.VerilogHeaderInstance</c> in the data layer.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, SystemVerilogCore.Documents.IVerilogIncludeFile> IncludeFiles =
            new Dictionary<string, SystemVerilogCore.Documents.IVerilogIncludeFile>();
        [JsonIgnore]
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();
        
        public List<string> ImportedPackages = new List<string>();

        public List<string> ReferencedUnitNameSpace = new List<string>();
        public List<string> ReferencedPackageNamemeSpace = new List<string>();

        public Dictionary<string, VerilogRelatedFileReference> DefinitionNameSpaceRefs = new Dictionary<string, VerilogRelatedFileReference>();

        
        /*
        public List<string> DefinitionNamespaceIds = new List<string>();
        public List<string> UnitNamespaceIds = new List<string>();
        public List<string> PackageNamespaceIds = new List<string>();
        public List<string> ImportedPackageIds = new List<string>();
        */

        // hierarchical instance paths referenced by bind directives
        // (bind_target_instance / bind_target_instance_list).
        // full path strings ("a.b.c") are registered so that hierarchy
        // analysis (SimulationSetup / ParseHierarchy) can track bind targets
        // that live inside instance hierarchies of this file.
        public List<string> BindTargetInstancePaths = new List<string>();

        /// <summary>
        /// A forward typedef declaration ("typedef identifier ;") seen while
        /// parsing. The actual data type definition shall be resolved within the
        /// same local scope; it is an error when the type_identifier does not
        /// resolve to a data type (IEEE 1800-2017 6.18). Resolved once the whole
        /// file has been parsed (a forward typedef may precede its definition).
        /// </summary>
        public class ForwardTypedefDeclaration
        {
            public required string Name { get; init; }
            public required WordReference Reference { get; init; }
            public required NameSpace DeclaringNameSpace { get; init; }
        }

        [JsonIgnore]
        public List<ForwardTypedefDeclaration> ForwardTypedefs { get; } = new List<ForwardTypedefDeclaration>();

        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides = new Dictionary<string, Expressions.Expression>();
        public string? TargetBuildingBlockName = null;

        public List<string> ExternalRefrenceModules = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        // for IndexReference

        private bool reparseRequested = true;
        public bool ReparseRequested
        {
            get
            {
                return reparseRequested;
            }
            set
            {
                reparseRequested = value;
            }
        }

        public void ReloadIncludeFiles()
        {
            foreach (SystemVerilogCore.Documents.IVerilogIncludeFile includeFile in IncludeFiles.Values)
            {
                includeFile.CloseInclude();
            }
        }

        private SystemVerilogCore.Documents.IVerilogTextDocument verilogDocument = null!;

        /// <summary>
        /// The buffer being coloured, as the UI-free document contract.
        ///
        /// <para>
        /// The parser writes colours, marks and fold regions into the buffer
        /// through this view only, and it is what the tokenizer restores after a
        /// macro-expansion dive. <see cref="CodeDocument"/> stays because the
        /// data layer still copies the colour table out of it
        /// (<c>CopyColorMarkFrom</c>) and the editor owns the palette, but the
        /// parser no longer names that type.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Documents.IVerilogTextDocument VerilogDocument
        {
            get
            {
                return verilogDocument;
            }
            set
            {
                verilogDocument = value;
            }
        }

        /// <summary>
        /// The same buffer as the plugin's concrete document, or null when the
        /// parser supplied a contract-only buffer (macro expansion, hint text).
        ///
        /// <para>
        /// Only the data layer needs the concrete type: it copies the colour
        /// table out of the parse result. Everything the parser does goes
        /// through <see cref="VerilogDocument"/>.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public pluginVerilog.CodeEditor.CodeDocument? CodeDocument
        {
            get
            {
                return verilogDocument as pluginVerilog.CodeEditor.CodeDocument;
            }
            set
            {
                verilogDocument = value!;
            }
        }

        /// <summary>
        /// The plugin property of the project this file belongs to, captured
        /// once at construction.
        ///
        /// <para>
        /// Every parse-time lookup the parser needs from a project goes through
        /// this object (<c>Macros</c>, <c>SystemFunctions</c>,
        /// <c>SystemTaskParsers</c>, <c>DefinitionNameSpace</c>,
        /// <c>RuleSet</c>), so the parser never needs the editor's
        /// <c>CodeEditor2.Data.Project</c> type. Holding it here also means the
        /// lookups keep working after the file's weak reference to the project
        /// has been collected.
        /// </para>
        /// </summary>
        /// <summary>
        /// The settings of the project this result belongs to, as the
        /// parser-facing contract, or null when the file is not attached to a
        /// project.
        ///
        /// <para>
        /// Typed as <see cref="IVerilogParserSettings"/> rather than the plugin's
        /// concrete <c>ProjectProperty</c>: that contract carries everything the
        /// parser reads, including the system task / function tables and the
        /// instance resolver. Assigning the concrete object works because it
        /// implements the contract.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public IVerilogParserSettings? ProjectProperty { get; private set; }

        public void Dispose()
        {
            if (Root == null) return;

            // NOTE: this used to read the file back and do nothing with it. The
            // read is gone with the editor type: the parse result no longer
            // names the editor's file.
        }

        /// <summary>
        /// Diagnostics collected while parsing this document.
        ///
        /// <para>
        /// This used to be inherited from
        /// <c>CodeEditor2.CodeEditor.ParsedDocument.Messages</c> (a
        /// <c>List&lt;CodeEditor2...Message&gt;</c>), which is why the parser
        /// had to derive from the editor's ParsedDocument. It is a plain list
        /// of this type's own <see cref="Message"/> now.
        /// </para>
        /// </summary>
        public List<Message> Messages { get; } = new List<Message>();

        [JsonIgnore]
        public int ErrorCount = 0;
        [JsonIgnore]
        public int WarningCount = 0;
        [JsonIgnore]
        public int HintCount = 0;
        [JsonIgnore]
        public int NoticeCount = 0;

        public void AddError(int index, int length, string message)
        {
            if (Project == null) return;

            // Diagnostics go through the UI-free document contract: the parser
            // reports a message and asks the host to mark the span. It used to
            // reach for the editor's concrete CodeDocument and its Marks table,
            // which is what kept this file bound to the editor assembly.
            //
            // The file comes from the DOCUMENT, not from this parse result: a
            // throw-away buffer (macro expansion body) belongs to no file, and
            // its diagnostics must be dropped. Reading this result's own file
            // instead would record them, and the enclosing scanner would then
            // report "include errors" for an error inside a macro expansion -
            // measured on sv-tests 22.5.1--define-expansion_26.
            SystemVerilogCore.Documents.IVerilogTextDocument document = VerilogDocument;
            if (document.File == null) return; // throw-away buffer: nothing to attach to
            SystemVerilogCore.Documents.IVerilogSourceFile? vFile = File;
            if (vFile == null) return;

            // add message
            if (ErrorCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Error, index, lineNo, length, Project));
            }
            else if (ErrorCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 errors", Verilog.ParsedDocument.Message.MessageType.Error, 0, 0, 0, Project)); ;
            }

            // increment message count
            ErrorCount++;

            // add mark
            document.SetMarkAt(index, length, 0);
        }

        public void AddWarning(int index, int length, string message)
        {
            if (Project == null) return;

            SystemVerilogCore.Documents.IVerilogTextDocument document = VerilogDocument;
            if (document.File == null) return; // throw-away buffer: nothing to attach to
            SystemVerilogCore.Documents.IVerilogSourceFile? vFile = File;
            if (vFile == null) return;

            // add message
            if (WarningCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Warning, index, lineNo, length, Project));
            }
            else if (WarningCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 warnings", Verilog.ParsedDocument.Message.MessageType.Warning, 0, 0, 0, Project));
            }

            // increment message count
            WarningCount++;

            // add mark
            document.SetMarkAt(index, length, 1);
        }
        public void AddNotice(int index, int length, string message)
        {

            if (Project == null) return;

            SystemVerilogCore.Documents.IVerilogTextDocument document = VerilogDocument;
            if (document.File == null) return; // throw-away buffer: nothing to attach to
            SystemVerilogCore.Documents.IVerilogSourceFile? vFile = File;
            if (vFile == null) return;

            // add message
            if (NoticeCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Notice, index, lineNo, length, Project));
            }
            else if (NoticeCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Notice, 0, 0, 0, Project));
            }

            // increment message count
            NoticeCount++;

            // add mark
            document.SetMarkAt(index, length, 2);
        }
        public void AddHint(int index, int length, string message)
        {
            if (Project == null) return;

            SystemVerilogCore.Documents.IVerilogTextDocument document = VerilogDocument;
            if (document.File == null) return; // throw-away buffer: nothing to attach to
            SystemVerilogCore.Documents.IVerilogSourceFile? vFile = File;
            if (vFile == null) return;

            // add message
            if (HintCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Hint, index, lineNo, length, Project));
            }
            else if (HintCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Hint, 0, 0, 0, Project));
            }

            // increment message count
            HintCount++;

            // add mark
            document.SetMarkAt(index, length, 3);
        }


        /// <summary>
        /// The data a mouse-over hint is built from at <paramref name="index"/>:
        /// the messages covering that position plus the element's declaration
        /// label, or null when there is nothing to show.
        ///
        /// <para>
        /// This used to build the editor's <c>PopupItem</c> directly (coloured
        /// text spans plus a rendered SVG icon), which is why the parser
        /// referenced the editor assembly and Avalonia. It now returns plain
        /// data and the editor does the drawing - see
        /// <c>Data/VerilogCommon/AutoCompleteHandler.GetPopupItem</c>.
        /// </para>
        /// </summary>
        public SystemVerilogCore.Documents.VerilogHintContent? GetHintContent(int index, string text)
        {
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            if (iref.RootParsedDocument == null) return null;

            SystemVerilogCore.Documents.VerilogHintContent content = new();

            // messages covering this position, in declaration order
            foreach (Message message in Messages)
            {
                if (index < message.Index) continue;
                if (index > message.Index + message.Length) continue;

                SystemVerilogCore.Documents.VerilogHintSeverity severity = message.Type switch
                {
                    Message.MessageType.Error => SystemVerilogCore.Documents.VerilogHintSeverity.Error,
                    Message.MessageType.Warning => SystemVerilogCore.Documents.VerilogHintSeverity.Warning,
                    Message.MessageType.Notice => SystemVerilogCore.Documents.VerilogHintSeverity.Notice,
                    _ => SystemVerilogCore.Documents.VerilogHintSeverity.Hint,
                };
                content.Messages.Add(
                    new SystemVerilogCore.Documents.VerilogHintMessage(severity, message.Text));
            }

            if (File == null || !getPopupTarget(index, out NameSpace? nameSpace, out INamedElement? element))
            {
                return null;
            }

            SystemVerilogCore.Documents.VerilogLabel elementLabel = new();
            if (element != null)
            {
                if (element is DataObject)
                {
                    ((DataObject)element).AppendLabel(elementLabel);
                }
                else if (element is Function)
                {
                    ((Function)element).AppendLabel(elementLabel);
                }
                else if (element is Task_)
                {
                    ((Task_)element).AppendLabel(elementLabel);
                }
                else if (element is DataObjects.Typedef)
                {
                    ((DataObjects.Typedef)element).AppendLabel(elementLabel);
                }
            }

            if (Macros.ContainsKey(text))
            {
                Macros[text].AppendLabel(elementLabel, Macros);
            }
            content.Label = elementLabel;
            return content;
        }

        private bool getPopupTarget(int index, out NameSpace? nameSpace, out INamedElement? element)
        {
            nameSpace = null;
            element = null;

            // The contract carries line lookup and substring; the token width
            // comes from the parser's own tokenizer, so nothing here names the
            // editor's concrete document (which owned GetWord / CreateLineString).
            SystemVerilogCore.Documents.IVerilogTextDocument document = VerilogDocument;

            int line = document.GetLineAt(index);
            int lineStartIndex = document.GetLineStartIndex(line);

            // Same walk the editor's GetWord does: fetch the first token of the
            // line, then advance while the next token starts at or before the
            // caret. Done through the tokenizer so the parser does not need the
            // editor's document type for this.
            int wordHeadIndex = lineStartIndex;
            int wordLength = 0;
            {
                int nextIndex;
                Verilog.WordPointer.WordTypeEnum wordType;
                string sectionName = "";
                Verilog.WordPointer.FetchNext(
                    document, ref wordHeadIndex, out wordLength, out nextIndex, out wordType, ref sectionName);
                while (nextIndex <= index && index < document.Length)
                {
                    wordHeadIndex = nextIndex;
                    Verilog.WordPointer.FetchNext(
                        document, ref wordHeadIndex, out wordLength, out nextIndex, out wordType, ref sectionName);
                }
            }

            string lineText = document.CreateString(
                lineStartIndex, document.GetLineLength(line));
            int lineLength = wordHeadIndex + wordLength - lineStartIndex;
            if (lineLength > lineText.Length) lineLength = lineText.Length;
            if (lineLength < 0) lineLength = 0;
            lineText = lineText.Substring(0, lineLength);

            { // pre carlet char check
                if (index != 0)
                {
                    char preChar = document.GetCharAt(index - 1);
                    if (preChar == ' ') return false;
                    if (preChar == '\t') return false;
                }
            }

            int blockStartIndex = 0;
            int blockEndIndex = 0;
            {
                // create short document to parse current pretext
                SystemVerilogCore.Documents.IVerilogTextDocument probeDocument =
                    new SystemVerilogCore.Documents.PlainTextDocument(lineText);
                WordScanner word = new WordScanner(probeDocument, this, SystemVerilog,true,false);

                List<(string, int)> words = new List<(string, int)>();
                while (!word.Eof)
                {
                    if (General.IsIdentifier(word.Text))
                    {
                        words.Add((word.Text, word.RootIndex));
                        word.MoveNext();
                        if (word.Text == "::" || word.Text == "->" || word.Text == ".")
                        {
                            words.Add((word.Text, word.RootIndex));
                            word.MoveNext();
                        }
                        else
                        {
                            if (word.Eof) break;
                            words.Clear();
                        }
                    }
                    else
                    {   // illegal text
                        words.Add((word.Text, word.RootIndex));
                        word.MoveNext();
                        if (word.Eof) break;
                        words.Clear();
                    }
                }

                if (words.Count == 0) return false;

                blockStartIndex = words[0].Item2;
                blockEndIndex = words.Last().Item2 + words.Last().Item1.Length;

            }

            // get namespace
            {
                // namespace must get from linestart index, because current index cann't match last parsed document
                IndexReference iref = IndexReference.Create(IndexReference, lineStartIndex);
                nameSpace = GetNameSpace(iref);
            }

            if(lineText.Length<blockStartIndex) return false;
            if(lineText.Length<blockEndIndex) return false;

            string elementText = lineText.Substring(blockStartIndex, blockEndIndex - blockStartIndex);
            element = null;
            {
                // create short document to parse current pretext
                SystemVerilogCore.Documents.IVerilogTextDocument elementDocument =
                    new SystemVerilogCore.Documents.PlainTextDocument(elementText);
                WordScanner word = new WordScanner(elementDocument, this, SystemVerilog,true, false );

                Verilog.Expressions.Expression? expression = null;
                while (!word.Eof)
                {
                    if (nameSpace != null) expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null) word.MoveNext();
                }
                if (expression is Verilog.Expressions.DataObjectReference)
                {
                    Verilog.Expressions.DataObjectReference dataObjectReference = (Verilog.Expressions.DataObjectReference)expression;
                    element = dataObjectReference.TargetDataObject;
                }
                else if (expression is Verilog.Expressions.NameSpaceReference)
                {
                    NameSpace targetNameSpace = ((Verilog.Expressions.NameSpaceReference)expression).NameSpace;
                    element = targetNameSpace;
                }
            }
            return true;
        }





        public BuildingBlock? GetBuildingBlockAt(int index)
        {
            if (Root == null) return null;
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            foreach (var moduleKvp in Root.BuildingBlocks)
            {
                if (iref.IsSmallerThan(moduleKvp.Value.BeginIndexReference)) continue;
                if (moduleKvp.Value.LastIndexReference == null) continue;
                if (iref.IsGreaterThan(moduleKvp.Value.LastIndexReference)) continue;
                return moduleKvp.Value;
            }
            return null;
        }

        public Items.IDocumentRegeion? GetDocumentRegionAt(IndexReference indexRef)
        {
            if (Root == null) return null;

            IndexReference? foundBegin = null;
            IndexReference? foundLast = null;
            Items.IDocumentRegeion? item = null;

            searchNameSpace(Root, indexRef, ref foundBegin, ref foundLast, ref item);
            if(item is NameSpace)
            {
                searchItem((NameSpace)item, indexRef, ref foundBegin, ref foundLast, ref item);
            }
            return item;
        }

        private void searchNameSpace(NameSpace nameSpace, IndexReference targetIndexRef, ref IndexReference? foundBegin,ref IndexReference? foundLast ,ref Items.IDocumentRegeion? item )
        {
            foreach (var element in nameSpace.NamedElements.Values)
            {
                NameSpace? subNameSpace = element as NameSpace;
                if (subNameSpace == null) continue;
                if (subNameSpace.BeginIndexReference == null) continue;
                if (subNameSpace.LastIndexReference == null) continue;
                // skip elements whose index hierarchy depth differs from the target:
                // IndexReference comparison only inspects up to the shorter index list,
                // so a nested (deeper) region falsely "contains" any later root-level position
                if (subNameSpace.BeginIndexReference.Indexes.Count != targetIndexRef.Indexes.Count) continue;
                if (subNameSpace.LastIndexReference.Indexes.Count != targetIndexRef.Indexes.Count) continue;

                if (targetIndexRef.IsSmallerThan(subNameSpace.BeginIndexReference)) continue;
                if (subNameSpace.LastIndexReference == null) continue;
                if (targetIndexRef.IsGreaterThan(subNameSpace.LastIndexReference)) continue;

                item = subNameSpace;
                foundBegin = subNameSpace.BeginIndexReference;
                foundLast = subNameSpace.LastIndexReference;
                searchNameSpace(subNameSpace, targetIndexRef, ref foundBegin, ref foundLast,ref item );
                break;
            }
        }

        private void searchItem(NameSpace nameSpace, IndexReference targetIndexRef, ref IndexReference? foundBegin, ref IndexReference? foundLast,ref Items.IDocumentRegeion? item)
        {
            foreach (Items.IDocumentRegeion documentRegion in nameSpace.DocumentRegions)
            {
                if (documentRegion.BeginIndexReference == null) continue;
                if (documentRegion.LastIndexReference == null) continue;
                // skip regions whose index hierarchy depth differs from the target:
                // IndexReference comparison only inspects up to the shorter index list,
                // so a nested (deeper) region (e.g. always inside generate block) falsely
                // "contains" any later root-level position and wins over the correct region
                if (documentRegion.BeginIndexReference.Indexes.Count != targetIndexRef.Indexes.Count) continue;
                if (documentRegion.LastIndexReference.Indexes.Count != targetIndexRef.Indexes.Count) continue;

                if (targetIndexRef.IsSmallerThan(documentRegion.BeginIndexReference)) continue;
                if (targetIndexRef.IsGreaterThan(documentRegion.LastIndexReference)) continue;

                if(foundBegin != null && foundLast != null)
                {
                    if (documentRegion.BeginIndexReference.IsSmallerThan(foundBegin)) continue;
                    if (documentRegion.LastIndexReference.IsGreaterThan(foundLast)) continue;
                }

                item = documentRegion;
                foundBegin = documentRegion.BeginIndexReference;
                foundLast = documentRegion.LastIndexReference;
            }
        }


        public NameSpace? GetNameSpace(IndexReference iref)
        {
            if (Root == null) return null;
            // get current buldingBlock
            NameSpace? space = null;
            foreach (var buildingBlockKvp in Root.BuildingBlocks)
            {
                if (iref.IsSmallerThan(buildingBlockKvp.Value.BeginIndexReference)) continue;
                if (buildingBlockKvp.Value.LastIndexReference == null) break;
                if (iref.IsGreaterThan(buildingBlockKvp.Value.LastIndexReference)) continue;
                space = buildingBlockKvp.Value.GetHierarchyNameSpace(iref);
                break;
            }
            return space;
        }


        private NameSpace? getSearchNameSpace(NameSpace? nameSpace, List<string> hier)
        {
            if (ProjectProperty == null) return null;
            if (nameSpace == null) return null;
            BuildingBlock? buildingBlock = nameSpace.BuildingBlock;
            if (buildingBlock == null) return null;

            if (hier.Count == 0) return nameSpace;

            if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(hier[0]))
            {
                IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)buildingBlock.NamedElements[hier[0]];
                NameSpace? bBlock = ProjectProperty.GetInstancedBuildingBlock(instantiation);

                if (instantiation is Verilog.Items.InterfaceInstance)
                {
                    Verilog.Items.InterfaceInstance? interfaceInstantiation = instantiation as Verilog.Items.InterfaceInstance;
                    if (interfaceInstantiation == null) throw new Exception();
                }

                hier.RemoveAt(0);
                return getSearchNameSpace(bBlock, hier);
            }
            else if (nameSpace.NamedElements.ContainsKey(hier[0]))
            {
                NameSpace? space = nameSpace.NamedElements[hier[0]] as NameSpace;
                hier.RemoveAt(0);
                return getSearchNameSpace(space, hier);
            }
            return nameSpace;
        }


        // NOTE: appendAutoCompleteINamedElements() was removed here. It was the
        // only caller of INamedElement.CreateAutoCompleteItem(), and nothing
        // called it: the live completion path is
        // CompletionContext.appendNamedElements -> appendItemsUpward, which
        // builds candidates itself. Keeping a second, unreachable implementation
        // is what made the parser appear to depend on the editor's
        // AutocompleteItem type.

       　





        private INamedElement? getSearchSubElement(INamedElement element, List<string> hier)
        {
            if (element == null) return null;
            if (hier.Count == 0) return element;

            if (element.NamedElements.ContainsKey(hier[0]))
            {
                INamedElement subElement = element.NamedElements[hier[0]];
                hier.RemoveAt(0);
                getSearchSubElement(element, hier);
            }
            return element;
        }







        /// <summary>
        /// A parse diagnostic (error / warning / notice / hint) with its
        /// position and the file it came from.
        ///
        /// <para>
        /// This used to derive from <c>CodeEditor2.CodeEditor.ParsedDocument.Message</c>,
        /// which dragged the editor's <c>MessageView</c> (Avalonia) into the
        /// parser. The only thing that base class contributed was the
        /// <c>CreateMessageNode()</c> hook for the editor's message pane, which
        /// is a UI concern and is now built by the editor from this plain data
        /// instead.
        /// </para>
        /// </summary>
        public class Message
        {
            public Message(SystemVerilogCore.Documents.IVerilogSourceFile file, string text, MessageType type, int index, int lineNo, int length, SystemVerilogCore.Projects.IVerilogProject? project)
            {
                this.fileRef = new WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile>(file);
                this.Text = text;
                this.Length = length;
                this.Index = index;
                this.LineNo = lineNo;
                this.Type = type;
                this.Project = project;
            }

            /// <summary>Character offset of the offending token.</summary>
            public int Index { get; set; }

            /// <summary>Length of the marked span, in characters.</summary>
            public int Length { get; set; }

            /// <summary>Diagnostic text.</summary>
            public string Text { get; set; } = "";

            /// <summary>
            /// Project the file belonged to when the message was created, or
            /// null. Kept so hosts can route a message back to its project
            /// without walking the file tree again.
            /// </summary>
            public SystemVerilogCore.Projects.IVerilogProject? Project { get; set; }

            private System.WeakReference<SystemVerilogCore.Documents.IVerilogSourceFile> fileRef = null!;

            /// <summary>
            /// The file this message came from, as the UI-free contract, or null
            /// when it has been collected. The editor's message pane narrows it
            /// back to its own file type.
            /// </summary>
            [JsonIgnore]
            public SystemVerilogCore.Documents.IVerilogSourceFile? File
            {
                get
                {
                    SystemVerilogCore.Documents.IVerilogSourceFile? file;
                    if (!fileRef.TryGetTarget(out file)) return null;
                    return file;
                }
            }

            public int LineNo { get; protected set; }

            public enum MessageType
            {
                Error,
                Warning,
                Notice,
                Hint
            }
            public MessageType Type { get; protected set; }

            // CreateMessageNode() (the editor's MessageView node) used to be an
            // override here. It is a UI concern, so the editor now builds its
            // node from this data itself; the parser no longer references
            // MessageView.

        }
    }

}
