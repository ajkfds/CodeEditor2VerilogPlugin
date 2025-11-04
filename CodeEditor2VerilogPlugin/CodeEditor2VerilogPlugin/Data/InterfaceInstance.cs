using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class InterfaceInstance : InstanceTextFile, IVerilogRelatedFile
    {
        public required string ModuleName { set; get; }

        public required Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides;

        protected InterfaceInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }
        public static InterfaceInstance? Create(
            Verilog.DataObjects.InterfaceInstance moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            Data.IVerilogRelatedFile? file = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
            if (file == null) return null;

            CodeEditor2.Data.TextFile? textFile = file as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            InterfaceInstance fileItem = new InterfaceInstance(textFile)
            {
                ModuleName = moduleInstantiation.SourceName,
                ParameterOverrides = moduleInstantiation.ParameterOverrides,
                Project = project,
                RelativePath = file.RelativePath,
                Name = moduleInstantiation.Name
            };

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                vFile.RegisterModuleInstance(fileItem);

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }
            return fileItem;
        }
        public bool SystemVerilog { get; set; } = false;

        public override string ID
        {
            get
            {
                if (ParameterId == "")
                {
                    return RelativePath + ":" + ModuleName;
                }
                else
                {
                    return RelativePath + ":" + ModuleName + ":" + ParameterId;
                }
            }
        }

        public bool ReplaceBy(
            Verilog.DataObjects.InterfaceInstance moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();

            Data.IVerilogRelatedFile? ivFile = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
            if (ivFile == null) return false;

            File? file = ivFile as File;
            if (file == null) return false;

            if (!IsSameAs(file)) return false;
            if (Project != project) return false;
            if (ModuleName != moduleInstantiation.SourceName) return false;

            if (ParameterId != moduleInstantiation.OverrideParameterID) return false;

            // re-register
            //disposeItems();

            ParameterOverrides = moduleInstantiation.ParameterOverrides;

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) return false;
                vFile.RegisterModuleInstance(this);
            }

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
            if (VerilogParsedDocument != null)
            {
                if (ParsedDocument != null && ParameterOverrides.Count != 0)
                {
                    foreach (var incFile in VerilogParsedDocument.IncludeFiles.Values)
                    {
                        incFile.Dispose();
                    }
                }
            }

            parsedDocument = null;
            SourceVerilogFile.RemoveModuleInstance(this);
        }

        public string ParameterId
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var kvp in ParameterOverrides)
                {
                    sb.Append(kvp.Key);
                    sb.Append("=");
                    sb.Append(kvp.Value.Value.ToString());
                    sb.Append(",");
                }
                return sb.ToString();
            }
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
                if (parsedDocument == null)
                {
                    if (ParameterOverrides.Count == 0)
                    {
                        Data.VerilogFile file = SourceVerilogFile;
                        if (file == null) return null;
                        parsedDocument = file.ParsedDocument;
                    }
                    else
                    {
                        Data.VerilogFile source = SourceVerilogFile;
                        parsedDocument = source.GetInstancedParsedDocument(ParameterId);
                    }
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
            //            Verilog.ParsedDocument? vParsedDocument = newParsedDocument as Verilog.ParsedDocument;
            //            if (vParsedDocument == null) return;

            //            parsedDocument = vParsedDocument;
            Data.VerilogFile source = SourceVerilogFile;
            if (source == null) return;

            if (ParameterOverrides.Count == 0)
            {
                source.AcceptParsedDocument(newParsedDocument);
            }
            else
            {
                source.RegisterInstanceParsedDocument(ModuleName + ":" + ParameterId, newParsedDocument, this);
                acceptParameterizedParsedDocument(newParsedDocument);
            }
            //            ReparseRequested = vParsedDocument.ReparseRequested;
            //            Update();
            System.Diagnostics.Debug.Print("### Verilog Module Instance Parsed " + ID);
        }

        private void acceptParameterizedParsedDocument(ParsedDocument newParsedDocument)
        {
            ParsedDocument oldParsedDocument = ParsedDocument;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            // copy include files

            ParsedDocument = newParsedDocument;

            if (VerilogParsedDocument == null)
            {
                Update();
                return;
            }

            // Register New Building Block
            //foreach (BuildingBlock buildingBlock in VerilogParsedDocument.Root.BuldingBlocks.Values)
            //{
            //    if (ProjectProperty.HasRegisteredBuildingBlock(buildingBlock.Name))
            //    {   // swap building block
            //        BuildingBlock? module = buildingBlock as Module;
            //        if (module == null) continue;

            //        BuildingBlock? registeredModule = ProjectProperty.GetBuildingBlock(module.Name) as Module;
            //        if (registeredModule == null) continue;
            //        if (registeredModule.File == null) continue;
            //        if (registeredModule.File.RelativePath == module.File.RelativePath) continue;

            //        continue;
            //    }

            //    // register new parsedDocument
            //    ProjectProperty.RegisterBuildingBlock(buildingBlock.Name, buildingBlock, this);
            //}

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument != null)
            {
                ReparseRequested = vParsedDocument.ReparseRequested;
            }

            VerilogFile.updateIncludeFiles(VerilogParsedDocument, Items);

            //Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            //foreach (var item in Items.Values)
            //{
            //    Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
            //    if (vh == null) continue;
            //    headerItems.Add(item.ID, vh);
            //}

            //foreach (var includeFile in VerilogParsedDocument.IncludeFiles.Values)
            //{
            //    if (!headerItems.ContainsKey(includeFile.ID)) continue;
            //    Data.VerilogHeaderInstance item = headerItems[includeFile.ID];
            //    item.CodeDocument.CopyColorMarkFrom(includeFile.VerilogParsedDocument.CodeDocument);
            //}

            Update(); // eliminated here
            System.Diagnostics.Debug.Print("### Verilog File Parsed " + ID);
        }



        [Newtonsoft.Json.JsonIgnore]
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
            NavigatePanel.InterfaceInstanceNode node = new NavigatePanel.InterfaceInstanceNode(this);
            nodeRef = new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node);
            return node;
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            //            return new Parser.VerilogParser(this, ModuleName, ParameterOverrides, parseMode);
            return new Parser.VerilogParser(this.SourceVerilogFile, ModuleName, ParameterOverrides, parseMode, token);
        }


        // update sub-items from ParsedDocument
        public override void Update()
        {
            VerilogCommon.Updater.Update(this);
        }

        public async System.Threading.Tasks.Task UpdateAsync()
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

        public override List<AutocompleteItem> GetAutoCompleteItems(int index, out string candidateWord)
        {
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out candidateWord);
        }


    }
}

