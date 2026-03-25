using Avalonia.Threading;
using CodeEditor2;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using DynamicData;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class VerilogFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {

        public new static async Task<VerilogFile> CreateAsync(string relativePath, CodeEditor2.Data.Project project)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }
            System.Diagnostics.Debug.Print(relativePath);
            CodeEditor2.FileTypes.FileType fileType = CodeEditor2.Global.FileTypes[FileTypes.VerilogFile.TypeID];
            VerilogFile fileItem = new VerilogFile() { Name = name, Project = project, RelativePath = relativePath };
            await fileItem.FileCheck();

            return fileItem;
        }

        ~VerilogFile()
        {
            //if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();

        }
        public override string Key
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return Verilog.ParsedDocument.KeyGenerator(this, null, null);
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }
        static VerilogFile()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
            CodeEditor2.Data.Item.PolymorphicResolver.DerivedTypes.Add(new JsonDerivedType(typeof(VerilogFile)));
        }

        public static async Task<VerilogFile> CreateSystemVerilog(string relativePath, CodeEditor2.Data.Project project)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }

            CodeEditor2.FileTypes.FileType fileType = CodeEditor2.Global.FileTypes[FileTypes.SystemVerilogFile.TypeID];
            VerilogFile fileItem = new VerilogFile() { Name = name, Project = project, RelativePath = relativePath };
            fileItem.SystemVerilog = true;

            await fileItem.FileCheck();
            return fileItem;
        }


        /// <summary>
        /// indicate this file is system verilog file.
        /// true : system verilog file
        /// failse : verilog file
        /// </summary>

        private bool systemVerilog = false;
        public bool SystemVerilog
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return systemVerilog;
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
                    systemVerilog = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        public string FileID
        {
            get
            {
                return RelativePath;
            }
        }

        protected override void CreateCodeDocument()
        {
            document = new pluginVerilog.CodeEditor.CodeDocument(this);
        }

        /// <summary>
        /// Accdept new Parsed Document for this Verilog File
        /// </summary>
        /// <param name="newParsedDocument"></param>
        public override async Task AcceptParsedDocumentAsync(ParsedDocument? newParsedDocument)
        {
            ParsedDocument? oldParsedDocument;
            textFileLock.EnterReadLock();
            oldParsedDocument = ParsedDocument;
            textFileLock.ExitReadLock();

            if (oldParsedDocument == newParsedDocument) return;

            // swap ParsedDocument
            textFileLock.EnterWriteLock();
            try
            {
                ParsedDocument = newParsedDocument;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }

            Verilog.ParsedDocument? vParsedDocument;
            textFileLock.EnterReadLock();
            try
            {
                vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            if (vParsedDocument == null)
            {
                await UpdateAsync();
                return;
            }

            CodeEditor.CodeDocument? codeDoc;
            textFileLock.EnterReadLock();
            try
            {
                codeDoc = document as pluginVerilog.CodeEditor.CodeDocument;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            if (codeDoc == null) return;
            if (newParsedDocument == null) return;

            textFileLock.EnterWriteLock();
            try
            {
                // Register New Building Block
                if (vParsedDocument.Root != null)
                {
                    foreach (BuildingBlock buildingBlock in vParsedDocument.Root.BuildingBlocks.Values)
                    {
                        // register new parsedDocument
                        ProjectProperty.RegisterBuildingBlock(buildingBlock.Name, buildingBlock, this);
                    }
                }

                reparseRequested = vParsedDocument.ReparseRequested;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }

            await updateIncludeFilesAsync(vParsedDocument, Items);

            await UpdateAsync();

            // update Navigate panel node visual for this item
            _ = Task.Run(async() => {
                await NavigatePanelNode.UpdateAsync();
                });
        }


        internal static async System.Threading.Tasks.Task updateIncludeFilesAsync(Verilog.ParsedDocument parsedDocument, ItemList items)
        {
            // create id table
            Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            foreach (var item in items)
            {
                Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
                if (vh == null) continue;
                headerItems.Add(item.ID, vh);
            }

            // get file selected in text editor
            CodeEditor2.NavigatePanel.NavigatePanelNode? node = await CodeEditor2.Controller.NavigatePanel.GetSelectedNodeAsync();
            CodeEditor2.Data.Item? currentItem = node?.Item;
            CodeEditor2.Data.ITextFile? currentTextFile = await Controller.CodeEditor.GetTextFileAsync
               ();


            foreach (var includeFile in parsedDocument.IncludeFiles.Values)
            {
                if (!headerItems.ContainsKey(includeFile.ID)) continue;
                Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
                if (item.CodeDocument == null) continue;

                item.CodeDocument.CopyColorMarkFrom(includeFile.VerilogParsedDocument.CodeDocument);

                // If this include file is selected in the editor, update the editor display.
                if (item == currentItem && currentTextFile != null)
                {
                    Controller.CodeEditor.PostRefresh();
                    Controller.MessageView.Update(includeFile.VerilogParsedDocument);
                }

//                includeFile.NavigatePanelNode.UpdateVisual();

                // update nested include file
                await updateIncludeFilesAsync(includeFile.VerilogParsedDocument, item.Items);
            }
        }

        public SemaphoreSlim BaseParseSemapho = new SemaphoreSlim(1,1);



        private ConcurrentDictionary<string, System.WeakReference<ParsedDocument>> instancedParsedDocumentRefs = new ConcurrentDictionary<string, WeakReference<ParsedDocument>>();

        internal string DebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("## " + Name + "\r\n");

            textFileLock.EnterReadLock();
            Verilog.ParsedDocument? parsedDocument = ParsedDocument as Verilog.ParsedDocument;
            textFileLock.ExitReadLock();

            if (parsedDocument != null)
            {
                sb.Append(",pd.ReparseRequested:" + parsedDocument.ReparseRequested + ",pd.Version" + parsedDocument.Version);
            }
            sb.Append("\r\n");
            foreach (var kvPair in instancedParsedDocumentRefs)
            {
                ParsedDocument? pDoc;
                if (!kvPair.Value.TryGetTarget(out pDoc)) continue;
                Verilog.ParsedDocument? iParsedDocument = pDoc as Verilog.ParsedDocument;

                if (iParsedDocument != null)
                {
                    sb.Append(",pd.ReparseRequested:" + iParsedDocument.ReparseRequested + ",pd.Version" + iParsedDocument.Version);
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        public ParsedDocument? GetInstancedParsedDocument(string key)
        {
            ParsedDocument? ret;
            if (key == "")
            {
                textFileLock.EnterReadLock();
                try
                {
                    return ParsedDocument;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            else
            {
                if(instancedParsedDocumentRefs.TryGetValue(key,out WeakReference < ParsedDocument >? weakRef))
                {
                    if(weakRef == null)
                    {
                        instancedParsedDocumentRefs.TryRemove(key, out _);
                        return null;
                    }
                    else
                    {
                        weakRef.TryGetTarget(out ret);
                        return ret;
                    }
                }
                else
                {
                    instancedParsedDocumentRefs.TryRemove(key, out _);
                    return null;
                }
            }
        }

        public void RegisterInstanceParsedDocument(string id, ParsedDocument parsedDocument, InstanceTextFile moduleInstance)
        {
            if (id == "")
            {
                textFileLock.EnterWriteLock();
                try
                {
                    ParsedDocument = parsedDocument;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
            else
            {
                instancedParsedDocumentRefs.AddOrUpdate(id,new WeakReference<ParsedDocument>(parsedDocument),(key,oldValue) => new WeakReference<ParsedDocument>(parsedDocument));
            }
        }

        public void CleanWeakRef()
        {
            ParsedDocument? ret;
            foreach (var r in instancedParsedDocumentRefs)
            {
                if (!r.Value.TryGetTarget(out ret)) instancedParsedDocumentRefs.TryRemove(r.Key, out _);
            }
        }

        public override void Dispose()
        {
            Verilog.ParsedDocument? vParsedDocument;
            textFileLock.EnterReadLock();
            try
            {
                vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            if (vParsedDocument != null)
            {
                foreach (var incFile in vParsedDocument.IncludeFiles.Values)
                {
                    incFile.Dispose();
                }
            }
            base.Dispose();
        }

        [JsonInclude]
        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument { get; set; }

        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return ParsedDocument as Verilog.ParsedDocument;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public ProjectProperty ProjectProperty
        {
            get
            {
                ProjectProperty? projectProperty = Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();
                return projectProperty;
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
            NavigatePanel.VerilogFileNode node = new NavigatePanel.VerilogFileNode(this);
            return node;
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.VerilogInstanceParser(this, parseMode, token);
        }

        // update sub-items from ParsedDocument
        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => UpdateAsync());
                return;
            }

            await VerilogCommon.Updater.UpdateAsync(this);
            NavigatePanelNode.UpdateVisual();
            if (CodeEditor2.Controller.NavigatePanel.GetSelectedFile() == this)
            {
                CodeEditor2.Controller.CodeEditor.PostRefresh();
                if (ParsedDocument != null) CodeEditor2.Controller.MessageView.Update(ParsedDocument);
            }
        }

        public override async Task ParseHierarchyAsync(Action<ITextFile> action)
        {
            await Tool.ParseHierarchy.ParseAsync(this, Tool.ParseHierarchy.ParseMode.ForceAllFiles);
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

        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            Verilog.ParsedDocument? parsedDoc = VerilogParsedDocument;
            if (parsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, parsedDoc, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            List<ToolItem> toolItems = new List<ToolItem>();
            if (CustomizeTooltem != null)
            {
                CustomizeTooltem?.Invoke(toolItems);
            }
            Verilog.ParsedDocument? parsedDoc = VerilogParsedDocument;
            if (parsedDoc == null) return toolItems;
            List<ToolItem> toolItems2 = VerilogCommon.AutoComplete.GetToolItems(this, index);
            foreach(ToolItem item in toolItems2)
            {
                toolItems.Add(item);
            }
            return toolItems;
        }
        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string? candidateWord)
        {
            candidateWord = "";
            Verilog.ParsedDocument? parsedDoc = VerilogParsedDocument;
            if (parsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, parsedDoc, index, out candidateWord);
        }


    }
}
