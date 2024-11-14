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
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.ModuleItems;
using static CodeEditor2.Controller;

namespace pluginVerilog.Data
{
    public class VerilogHeaderInstance : InstanceTextFile, IVerilogRelatedFile
    {
        protected VerilogHeaderInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }

        
        public static VerilogHeaderInstance Create(
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

            instance.id = id;
            instance.RootFile = parentFile;
            instance.InstancedReference = instancedReference;

            return instance;
        }
        public IVerilogRelatedFile RootFile { get; protected set; }
        public IndexReference InstancedReference { get; protected set; }

        private string id;
        public override string ID
        {
            get
            {
                return id;
            }
        }


        public bool ReplaceBy(
            VerilogHeaderInstance file
            )
        {
            if (file == null) return false;
            if (!IsSameAs(file as File)) return false;

            ParsedDocument = file.ParsedDocument;

            return true;
        }


        private ulong cashedVersion = ulong.MaxValue;
        CodeDocument cashedDocument = null;
        public override CodeEditor2.CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                if (SourceVerilogFile == null) return null;
                return SourceVerilogFile.CodeDocument;
            }
        }

        private void disposeItems()
        {
            if (ParsedDocument != null)// && ParameterOverrides.Count != 0)
            {
                foreach (var incFile in VerilogParsedDocument.IncludeFiles.Values)
                {
                    incFile.Dispose();
                }
            }
            parsedDocument = null;
        }

        public string ModuleName { set; get; }


        public string ParameterId
        {
            get
            {
                return "";
            }
        }

        private Data.VerilogHeaderFile SourceVerilogFile
        {
            get
            {
                return SourceTextFile as VerilogHeaderFile;
            }
        }



        public override void Close()
        {
            if (VerilogParsedDocument != null) VerilogParsedDocument.ReloadIncludeFiles();
            //SourceVerilogFile.Close();
        }

        private Verilog.ParsedDocument parsedDocument = null;

        public override CodeEditor2.CodeEditor.ParsedDocument ParsedDocument
        {
            get
            {
                return parsedDocument;
            }
            set
            {
                Verilog.ParsedDocument? vParsedDocument = value as Verilog.ParsedDocument;
                if (vParsedDocument == null) throw new Exception();
                parsedDocument = vParsedDocument;
            }
        }
        public override void Save()
        {
            if (CodeDocument == null) return;

            SourceTextFile.Save();
        }

        public override DateTime? LoadedFileLastWriteTime
        {
            get
            {
                return SourceTextFile.LoadedFileLastWriteTime;
            }
        }

        public Verilog.ParsedDocument VerilogParsedDocument
        {
            get
            {
                return parsedDocument;
            }
        }

        public override void AcceptParsedDocument(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
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
            Update();
        }




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
            nodeRef = new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(node);
            return node;
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode)
        {
            Data.IVerilogRelatedFile parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode);
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

        //public override List<CodeEditor2.CodeEditor.PopupItem> GetPopupItems(ulong version, int index)
        //{
        //    return VerilogCommon.AutoComplete.GetPopupItems(this,VerilogParsedDocument, version, index);
        //}
        public override PopupItem GetPopupItem(ulong version, int index)
        {
            return VerilogCommon.AutoComplete.GetPopupItem(this, VerilogParsedDocument, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override List<AutocompleteItem> GetAutoCompleteItems(int index, out string cantidateWord)
        {
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out cantidateWord);
        }



    }
}
