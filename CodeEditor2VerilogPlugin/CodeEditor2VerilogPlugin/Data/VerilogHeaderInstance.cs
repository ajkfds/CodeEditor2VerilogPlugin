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
        static VerilogHeaderInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
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

        public bool SystemVerilog { get { return RootFile.SystemVerilog; } }

        public bool ReplaceBy(
            VerilogHeaderInstance file
            )
        {
            if (file == null) return false;
            if (!IsSameAs(file as File)) return false;

            ParsedDocument = file.ParsedDocument;
            if(CodeDocument != null && file.CodeDocument != null)
            {
                if(CodeDocument.Version == file.CodeDocument.Version)
                {
                    CodeDocument.CopyColorMarkFrom(file.CodeDocument);
                }
            }

            return true;
        }


        private ulong cashedVersion = ulong.MaxValue;
        CodeDocument? cashedDocument = null;
        public override CodeEditor2.CodeEditor.CodeDocument? CodeDocument
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

        private Data.VerilogHeaderFile? SourceVerilogFile
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

        protected Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>> nodeRefDictionary
            = new Dictionary<WeakReference<CodeEditor2.Data.Item?>, WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>>();
        public override CodeEditor2.NavigatePanel.NavigatePanelNode NavigatePanelNode
        {
            get
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

                if (indexRef != null)
                {
                    nodeRefDictionary.Remove(indexRef);
                }
                WeakReference<CodeEditor2.Data.Item?> parentNewRef = new WeakReference<CodeEditor2.Data.Item?>(Parent);
                nodeRefDictionary.Add(parentNewRef, new WeakReference<CodeEditor2.NavigatePanel.NavigatePanelNode>(value));
            }
        }

        public override void Save()
        {
            if(SourceTextFile == null) return;
            SourceTextFile.Save();
        }

        public override DateTime? LoadedFileLastWriteTime
        {
            get
            {
                if(SourceTextFile == null) return null;
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




        [Newtonsoft.Json.JsonIgnore]
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
            if(Parent == null)
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
            return VerilogCommon.AutoComplete.GetPopupItem(this, VerilogParsedDocument, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string cantidateWord)
        {
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index, out cantidateWord);
        }



    }
}
