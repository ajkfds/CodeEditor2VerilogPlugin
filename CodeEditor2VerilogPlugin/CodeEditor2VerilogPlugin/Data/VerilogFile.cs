using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.Data
{
    public class VerilogFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {
        public new static VerilogFile Create(string relativePath, CodeEditor2.Data.Project project)
        {
            VerilogFile fileItem = new VerilogFile();
            fileItem.Project = project;
            fileItem.RelativePath = relativePath;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                fileItem.Name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                fileItem.Name = relativePath;
            }

            return fileItem;
        }

        public static VerilogFile CreateSystemVerilog(string relativePath, CodeEditor2.Data.Project project)
        {
            VerilogFile fileItem = new VerilogFile();
            fileItem.Project = project;
            fileItem.RelativePath = relativePath;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                fileItem.Name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                fileItem.Name = relativePath;
            }
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
                return document;
            }
            protected set
            {
                if (value != null &&  value as CodeEditor2.CodeEditor.CodeDocument == null) System.Diagnostics.Debugger.Break();
                document = value as CodeEditor2.CodeEditor.CodeDocument ;
            }
        }

        // accept new Parsed Document
        public override void AcceptParsedDocument(ParsedDocument newParsedDocument)
        {
            ParsedDocument oldParsedDocument = ParsedDocument;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            // copy include files

            ParsedDocument = newParsedDocument;

            if(VerilogParsedDocument == null)
            {
                Update();
                return;
            }

            // Register New Building Block
            foreach (BuildingBlock buildingBlock in VerilogParsedDocument.Root.BuldingBlocks.Values)
            {
                if (ProjectProperty.HasRegisteredBuildingBlock(buildingBlock.Name))
                {   // swap building block
                    BuildingBlock? module = buildingBlock as Module;
                    if (module == null) continue;

                    BuildingBlock? registeredModule = ProjectProperty.GetBuildingBlock(module.Name)as Module;
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
            //foreach(var includeFile in VerilogParsedDocument.IncludeFiles.Values)
            //{
            //    if (!headerItems.ContainsKey(includeFile.ID)) continue;
            //    Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
            //    item.CodeDocument.CopyColorMarkFrom(includeFile.VerilogParsedDocument.CodeDocument);
            //}

            Update(); // eliminated here
            System.Diagnostics.Debug.Print("### Verilog File Parsed "+ID);
        }

        internal static void updateIncludeFiles(Verilog.ParsedDocument parsedDocument,ItemList items)
        {
            // create id table
            Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            foreach (var item in items.Values)
            {
                Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
                if (vh == null) continue;
                headerItems.Add(item.ID, vh);
            }

            foreach (var includeFile in parsedDocument.IncludeFiles.Values)
            {
                if (!headerItems.ContainsKey(includeFile.ID)) continue;
                Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
                item.CodeDocument.CopyColorMarkFrom(includeFile.VerilogParsedDocument.CodeDocument);
                updateIncludeFiles(includeFile.VerilogParsedDocument, item.Items);
            }

            //foreach (var item in headerItems.Values)
            //{
            //    updateIncludeFiles(item.VerilogParsedDocument, item.Items);
            //    item.AcceptParsedDocument(item.VerilogParsedDocument);
            //}
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
                if(document == null) document = new CodeEditor.CodeDocument(this);
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

        public ParsedDocument? GetInstancedParsedDocument(string parameterId)
        {
            cleanWeakRef();
            ParsedDocument ret;
            if(parameterId == "")
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

        public void RegisterInstanceParsedDocument(string id, ParsedDocument parsedDocument,VerilogModuleInstance moduleInstance)
        {
            System.Diagnostics.Debug.Print("#### Try RegisterInstanceParsedDocument " + id);
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
                        System.Diagnostics.Debug.Print("#### Try RegisterInstanceParsedDocument.Update " + id);
                    }
                    else
                    {
                        instancedParsedDocumentRefs.Add(id, new WeakReference<ParsedDocument>(parsedDocument));
                        Project.AddReparseTarget(moduleInstance);
                        System.Diagnostics.Debug.Print("#### Try RegisterInstanceParsedDocument.Add " + id);
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
                foreach(var r in instancedParsedDocumentRefs)
                {
                    if (!r.Value.TryGetTarget(out ret)) removeKeys.Add(r.Key);
                }
                foreach(string key in removeKeys)
                {
                    instancedParsedDocumentRefs.Remove(key);
                }
            }
        }

        private List<System.WeakReference<Data.VerilogModuleInstance>> moduleInstanceRefs
            = new List<WeakReference<VerilogModuleInstance>>();

        public void RegisterModuleInstance(VerilogModuleInstance verilogModuleInstance)
        {
            moduleInstanceRefs.Add(new WeakReference<VerilogModuleInstance>(verilogModuleInstance));
        }

        public void RemoveModuleInstance(VerilogModuleInstance verilogModuleInstance)
        {
            for(int i = 0; i< moduleInstanceRefs.Count; i++)
            {
                VerilogModuleInstance? ret;
                if (!moduleInstanceRefs[i].TryGetTarget(out ret)) continue;
                if (ret == verilogModuleInstance) moduleInstanceRefs.Remove(moduleInstanceRefs[i]);
            }
        }

        public override void Dispose()
        {
            if(ParsedDocument != null)
            {
                foreach(var incFile in VerilogParsedDocument.IncludeFiles.Values)
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

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode createNode()
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

        public override CodeEditor2.CodeEditor.PopupItem GetPopupItem(ulong version, int index)
        {
            return VerilogCommon.AutoComplete.GetPopupItem(this, VerilogParsedDocument, version, index);
        }

        public override List<CodeEditor2.CodeEditor.ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }
        public override List<CodeEditor2.CodeEditor.AutocompleteItem> GetAutoCompleteItems(int index, out string cantidateWord)
        {
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out cantidateWord);
        }

    }
}
