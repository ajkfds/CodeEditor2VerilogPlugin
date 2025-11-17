using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class VerilogModuleInstance : InstanceTextFile, IVerilogRelatedFile
    {
        public required string ModuleName { set; get; }

        public required Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides;


        protected VerilogModuleInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }
        public static VerilogModuleInstance? Create(
            Verilog.ModuleItems.ModuleInstantiation moduleInstantiation
            )
        {
            CodeEditor2.Data.Project project = moduleInstantiation.GetInstancedBuildingBlockProject();
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            Data.IVerilogRelatedFile? file = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
            if (file == null) return null;

           CodeEditor2.Data.TextFile? textFile = file as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            VerilogModuleInstance fileItem = new VerilogModuleInstance(textFile) 
            {
                ModuleName = moduleInstantiation.SourceName,
                ParameterOverrides = moduleInstantiation.ParameterOverrides,
                Project = project,
                RelativePath = file.RelativePath,
                Name = moduleInstantiation.Name
            };
            if(moduleInstantiation.Project != project)
            {
                fileItem.ExternalProject = true;
            }

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                vFile.RegisterModuleInstance(fileItem);

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }

            fileItem.weakInstantiating = new WeakReference<ModuleInstantiation>(moduleInstantiation);
            return fileItem;
        }
        static VerilogModuleInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }
        public bool SystemVerilog { get; set; } = false;

        protected WeakReference<Verilog.ModuleItems.ModuleInstantiation> weakInstantiating;
        public Verilog.ModuleItems.ModuleInstantiation? GetParentInstanciatiation()
        {
            if (weakInstantiating == null) return null;
            weakInstantiating.TryGetTarget(out var moduleInstantiation);
            return moduleInstantiation;
        }
        public override string Key
        {
            get
            {
                return Verilog.ParsedDocument.KeyGenerator(this, ModuleName, ParameterOverrides);
            }
        }

        public Module? Module
        {
            get
            {
                if (VerilogParsedDocument?.Root == null) return null;
                if (VerilogParsedDocument.Root.BuildingBlocks.TryGetValue(ModuleName, out var block))
                {
                    return block as Module;
                }
                return null;
            }
        }

        public override string ID
        {
            get
            {
                return Key;
                //if (InstanceId == "")
                //{
                //    return RelativePath + ":" + ModuleName;
                //}
                //else
                //{
                //    return RelativePath + ":" + ModuleName + ":" + InstanceId;
                //}
            }
        }

        public bool ReplaceBy(
            Verilog.ModuleItems.ModuleInstantiation moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {
            System.Diagnostics.Debug.Print("### VerilogModuleInstance ReplaceBy "+ID);
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();

            Data.IVerilogRelatedFile? ivFile = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
            if (ivFile == null) return false;

            CodeEditor2.Data.File? file = ivFile as CodeEditor2.Data.File;
            if (file == null) return false;

            if (!IsSameAs(file)) return false;
            if (Project != project) return false;
            if (ModuleName != moduleInstantiation.SourceName) return false;

            string instanceKey = Verilog.ParsedDocument.KeyGenerator(ivFile, moduleInstantiation.SourceName, moduleInstantiation.ParameterOverrides);
            if (Key != instanceKey) return false;

            // re-register
            //disposeItems();

            ParameterOverrides = moduleInstantiation.ParameterOverrides;

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) return false;
                vFile.RegisterModuleInstance(this);
            }

            weakInstantiating = new WeakReference<ModuleInstantiation>(moduleInstantiation);
            return true;
        }

        public override CodeEditor2.CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                return SourceVerilogFile.CodeDocument;
            }
        }
        public override void Dispose()
        {
            disposeItems();
        }

        private void disposeItems()
        {
            if(VerilogParsedDocument != null)
            {
                if (ParsedDocument != null && ParameterOverrides.Count != 0)
                {
                    foreach (var incFile in VerilogParsedDocument.IncludeFiles.Values)
                    {
                        incFile.Dispose();
                    }
                }
            }

            ParsedDocument = null;
            SourceVerilogFile.RemoveModuleInstance(this);
        }

        public Data.VerilogFile SourceVerilogFile
        {
            get
            {
                Data.VerilogFile? vFile = SourceTextFile as VerilogFile;
                if (vFile == null) throw new Exception();
                return vFile;
            }
        }



        public override void Close()
        {
            if (VerilogParsedDocument != null) VerilogParsedDocument.ReloadIncludeFiles();
            SourceVerilogFile.Close();
        }

        private CodeEditor2.CodeEditor.ParsedDocument? parsedDocument = null;

        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                {
                    Data.VerilogFile source = SourceVerilogFile;
                    parsedDocument = source.GetInstancedParsedDocument(Key);
                }
                return parsedDocument;
            }
            set
            {
                parsedDocument = value;
            }
        }
        public override void Save()
        {
            if (CodeDocument == null) return;
            if (SourceTextFile == null) return;
            SourceTextFile.Save();
        }

        public override DateTime? LoadedFileLastWriteTime
        {
            get
            {
                if (SourceTextFile == null) return null;
                return SourceTextFile.LoadedFileLastWriteTime;
            }
        }

        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
            }
        }

        public override void AcceptParsedDocument(ParsedDocument newParsedDocument)
        {
            if (newParsedDocument == null) throw new Exception();
            Data.VerilogFile source = SourceVerilogFile;
            if (source == null) return;

            {
                ParsedDocument? oldParsedDocument = ParsedDocument;
                {
                    ParsedDocument = newParsedDocument; // should keep parseddocument 1st
                    source.RegisterInstanceParsedDocument(Key, newParsedDocument, this);
                    acceptParameterizedParsedDocument(newParsedDocument);
                }
                if (oldParsedDocument != null) oldParsedDocument.Dispose();
            }

            if(source.ParsedDocument != null)// && source.ParsedDocument.Version != newParsedDocument.Version)
            {
                Verilog.ParsedDocument vParsedDocument = (Verilog.ParsedDocument)newParsedDocument;
                if(vParsedDocument.Root != null && vParsedDocument.Root.BuildingBlocks.Count == 1)
                {
                    source.AcceptParsedDocument(newParsedDocument);
                }
                else
                {
                    source.ReparseRequested = true;
                }
            }

            NavigatePanelNode.UpdateVisual();
        }

        private void acceptParameterizedParsedDocument(ParsedDocument newParsedDocument)
        {

            // copy include files

            ParsedDocument = newParsedDocument;

            if (VerilogParsedDocument == null)
            {
                Update();
                return;
            }

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument != null)
            {
                ReparseRequested = vParsedDocument.ReparseRequested;
            }

            VerilogFile.updateIncludeFiles(VerilogParsedDocument, Items);

            Update(); // eliminated here
            //System.Diagnostics.Debug.Print("### Verilog File Parsed " + ID);
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
            NavigatePanel.VerilogModuleInstanceNode node = new NavigatePanel.VerilogModuleInstanceNode(this);
            nodeRef = new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node);
            return node;
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.VerilogInstanceParser(this, ModuleName, ParameterOverrides, parseMode,token);
//            return new Parser.VerilogParser(this.SourceVerilogFile , ModuleName, ParameterOverrides, parseMode);
        }


        // update sub-items from ParsedDocument
        public override void Update()
        {
            VerilogCommon.Updater.Update(this);
            NavigatePanelNode.UpdateVisual();

            Dispatcher.UIThread.Post(
                new Action(() =>
                {
                    if (CodeEditor2.Controller.NavigatePanel.GetSelectedFile() == this)
                    {
                        CodeEditor2.Controller.CodeEditor.Refresh();
                        if(ParsedDocument!=null) CodeEditor2.Controller.MessageView.Update(ParsedDocument);
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


        protected Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>> nodeRefDictionary
            = new Dictionary<WeakReference<Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>>();
        public override CodeEditor2.NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
            {
                CodeEditor2.NavigatePanel.NavigatePanelNode? node = null;
                List<WeakReference<CodeEditor2.Data.Item?>> disposeRefs = new List<WeakReference<Item?>>();

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
                foreach(var disposeRef in disposeRefs)
                {
                    nodeRefDictionary.Remove(disposeRef);
                }

                if(node == null)
                {
                    node = CreateNode();
                    if (node == null) throw new Exception();

                    WeakReference<CodeEditor2.Data.Item?> parent = new WeakReference<Item?>(Parent);
                    nodeRefDictionary.Add(parent, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node));
                }

                return node;
            }
            protected set
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

                if(indexRef != null)
                {
                    nodeRefDictionary.Remove(indexRef);
                }
                WeakReference<CodeEditor2.Data.Item?> parentNewRef = new WeakReference<Item?>(Parent);
                nodeRefDictionary.Add(parentNewRef, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(value));
            }
        }


        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, VerilogParsedDocument, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string candidateWord)
        {
            candidateWord = "";
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out candidateWord);
        }

        public override string CasheId
        {
            get
            {
                if (ParsedDocument == null) return "";

                //byte[] data = Encoding.UTF8.GetBytes(AbsolutePath);
                //byte[] hashBytes = XxHash64.Hash(data);
                //string hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                string hex = ParsedDocument.Key.Replace(@"\", "_").Replace("/", "_").Replace(":", "_").Replace(".", "_") + ".json";
                return hex;
            }
        }
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
                System.Diagnostics.Debug.Print("start json " + path);
                var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);
                using (var writer = new StreamWriter(path))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    serializer.Serialize(jsonWriter, casheObject);
                }
                System.Diagnostics.Debug.Print("complete json " + path);
            }
            catch (Exception exception)
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

