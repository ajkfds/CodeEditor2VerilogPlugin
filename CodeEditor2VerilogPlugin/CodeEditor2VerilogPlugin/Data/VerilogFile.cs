using Avalonia.Controls.Shapes;
using Avalonia.Media;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using pluginVerilog.CodeEditor;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using Splat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

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

        public override string Key
        {
            get
            {
                return Verilog.ParsedDocument.KeyGenerator(this, null, null);
            }
        }
        static VerilogFile()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
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


        /// <summary>
        /// indicate this file is system verilog file.
        /// true : system verilog file
        /// failse : verilog file
        /// </summary>

        public bool SystemVerilog { get; set; } = false;

        public string FileID
        {
            get
            {
                return RelativePath;
            }
        }

        /// <summary>
        /// CodeDocument Object to keep the text data of the file.
        /// only single instance is created for a file, and the same instance is used for all IVerilogRelatedFile instances.
        /// </summary>
        public override CodeEditor2.CodeEditor.CodeDocument? CodeDocument
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
                if (value is not pluginVerilog.CodeEditor.CodeDocument) throw new Exception();
                document = value as CodeEditor2.CodeEditor.CodeDocument;
            }
        }

        /// <summary>
        /// Accdept new Parsed Document for this Verilog File
        /// </summary>
        /// <param name="newParsedDocument"></param>
        public override void AcceptParsedDocument(ParsedDocument? newParsedDocument)
        {
            ParsedDocument? oldParsedDocument = ParsedDocument;
            if (oldParsedDocument == newParsedDocument) return;

            // swap ParsedDocument
            ParsedDocument = newParsedDocument;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            if (VerilogParsedDocument == null)
            {
                Update();
                return;
            }
            if(VerilogParsedDocument.CodeDocument != null && CodeDocument != null) CodeDocument.CopyColorMarkFrom(VerilogParsedDocument.CodeDocument);

            // Register New Building Block
            if (VerilogParsedDocument.Root != null)
            {
                foreach (BuildingBlock buildingBlock in VerilogParsedDocument.Root.BuildingBlocks.Values)
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
            }

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument != null)
            {
                ReparseRequested = vParsedDocument.ReparseRequested;
            }


            updateIncludeFiles(VerilogParsedDocument, Items);

            Update();

            // update Navigate panel node visual for this item
            NavigatePanelNode.UpdateVisual();

            Task.Run(
                async () =>
                {
                    try
                    {
                        await CreateCashe();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debugger.Break();
                        Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
                    }
                }
            );


        }

        internal static void updateIncludeFiles(Verilog.ParsedDocument parsedDocument, ItemList items)
        {
            // create id table
            Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            lock (items)
            {
                foreach (var item in items.Values)
                {
                    Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
                    if (vh == null) continue;
                    headerItems.Add(item.ID, vh);
                }
            }

            // get file selected in text editor
            CodeEditor2.NavigatePanel.NavigatePanelNode? node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            CodeEditor2.Data.Item? currentItem = node?.Item;
            CodeEditor2.Data.ITextFile? currentTextFile = Controller.CodeEditor.GetTextFile();


            foreach (var includeFile in parsedDocument.IncludeFiles.Values)
            {
                if (!headerItems.ContainsKey(includeFile.ID)) continue;
                Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
                if (item.CodeDocument == null) continue;

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
            catch (Exception e)
            {
                document = null;
                Console.Error.WriteLine("**error VerilogFile.loadDocumentFromFile");
                Console.Error.WriteLine("* " + AbsolutePath);
                Console.Error.WriteLine("* " + e.Message);
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

        public ParsedDocument? GetInstancedParsedDocument(string key)
        {
            cleanWeakRef();
            ParsedDocument ret;
            if (key == "")
            {
                return ParsedDocument;
            }
            else
            {
                lock (instancedParsedDocumentRefs)
                {
                    if (instancedParsedDocumentRefs.ContainsKey(key))
                    {
                        if (instancedParsedDocumentRefs[key].TryGetTarget(out ret))
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
            //            System.Diagnostics.Debug.Print("#### RegisterInstanceParsedDocument " + id + "::" + parsedDocument.ObjectID);
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
                        //                        System.Diagnostics.Debug.Print("#### RegisterInstanceParsedDocument replace to " + id + "::" + parsedDocument.ObjectID);
                    }
                    else
                    {
                        instancedParsedDocumentRefs.Add(id, new WeakReference<ParsedDocument>(parsedDocument));
                        //Project.AddReparseTarget(moduleInstance);
                        //                        System.Diagnostics.Debug.Print("#### Try RegisterInstanceParsedDocument.Add " + id + "::" + parsedDocument.ObjectID);
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

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.VerilogParser(this, parseMode, token);
        }

        // update sub-items from ParsedDocument
        public override void Update()
        {
            //if (!Dispatcher.UIThread.CheckAccess())
            //{
            //    throw new Exception();
            //}
            VerilogCommon.Updater.Update(this);
            NavigatePanelNode.UpdateVisual();

            Dispatcher.UIThread.Post(
                new Action(() =>
                {
                    try
                    {
                        if (CodeEditor2.Controller.NavigatePanel.GetSelectedFile() == this)
                        {
                            CodeEditor2.Controller.CodeEditor.Refresh();
                            if (ParsedDocument != null) CodeEditor2.Controller.MessageView.Update(ParsedDocument);
                        }
                    }
                    catch (Exception ex)
                    {
                        CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Colors.Red);
                    }
                })
            );

        }
        public async Task UpdateAsync()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Update();
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(
                    () =>
                    {
                        Update();
                    }
                );
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

        public override PopupItem? GetPopupItem(ulong version, int index)
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

        //public override string CasheId
        //{
        //    get
        //    {
        //        if (ParsedDocument == null) return "";

        //        //byte[] data = Encoding.UTF8.GetBytes(AbsolutePath);
        //        //byte[] hashBytes = XxHash64.Hash(data);
        //        //string hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        //        string hex = ParsedDocument.Key.Replace(@"\", "_").Replace("/", "_").Replace(":", "_").Replace(".", "_") + ".json";
        //        return hex;
        //    }
        //}

        public override CodeEditor2.CodeEditor.ParsedDocument? GetCashedParsedDocument()
        {
            if (!CodeEditor2.Global.ActivateCashe) return null;

            string path = Project.RootPath + System.IO.Path.DirectorySeparatorChar + ".cashe";
            if (!System.IO.Path.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.Diagnostics.Debug.Print("entry json " + path);

            path = path + System.IO.Path.DirectorySeparatorChar + CasheId;
            if (!System.IO.File.Exists(path)) return null;

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableInterface = true,
                    IgnoreSerializableAttribute = true
                }
            };
            var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);

            pluginVerilog.Verilog.ParsedDocument? parsedDocument;
            try
            {
                using (var reader = new StreamReader(path))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    parsedDocument = serializer.Deserialize<pluginVerilog.Verilog.ParsedDocument>(jsonReader);
                }
            }
            catch (Exception exception)
            {
                CodeEditor2.Controller.AppendLog("exp " + exception.Message);
                return null;
            }
            return parsedDocument;
        }

        public override async Task<bool> CreateCashe()
        {
            if (!CodeEditor2.Global.ActivateCashe) return true;

            if (VerilogParsedDocument == null) return false;

            await TextFile.CasheSemaphore.WaitAsync();

            Verilog.ParsedDocument casheObject = VerilogParsedDocument;
            string path = Project.RootPath + System.IO.Path.DirectorySeparatorChar + ".cashe";
            if (!System.IO.Path.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.Diagnostics.Debug.Print("entry json " + path);

            path = path + System.IO.Path.DirectorySeparatorChar + CasheId;

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    IgnoreSerializableInterface = true,
                    IgnoreSerializableAttribute = true
                }
            };

            try
            {
                System.Diagnostics.Debug.Print("start json "+path);
                var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);
                using (var writer = new StreamWriter(path))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    serializer.Serialize(jsonWriter, casheObject);
                }
                System.Diagnostics.Debug.Print("complete json " + path);
            }
            catch(Exception exception)
            {
                CodeEditor2.Controller.AppendLog("exp " + exception.Message);
            }
            finally
            {
                TextFile.CasheSemaphore.Release();
            }

            return true;
        }

    }
}
