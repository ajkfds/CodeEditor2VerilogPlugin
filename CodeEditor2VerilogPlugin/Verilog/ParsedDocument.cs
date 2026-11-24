using pluginVerilog.CodeEditor;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
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

        /// <summary>The file this result was parsed from, or null.</summary>
        [JsonIgnore]
        public Data.IVerilogRelatedFile? TextFile
        {
            get
            {
                if (!fileRef.TryGetTarget(out Data.IVerilogRelatedFile? file)) return null;
                return file;
            }
        }

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
        public SystemVerilogCore.Projects.IVerilogProject? Project => TextFile?.Project;

        public ParsedDocument(Data.IVerilogRelatedFile file, string key, IndexReference? indexReference, SystemVerilogCore.Parsing.VerilogParseMode parseMode)
        {
            Key = key;
            ParseMode = parseMode;

            CodeDocument? document = file.CodeDocument as CodeDocument;
            if (document == null) throw new Exception();
            codeDocument = document;
            Version = (ulong)document.Version;

            if (file is SystemVerilogFile || file is SystemVerilogHeaderFile) SystemVerilog = true;

            fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
            // Capture the project property now: parse-time lookups
            // (Macros / SystemFunctions / RuleSet / name spaces) all go through
            // it, and the weak file reference may be collected later.
            ProjectProperty = file.ProjectProperty;
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

        private static CodeDocument? getCodeDocument(Data.IVerilogRelatedFile file)
        {
            if (file == null) return null; // foe illeagal jon constructor 
            CodeDocument? codeDocument = file.CodeDocument as CodeDocument;

            codeDocument = file.CodeDocument as pluginVerilog.CodeEditor.CodeDocument;
            if (codeDocument == null)
            {
                System.Diagnostics.Debugger.Break();
            }
            return codeDocument;
        }


        public static string KeyGenerator(
            Data.IVerilogRelatedFile verilogRelatedFile,
            string? moduleName,
            Dictionary<string, Verilog.Expressions.Expression>? parameterOverrides
            )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(verilogRelatedFile.Project.Name);
            sb.Append(System.IO.Path.DirectorySeparatorChar);

            sb.Append(verilogRelatedFile.RelativePath);
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
        public static string KeyGenerator(
            string key,
            Data.IVerilogRelatedFile verilogRelatedFile,
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

        private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
        [JsonIgnore]
        public Data.IVerilogRelatedFile? File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }


        [JsonIgnore]
        public IndexReference IndexReference;

        [JsonIgnore]
        public List<int> Indexes = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;

        public bool RestrictBaseParse { get; set; } = false;
        public Root Root { set; get; } = null!;

        [JsonIgnore]
        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
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
            foreach (var includeFile in IncludeFiles.Values)
            {
                includeFile.Close();
            }
        }

        private CodeDocument codeDocument;
        [JsonIgnore]
        public CodeDocument CodeDocument
        {
            get
            {
                return codeDocument;
            }
            set
            {
                codeDocument = value;
            }
        }

        /// <summary>
        /// The buffer being coloured, as the UI-free document contract.
        ///
        /// <para>
        /// The parser writes colours, marks and fold regions into the buffer
        /// through this view only. <see cref="CodeDocument"/> stays because the
        /// data layer still copies the colour table out of it
        /// (<c>CopyColorMarkFrom</c>) and the editor owns the palette, but the
        /// parser no longer names that type.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Documents.IVerilogTextDocument VerilogDocument => codeDocument;

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
        [JsonIgnore]
        public ProjectProperty? ProjectProperty { get; private set; }

        public void Dispose()
        {
            if (Root == null) return;

            Data.IVerilogRelatedFile? file = File;
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

            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
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
            document.Marks.SetMarkAt(index, length, 0);
        }

        public void AddWarning(int index, int length, string message)
        {
            if (Project == null) return;

            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
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
            document.Marks.SetMarkAt(index, length, 1);
        }
        public void AddNotice(int index, int length, string message)
        {

            if (Project == null) return;

            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
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
            document.Marks.SetMarkAt(index, length, 2);
        }
        public void AddHint(int index, int length, string message)
        {
            if (Project == null) return;

            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
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
            document.Marks.SetMarkAt(index, length, 3);
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

            int line = CodeDocument.GetLineAt(index);
            int lineStartIndex = CodeDocument.GetLineStartIndex(line);
            CodeDocument.GetWord(index, out int wordHeadIndex, out int wordLength);

            string lineText = CodeDocument.CreateLineString(line);
            int lineLength = wordHeadIndex + wordLength - lineStartIndex;
            if (lineLength > lineText.Length) lineLength = lineText.Length;
            lineText = lineText.Substring(0, lineLength);

            { // pre carlet char check
                if (index != 0)
                {
                    char preChar = CodeDocument.GetCharAt(index - 1);
                    if (preChar == ' ') return false;
                    if (preChar == '\t') return false;
                }
            }

            int blockStartIndex = 0;
            int blockEndIndex = 0;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(lineText);
                WordScanner word = new WordScanner(document, this, SystemVerilog,true,false);

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
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(elementText);
                WordScanner word = new WordScanner(document, this, SystemVerilog,true, false );

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
            public Message(Data.IVerilogRelatedFile file, string text, MessageType type, int index, int lineNo, int length, SystemVerilogCore.Projects.IVerilogProject? project)
            {
                this.fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
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

            private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
            [JsonIgnore]
            public Data.IVerilogRelatedFile? File
            {
                get
                {
                    Data.IVerilogRelatedFile? file;
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
