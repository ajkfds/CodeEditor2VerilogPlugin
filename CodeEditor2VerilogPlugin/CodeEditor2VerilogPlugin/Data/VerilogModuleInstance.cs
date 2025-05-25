using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.Data.VerilogCommon;
using pluginVerilog.Verilog.BuildingBlocks;

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
            Verilog.ModuleItems.ModuleInstantiation moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {
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

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                vFile.RegisterModuleInstance(fileItem);

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }
            return fileItem;
        }
        static VerilogModuleInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }
        public bool SystemVerilog { get; set; } = false;
        public string InstanceId
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ModuleName);
                sb.Append(":");
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

        public override string ID
        {
            get
            {
                if (InstanceId == "")
                {
                    return RelativePath + ":" + ModuleName;
                }
                else
                {
                    return RelativePath + ":" + ModuleName + ":" + InstanceId;
                }
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

            File? file = ivFile as File;
            if (file == null) return false;

            if (!IsSameAs(file)) return false;
            if (Project != project) return false;
            if (ModuleName != moduleInstantiation.SourceName) return false;

            if (InstanceId != moduleInstantiation.OverrideParameterID) return false;

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
                    parsedDocument = source.GetInstancedParsedDocument(InstanceId);
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
                    source.RegisterInstanceParsedDocument(InstanceId, newParsedDocument, this);
                    acceptParameterizedParsedDocument(newParsedDocument);
                }
                if (oldParsedDocument != null) oldParsedDocument.Dispose();
            }

            if(source.ParsedDocument != null && source.ParsedDocument.Version != newParsedDocument.Version)
            {
                Verilog.ParsedDocument vParsedDocument = (Verilog.ParsedDocument)newParsedDocument;
                if(vParsedDocument.Root != null && vParsedDocument.Root.BuldingBlocks.Count == 1)
                {
                    source.AcceptParsedDocument(newParsedDocument);
                }
                else
                {
                    source.ReparseRequested = true;
                }
            }

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

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode)
        {
            return new Parser.VerilogParser(this, ModuleName, ParameterOverrides, parseMode);
//            return new Parser.VerilogParser(this.SourceVerilogFile , ModuleName, ParameterOverrides, parseMode);
        }


        // update sub-items from ParsedDocument
        public override void Update()
        {
            VerilogCommon.Updater.Update(this);
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

        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string candidateWord)
        {
            candidateWord = "";
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out candidateWord);
        }


    }
}

