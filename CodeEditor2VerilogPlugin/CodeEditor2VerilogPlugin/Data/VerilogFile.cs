using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CodeEditor2;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using DynamicData;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using Splat;

namespace pluginVerilog.Data
{
    public class VerilogFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {
        public new static VerilogFile Create(string relativePath, CodeEditor2.Data.Project project)
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

            CodeEditor2.FileTypes.FileType fileType = CodeEditor2.Global.FileTypes[FileTypes.VerilogFile.TypeID];
            VerilogFile fileItem = new VerilogFile() { Name = name, Project = project, RelativePath = relativePath };

            return fileItem;
        }


        public static VerilogFile CreateSystemVerilog(string relativePath, CodeEditor2.Data.Project project)
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
            return fileItem;
        }

        public bool SystemVerilog { get; set; } = false;

        public string FileID
        {
            get
            {
                return RelativePath;
            }
        }

        public override CodeEditor2.CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                if (document == null)
                {
                    try
                    {
                        loadDocumentFromFile();
                    }
                    catch
                    {
                        document = null;
                    }
                }
                if (document == null) throw new Exception();
                return document;
            }
            protected set
            {
                if (value != null && value as CodeEditor2.CodeEditor.CodeDocument == null) System.Diagnostics.Debugger.Break();
                document = value as CodeEditor2.CodeEditor.CodeDocument;
            }
        }

        // accept new Parsed Document
        public override void AcceptParsedDocument(ParsedDocument newParsedDocument)
        {
            ParsedDocument? oldParsedDocument = ParsedDocument;
            if (oldParsedDocument == newParsedDocument) return;

            ParsedDocument = newParsedDocument;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            if (VerilogParsedDocument == null)
            {
                Update();
                return;
            }
            CodeDocument.CopyColorMarkFrom(VerilogParsedDocument.CodeDocument);

            // Register New Building Block
            foreach (BuildingBlock buildingBlock in VerilogParsedDocument.Root.BuldingBlocks.Values)
            {
                if (ProjectProperty.HasRegisteredBuildingBlock(buildingBlock.Name))
                {   // swap building block
                    BuildingBlock? module = buildingBlock as Module;
                    if (module == null) continue;

                    BuildingBlock? registeredModule = ProjectProperty.GetBuildingBlock(module.Name) as Module;
                    if (registeredModule == null) continue;
                    if (registeredModule.File == null) continue;
                    if (registeredModule.File.RelativePath == module.File.RelativePath) continue;

                    continue;
                }

                // register new parsedDocument
                ProjectProperty.RegisterBuildingBlock(buildingBlock.Name, buildingBlock, this);
            }

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument != null)
            {
                ReparseRequested = vParsedDocument.ReparseRequested;
            }


            updateIncludeFiles(VerilogParsedDocument, Items);

            Update(); // eliminated here
            //System.Diagnostics.Debug.Print("### Verilog File Parsed "+ID);

            // update navigate menu icons
            // update current node to update include file icon
            CodeEditor2.NavigatePanel.NavigatePanelNode node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            if (node != null) node.UpdateVisual();
        }

        internal static void updateIncludeFiles(Verilog.ParsedDocument parsedDocument, ItemList items)
        {
            // create id table
            Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            foreach (var item in items.Values)
            {
                Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
                if (vh == null) continue;
                headerItems.Add(item.ID, vh);
            }

            // get file selected in text editor
            CodeEditor2.NavigatePanel.NavigatePanelNode? node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            CodeEditor2.Data.Item? currentItem = node?.Item;
            CodeEditor2.Data.ITextFile? currentTextFile = Controller.CodeEditor.GetTextFile();


            foreach (var includeFile in parsedDocument.IncludeFiles.Values)
            {
                if (!headerItems.ContainsKey(includeFile.ID)) continue;
                Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
                item.CodeDocument.CopyColorMarkFrom(includeFile.VerilogParsedDocument.CodeDocument);

                // If this include file is selected in the editor, update the editor display.
                if (item == currentItem && currentTextFile != null)
                {
                    Controller.CodeEditor.Refresh();
                    Controller.MessageView.Update(includeFile.VerilogParsedDocument);
                }

                includeFile.NavigatePanelNode.UpdateVisual();

                // update nested include file
                updateIncludeFiles(includeFile.VerilogParsedDocument, item.Items);
            }
        }


        public override void LoadFormFile()
        {
            loadDocumentFromFile();
            AcceptParsedDocument(null);
            Project.AddReparseTarget(this);
            if (NavigatePanelNode != null) NavigatePanelNode.Update();
        }

        private void loadDocumentFromFile()
        {
            try
            {
                if (document == null) document = new CodeEditor.CodeDocument(this);
                using (System.IO.StreamReader sr = new System.IO.StreamReader(Project.GetAbsolutePath(RelativePath)))
                {
                    loadedFileLastWriteTime = System.IO.File.GetLastWriteTime(AbsolutePath);

                    string text = sr.ReadToEnd();
                    document.Replace(0, document.Length, 0, text);
                    document.ClearHistory();
                    document.Clean();
                }
            }
            catch
            {
                document = null;
            }
        }

        private Dictionary<string, System.WeakReference<ParsedDocument>> instancedParsedDocumentRefs = new Dictionary<string, WeakReference<ParsedDocument>>();

        internal string DebugInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("## " + Name + "\r\n");

            Verilog.ParsedDocument? parsedDocument = VerilogParsedDocument;
            sb.Append(" path " + ",ID:" + ObjectID + ",ReparseRequested:" + ReparseRequested);
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

                sb.Append(" instance key:" + kvPair.Key + ",ID:" + pDoc.ObjectID);
                if (iParsedDocument != null)
                {
                    sb.Append(",pd.ReparseRequested:" + iParsedDocument.ReparseRequested + ",pd.Version" + iParsedDocument.Version);
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        public ParsedDocument? GetInstancedParsedDocument(string parameterId)
        {
            cleanWeakRef();
            ParsedDocument ret;
            if (parameterId == "")
            {
                return ParsedDocument;
            }
            else
            {
                lock (instancedParsedDocumentRefs)
                {
                    if (instancedParsedDocumentRefs.ContainsKey(parameterId))
                    {
                        if (instancedParsedDocumentRefs[parameterId].TryGetTarget(out ret))
                        {
                            return ret;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public void RegisterInstanceParsedDocument(string id, ParsedDocument parsedDocument, InstanceTextFile moduleInstance)
        {
            System.Diagnostics.Debug.Print("#### RegisterInstanceParsedDocument " + id + "::" + parsedDocument.ObjectID);
            cleanWeakRef();
            if (id == "")
            {
                ParsedDocument = parsedDocument;
            }
            else
            {
                lock (instancedParsedDocumentRefs)
                {
                    if (instancedParsedDocumentRefs.ContainsKey(id))
                    {
                        instancedParsedDocumentRefs[id] = new WeakReference<ParsedDocument>(parsedDocument);
                        System.Diagnostics.Debug.Print("#### RegisterInstanceParsedDocument replace to " + id + "::" + parsedDocument.ObjectID);
                    }
                    else
                    {
                        instancedParsedDocumentRefs.Add(id, new WeakReference<ParsedDocument>(parsedDocument));
                        //Project.AddReparseTarget(moduleInstance);
                        System.Diagnostics.Debug.Print("#### Try RegisterInstanceParsedDocument.Add " + id + "::" + parsedDocument.ObjectID);
                    }
                }
            }
        }

        private void cleanWeakRef()
        {
            List<string> removeKeys = new List<string>();
            ParsedDocument? ret;
            lock (instancedParsedDocumentRefs)
            {
                foreach (var r in instancedParsedDocumentRefs)
                {
                    if (!r.Value.TryGetTarget(out ret)) removeKeys.Add(r.Key);
                }
                foreach (string key in removeKeys)
                {
                    instancedParsedDocumentRefs.Remove(key);
                    System.Diagnostics.Debug.Print("### remove key: " + key);
                }
            }
        }

        private List<System.WeakReference<Data.InstanceTextFile>> moduleInstanceRefs
            = new List<WeakReference<InstanceTextFile>>();

        public void RegisterModuleInstance(InstanceTextFile verilogModuleInstance)
        {
            moduleInstanceRefs.Add(new WeakReference<InstanceTextFile>(verilogModuleInstance));
        }

        public void RemoveModuleInstance(InstanceTextFile verilogModuleInstance)
        {
            for (int i = 0; i < moduleInstanceRefs.Count; i++)
            {
                InstanceTextFile? ret;
                if (!moduleInstanceRefs[i].TryGetTarget(out ret)) continue;
                if (ret == verilogModuleInstance) moduleInstanceRefs.Remove(moduleInstanceRefs[i]);
            }
        }

        public override void Dispose()
        {
            if (VerilogParsedDocument != null)
            {
                foreach (var incFile in VerilogParsedDocument.IncludeFiles.Values)
                {
                    incFile.Dispose();
                }
            }
            moduleInstanceRefs.Clear();
            base.Dispose();
        }

        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
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

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode)
        {
            return new Parser.VerilogParser(this, parseMode);
        }

        // update sub-items from ParsedDocument
        public override void Update()
        {
            VerilogCommon.Updater.Update(this);
            Dispatcher.UIThread.Post(
                new Action(() =>
                {
                    NavigatePanelNode.UpdateVisual();
                })
                );

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

        public override PopupItem GetPopupItem(ulong version, int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, VerilogParsedDocument, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }
        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string? candidateWord)
        {
            candidateWord = "";
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out candidateWord);
        }

    }
}
