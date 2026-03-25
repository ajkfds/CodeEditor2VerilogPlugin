using Avalonia.Threading;
//using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.Controller;
using System.Text.Json.Serialization;
using System.Threading;

namespace pluginVerilog.Data
{
    public class VerilogHeaderInstance : InstanceTextFile, IVerilogRelatedFile
    {

        protected VerilogHeaderInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {
            idLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            moduleNameLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            nodeRefDictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        
        public static VerilogHeaderInstance? Create(
            string relativePath,
            string name,
            IndexReference instancedReference,
            IVerilogRelatedFile parentFile,
            CodeEditor2.Data.Project project,
            string id)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            CodeEditor2.Data.Item? fileItem = project.GetItem(relativePath);
            VerilogHeaderFile? vhFile = fileItem as VerilogHeaderFile;

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
            VerilogHeaderInstance instance = new VerilogHeaderInstance(vhFile) {
                Name = name,
                Project = project,
                RelativePath = relativePath 
            };

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

        private readonly ReaderWriterLockSlim idLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private string id = string.Empty;
        public override string ID
        {
            get
            {
                idLock.EnterReadLock();
                try
                {
                    return id;
                }
                finally
                {
                    idLock.ExitReadLock();
                }
            }
            set
            {
                idLock.EnterWriteLock();
                try
                {
                    id = value;
                }
                finally
                {
                    idLock.ExitWriteLock();
                }
            }
        }

        public bool SystemVerilog { get { return RootFile.SystemVerilog; } }

        public bool ReplaceBy(
            VerilogHeaderInstance file
            )
        {
            if (file == null) return false;
            if (!IsSameAs(file as File)) return false;

            // Use write lock for updating state
            textFileLock.EnterWriteLock();
            try
            {
                parsedDocument = file.parsedDocument;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }

            // CodeDocument is from source file, so read lock is sufficient
            textFileLock.EnterReadLock();
            var codeDoc = CodeDocument;
            textFileLock.ExitReadLock();

            file.textFileLock.EnterReadLock();
            var fileCodeDoc = file.CodeDocument;
            file.textFileLock.ExitReadLock();

            if (codeDoc != null && fileCodeDoc != null)
            {
                if (codeDoc.Version == fileCodeDoc.Version)
                {
                    codeDoc.CopyColorMarkFrom(fileCodeDoc);
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


        private readonly ReaderWriterLockSlim moduleNameLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private string moduleName = string.Empty;
        public string ModuleName
        {
            get
            {
                moduleNameLock.EnterReadLock();
                try
                {
                    return moduleName;
                }
                finally
                {
                    moduleNameLock.ExitReadLock();
                }
            }
            set
            {
                moduleNameLock.EnterWriteLock();
                try
                {
                    moduleName = value;
                }
                finally
                {
                    moduleNameLock.ExitWriteLock();
                }
            }
        }


        public string ParameterId
        {
            get
            {
                return "";
            }
        }

        private Data.VerilogHeaderFile? SourceVerilogFile
        {
            get
            {
                return SourceTextFile as VerilogHeaderFile;
            }
        }



        public override void Close()
        {
            textFileLock.EnterReadLock();
            var parsed = VerilogParsedDocument;
            textFileLock.ExitReadLock();

            if (parsed != null) parsed.ReloadIncludeFiles();
            //SourceVerilogFile.Close();
        }

        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    if(parsedDocument==null) throw new Exception();
                    return parsedDocument;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                Verilog.ParsedDocument? vParsedDocument = value as Verilog.ParsedDocument;
                if (vParsedDocument == null) throw new Exception();

                textFileLock.EnterWriteLock();
                try
                {
                    parsedDocument = vParsedDocument;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        protected Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>> nodeRefDictionary
            = new Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>>();
        private readonly ReaderWriterLockSlim nodeRefDictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public override CodeEditor2.NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
            {
                nodeRefDictionaryLock.EnterWriteLock();
                try
                {
                    CodeEditor2.NavigatePanel.NavigatePanelNode? node = null;
                    List<WeakReference<CodeEditor2.Data.Item?>> disposeRefs = new List<WeakReference<CodeEditor2.Data.Item?>>();

                    // search parent based table
                    foreach (var pair in nodeRefDictionary)
                    {
                        var parentRef = pair.Key;
                        if (!parentRef.TryGetTarget(out var parent))
                        {
                            disposeRefs.Add(parentRef);
                            continue;
                        }
                        if (parent != Parent) continue;

                        var nodeRef = pair.Value;
                        if (nodeRef.TryGetTarget(out node)) break;
                    }

                    // remove unconnected weakRefs
                    foreach (var disposeRef in disposeRefs)
                    {
                        nodeRefDictionary.Remove(disposeRef);
                    }

                    if (node == null)
                    {
                        node = CreateNode();
                        if (node == null) throw new Exception();

                        WeakReference<CodeEditor2.Data.Item?> parent = new WeakReference<CodeEditor2.Data.Item?>(Parent);
                        nodeRefDictionary.Add(parent, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node));
                    }

                    return node;
                }
                finally
                {
                    nodeRefDictionaryLock.ExitWriteLock();
                }
            }
            protected set
            {
                nodeRefDictionaryLock.EnterWriteLock();
                try
                {
                    WeakReference<CodeEditor2.Data.Item?>? indexRef = null;

                    // search parent based table
                    foreach (var pair in nodeRefDictionary)
                    {
                        var parentRef = pair.Key;
                        if (!parentRef.TryGetTarget(out var parent))
                        {
                            continue;
                        }
                        if (parent != Parent) continue;
                        indexRef = parentRef;
                    }

                    if (indexRef != null)
                    {
                        nodeRefDictionary.Remove(indexRef);
                    }
                    WeakReference<CodeEditor2.Data.Item?> parentNewRef = new WeakReference<CodeEditor2.Data.Item?>(Parent);
                    nodeRefDictionary.Add(parentNewRef, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(value));
                }
                finally
                {
                    nodeRefDictionaryLock.ExitWriteLock();
                }
            }
        }

        public override async System.Threading.Tasks.Task SaveAsync()
        {
            if(SourceTextFile == null) return;
            await SourceTextFile.SaveAsync();
        }

        public Verilog.ParsedDocument VerilogParsedDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    Verilog.ParsedDocument? vParsedDocument = parsedDocument as Verilog.ParsedDocument;
                    if (vParsedDocument == null) throw new Exception();
                    return vParsedDocument;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public override async System.Threading.Tasks.Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            //{
            //    Data.VerilogFile source = SourceVerilogFile;
            //    if (source == null) return;
            //    source.RegisterInstanceParsedDocument(ParameterId, newParsedDocument, this);
            //}

            //Data.VerilogFile source = SourceVerilogFile;
            //if (source == null) return;

            //if (ParameterOverrides.Count == 0)
            //{
            //    source.AcceptParsedDocument(newParsedDocument);


//            ReparseRequested = VerilogParsedDocument.ReparseRequested;
            await UpdateAsync();
        }




        [JsonIgnore]
        public ProjectProperty ProjectProperty
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


        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            NavigatePanel.VerilogHeaderInstanceNode node = new NavigatePanel.VerilogHeaderInstanceNode(this,Project);
            return node;
        }

        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            Data.IVerilogRelatedFile? parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode,token);
        }

        // update sub-items from ParsedDocument
        public override async System.Threading.Tasks.Task UpdateAsync()
        {
            await base.UpdateAsync();

            if (Parent == null)
            {
                await VerilogCommon.Updater.UpdateAsync(this);
            }
            CodeEditor2.Data.Item? item = Parent;
            while (true)
            {
                if (item == null) break;
                if (item is VerilogHeaderFile)
                {
                    item = item.Parent;
                }
                else
                {
                    break;
                }
            }

            if (item != null && item is VerilogFile)
            {
                VerilogFile vFile = (VerilogFile)item;
                await VerilogCommon.Updater.UpdateAsync(vFile);
            }
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

        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string cantidateWord)
        {
            textFileLock.EnterReadLock();
            var parsed = VerilogParsedDocument;
            textFileLock.ExitReadLock();

            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, parsed, index, out cantidateWord);
        }



    }
}
