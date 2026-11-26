using Avalonia.Threading;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using SystemVerilogCore.Verilog;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace pluginVerilog.Data
{
    public class VerilogHeaderInstance : InstanceTextFile, IVerilogRelatedFile,
        SystemVerilogCore.Documents.IVerilogIncludeFile
    {
        /// <summary>
        /// Unique id, as the include-file contract.
        /// </summary>
        string SystemVerilogCore.Documents.IVerilogIncludeFile.IncludeId => ID;

        /// <summary>
        /// The include's parse result, as the include-file contract.
        /// </summary>
        SystemVerilogCore.Parsing.IParsedDocument?
            SystemVerilogCore.Documents.IVerilogIncludeFile.IncludeParsedDocument => VerilogParsedDocument;

        /// <summary>
        /// Where the <c>include</c> appeared, as the include-file contract.
        /// </summary>
        object? SystemVerilogCore.Documents.IVerilogIncludeFile.InstancedReference =>
            InstancedReference;

        /// <summary>
        /// The included file as a source file. A header instance is itself one
        /// (it implements the source file contract).
        /// </summary>
        SystemVerilogCore.Documents.IVerilogSourceFile?
            SystemVerilogCore.Documents.IVerilogIncludeFile.SourceFile => this;

        /// <summary>
        /// Releases the include, as the include-file contract. Forwards to
        /// <see cref="Close"/>.
        /// </summary>
        void SystemVerilogCore.Documents.IVerilogIncludeFile.CloseInclude() => Close();

        protected VerilogHeaderInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {
        }

        private string _id = string.Empty;
        public override string ID
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _id;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _id = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }


        // ------------------------


        /// <summary>
        /// Creates a header instance, resolving the included file through the
        /// UI-free project contract.
        ///
        /// <para>
        /// This is the overload the parser calls: it used to pass the editor's
        /// concrete <c>Data.Project</c> just so the item tree could be walked,
        /// which is what kept <c>WordScanner</c> bound to the editor assembly.
        /// The contract carries the same lookup
        /// (<see cref="SystemVerilogCore.Projects.IVerilogItemTree.FindByRelativePath"/>).
        /// </para>
        /// </summary>
        public static VerilogHeaderInstance? Create(
            string relativePath,
            string name,
            IndexReference instancedReference,
            IVerilogRelatedFile parentFile,
            SystemVerilogCore.Projects.IVerilogProject project,
            string id)
        {
            CodeEditor2.Data.Project? editorProject = ProjectInterop.AsEditorProject(project);
            if (editorProject == null) return null;
            return Create(relativePath, name, instancedReference, parentFile, editorProject, id);
        }

        /// <summary>
        /// The parser-facing overload: the parent file arrives as the UI-free
        /// contract, so <c>WordScanner</c> does not have to name the editor's
        /// file type. The narrowing to <see cref="IVerilogRelatedFile"/> happens
        /// here, in the data layer, which is the one place that knows the editor
        /// types.
        /// </summary>
        public static VerilogHeaderInstance? Create(
            string relativePath,
            string name,
            IndexReference instancedReference,
            SystemVerilogCore.Documents.IVerilogSourceFile parentFile,
            SystemVerilogCore.Projects.IVerilogProject project,
            string id)
        {
            if (parentFile is not IVerilogRelatedFile editorParentFile) return null;
            return Create(relativePath, name, instancedReference, editorParentFile, project, id);
        }

        public static VerilogHeaderInstance? Create(
            string relativePath,
            string name,
            IndexReference instancedReference,
            IVerilogRelatedFile parentFile,
            CodeEditor2.Data.Project project,
            string id)
        {
            // NOTE: a `projectProperty` lookup used to sit here; it was never
            // read (the instance takes its settings from the file it wraps), so
            // it is gone rather than carried along.
            CodeEditor2.Data.Item? fileItem = project.GetItem(relativePath);
            CodeEditor2.Data.TextFile? vhFile = fileItem as CodeEditor2.Data.TextFile;

            if (vhFile == null) return null;

            //string name;
            //if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            //{
            //    name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            //}
            //else
            //{
            //    name = relativePath;
            //}
            VerilogHeaderInstance instance = new VerilogHeaderInstance(vhFile)
            {
                Name = name,
                Project = project,
                RelativePath = relativePath,
                InstanceName = name,
                ParameterOverrides= new Dictionary<string, Verilog.Expressions.Expression>()
            };
            instance.CopyCodeDocumentFrom(vhFile);

            instance.ID = id;
            instance.RootFile = parentFile;
            instance.InstancedReference = instancedReference;
            return instance;
        }
        static VerilogHeaderInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }
        public IVerilogRelatedFile RootFile { get; protected set; }
        public IndexReference InstancedReference { get; protected set; }


        public new bool SystemVerilog { get { return RootFile.SystemVerilog; } }

        public override bool ReparseRequested
        {
            get
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return true;
                return vParsedDocument.ReparseRequested;
            }
            set
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return;
                vParsedDocument.ReparseRequested = value;
            }
        }
        public new void CheckDirty()
        {
            ParsedDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            if (vParsedDocument.ReparseRequested) return;
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = CodeDocument;
            if (codeDocument == null) return;
            if (codeDocument.Version != vParsedDocument.Version)
            {
                ReparseRequested = true;
            }
        }
        public bool ReplaceBy(
            VerilogHeaderInstance file
            )
        {
            if (file == null) return false;
            if (!IsSameAs(file as File)) return false;

            // Use write lock for updating state
            ParsedDocument = file.ParsedDocument;

            // CodeDocument is from source file, so read lock is sufficient
            var codeDoc = CodeDocument;

            var fileCodeDoc = file.CodeDocument;

            if (codeDoc != null && fileCodeDoc != null)
            {
                if (codeDoc.Version == fileCodeDoc.Version)
                {
                    // Check if they are the same object to avoid LockRecursionException
                    // (cannot acquire read lock when already holding write lock on same lock)
                    if (!object.ReferenceEquals(codeDoc, fileCodeDoc))
                    {
                        codeDoc.CopyColorMarkFrom(fileCodeDoc);
                    }
                }
            }

            return true;
        }


        public override CodeEditor2.CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                if (SourceVerilogFile == null) throw new Exception();
                return SourceVerilogFile.CodeDocument;
            }
        }




        public string ParameterId
        {
            get
            {
                return "";
            }
        }

        private CodeEditor2.Data.TextFile? SourceVerilogFile
        {
            get
            {
                // An include target may be a .svh (VerilogHeaderFile) or a plain
                // .sv/.v file (VerilogFile). Both are TextFile; the include only
                // needs the source CodeDocument.
                return SourceTextFile;
            }
        }



        public override void Close()
        {
            var parsed = VerilogParsedDocument;

            if (parsed != null) parsed.ReloadIncludeFiles();
            //SourceVerilogFile.Close();
        }

        //public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        //{
        //    get
        //    {
        //        if (ParsedDocument == null) throw new Exception();
        //        return ParsedDocument;
        //    }
        //    set
        //    {
        //        Verilog.ParsedDocument? vParsedDocument = value as Verilog.ParsedDocument;
        //        if (vParsedDocument == null) throw new Exception();
        //        ParsedDocument = vParsedDocument;
        //    }
        //}

        //protected Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>> nodeRefDictionary
        //    = new Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>>();

        //public override CodeEditor2.NavigatePanel.NavigatePanelNode NavigatePanelNode
        //{
        //    get
        //    {
        //        CodeEditor2.NavigatePanel.NavigatePanelNode? node = null;
        //        List<WeakReference<CodeEditor2.Data.Item?>> disposeRefs = new List<WeakReference<CodeEditor2.Data.Item?>>();

        //        // search parent based table
        //        foreach (var pair in nodeRefDictionary)
        //        {
        //            var parentRef = pair.Key;
        //            if (!parentRef.TryGetTarget(out var parent))
        //            {
        //                disposeRefs.Add(parentRef);
        //                continue;
        //            }
        //            if (parent != Parent) continue;

        //            var nodeRef = pair.Value;
        //            if (nodeRef.TryGetTarget(out node)) break;
        //        }

        //        // remove unconnected weakRefs
        //        foreach (var disposeRef in disposeRefs)
        //        {
        //            nodeRefDictionary.Remove(disposeRef);
        //        }

        //        if (node == null)
        //        {
        //            node = CreateNode();
        //            if (node == null) throw new Exception();

        //            WeakReference<CodeEditor2.Data.Item?> parent = new WeakReference<CodeEditor2.Data.Item?>(Parent);
        //            nodeRefDictionary.Add(parent, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node));
        //        }

        //        return node;
        //    }
        //    protected set
        //    {
        //        WeakReference<CodeEditor2.Data.Item?>? indexRef = null;

        //        // search parent based table
        //        foreach (var pair in nodeRefDictionary)
        //        {
        //            var parentRef = pair.Key;
        //            if (!parentRef.TryGetTarget(out var parent))
        //            {
        //                continue;
        //            }
        //            if (parent != Parent) continue;
        //            indexRef = parentRef;
        //        }

        //        if (indexRef != null)
        //        {
        //            nodeRefDictionary.Remove(indexRef);
        //        }
        //        WeakReference<CodeEditor2.Data.Item?> parentNewRef = new WeakReference<CodeEditor2.Data.Item?>(Parent);
        //        nodeRefDictionary.Add(parentNewRef, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(value));
        //    }
        //}

        public override async System.Threading.Tasks.Task SaveAsync()
        {
            if (SourceTextFile == null) return;

            // Save through the source file and refresh this instance's navigate panel
            // node (the source file's SaveAsync only refreshes its own node, which
            // left this node's dirty badge stale after Save).
            await SaveSourceTextFileAsync();
        }

        public new Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
            }
        }

        public override async System.Threading.Tasks.Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.Parser.DocumentParser parser)
        {
            if (Plugin.StopParse) return;

            TextFile? textFile = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (textFile == this)
            {
                textFile.CodeDocument?.CopyColorMarkFrom(parser.Document);
                CodeEditor2.Controller.CodeEditor.PostRefresh();
            }
            await UpdateAsync();
            // Announce the updated parse result; the host decides how to
            // refresh (editor: UI thread repaint, server: hierarchy push).
            NotifyPresentationChanged();
        }




        [JsonIgnore]
        public new ProjectProperty ProjectProperty
        {
            get
            {
                CodeEditor2.Data.ProjectProperty projectProperty = Project.ProjectProperties[Plugin.StaticID];
                ProjectProperty? vProjectProperty = projectProperty as ProjectProperty;
                if (vProjectProperty == null) throw new Exception();
                return vProjectProperty;
            }
        }


        public override CodeEditor2.CodeEditor.CodeDrawStyle DrawStyle
        {
            get
            {
                return Global.CodeDrawStyle;
            }
        }




        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            Data.IVerilogRelatedFile? parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode, token);
        }

        /// <summary>
        /// UI-free parser factory (see
        /// <see cref="IVerilogRelatedFile.CreateVerilogDocumentParser"/>).
        /// A header instance has no parser of its own: it is parsed together
        /// with its parent file, so only an edit parse (which goes through the
        /// parent) has anything to do.
        /// </summary>
        public SystemVerilogCore.Parsing.IVerilogDocumentParser? CreateVerilogDocumentParser(
            SystemVerilogCore.Parsing.VerilogParseMode parseMode,
            System.Threading.CancellationToken? token)
        {
            Data.IVerilogRelatedFile? parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            if (parseMode != SystemVerilogCore.Parsing.VerilogParseMode.EditParse) return null;
            return parentFile.CreateVerilogDocumentParser(parseMode, token);
        }

        // update sub-items from ParsedDocument
        public override async System.Threading.Tasks.Task UpdateAsync()
        {
            // search parent verilogFile hierachy
            CodeEditor2.Data.Item? parentItem = Parent;
            while (true)
            {
                if (parentItem == null) break;
                if (parentItem is VerilogHeaderFile)
                {
                    parentItem = parentItem.Parent;
                }
                else
                {
                    break;
                }
            }

            if (parentItem != null && parentItem is VerilogFile)
            {
                VerilogFile vFile = (VerilogFile)parentItem;
                await VerilogCommon.Updater.UpdateAsync(vFile, itemUpdateSemaphore);
            }

            await base.UpdateAsync();
        }

        // Auto Complete Handler

        //public override void AfterKeyDown(System.Windows.Forms.KeyEventArgs e)
        //{
        //    VerilogCommon.AutoComplete.AfterKeyDown(this, e);
        //}

        //public override void AfterKeyPressed(System.Windows.Forms.KeyPressEventArgs e)
        //{
        //    VerilogCommon.AutoComplete.AfterKeyPressed(this, e);
        //}

        //public override void BeforeKeyPressed(System.Windows.Forms.KeyPressEventArgs e)
        //{
        //    VerilogCommon.AutoComplete.BeforeKeyPressed(this, e);
        //}

        //public override void BeforeKeyDown(System.Windows.Forms.KeyEventArgs e)
        //{
        //    VerilogCommon.AutoComplete.BeforeKeyDown(this, e);
        //}

        //public override List<CodeEditor2.CodeEditor.PopupItem> GetPopupItems(ulong version, int index)
        //{
        //    return VerilogCommon.AutoComplete.GetPopupItems(this,VerilogParsedDocument, version, index);
        //}
        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            var parsed = VerilogParsedDocument;

            return VerilogCommon.AutoComplete.GetPopupItem(this, parsed, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override SystemVerilogCore.Documents.VerilogCompletionContext? GetAutoCompleteItems(int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index);
        }

    }
}
