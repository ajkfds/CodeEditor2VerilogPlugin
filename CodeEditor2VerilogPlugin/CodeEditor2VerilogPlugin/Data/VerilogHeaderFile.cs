using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using pluginVerilog.Verilog;
using static CodeEditor2.Controller;

namespace pluginVerilog.Data
{

    public class VerilogHeaderFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {
        public new static VerilogHeaderFile Create(string relativePath, CodeEditor2.Data.Project project)
        {
            VerilogHeaderFile fileItem = new VerilogHeaderFile();
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

        private string id = null;
        public override string ID
        {
            get
            {
                return id;
            }
        }
        public override CodeEditor2.CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                if (document != null && document as CodeEditor.CodeDocument == null) System.Diagnostics.Debugger.Break();
                if (document == null)
                {
                    try
                    {
                        while (!readFromFile())
                        {
                            System.Threading.Thread.Sleep(10);
                        }
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
                if (value != null && value as CodeEditor.CodeDocument == null) System.Diagnostics.Debugger.Break();
                document = value as CodeEditor.CodeDocument;
            }
        }

        // update sub-items from ParsedDocument
        public override void Update()
        {
            VerilogCommon.Updater.Update(this);
        }

        private bool readFromFile()
        {
            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(Project.GetAbsolutePath(RelativePath)))
                {
                    document = new CodeEditor.CodeDocument(this);
                    string text = sr.ReadToEnd();
                    document.Replace(0, 0, 0, text);
                    document.ClearHistory();
                    document._tag = "readFormFile";
                    document.Clean();
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147024864) // used by another process
                {
                    return false;
                }
                else
                {
                    throw ex;
                }
            }
        }

        public override void Dispose()
        {
            if (ParsedDocument != null) ParsedDocument.Dispose();
            base.Dispose();
        }

        public ProjectProperty ProjectProperty
        {
            get
            {
                return Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            }
        }


        public override CodeEditor2.CodeEditor.CodeDrawStyle DrawStyle
        {
            get
            {
                return Global.CodeDrawStyle;
            }
        }

        public Verilog.ParsedDocument VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
            }
        }

        public override void AcceptParsedDocument(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            Data.IVerilogRelatedFile parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return;

            parentFile.AcceptParsedDocument(newParsedDocument);

            Update();
        }

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode createNode()
        {
            return new NavigatePanel.VerilogHeaderNode(this);
        }

        public override CodeEditor2.CodeEditor.DocumentParser CreateDocumentParser(CodeEditor2.CodeEditor.DocumentParser.ParseModeEnum parseMode)
        {
            Data.IVerilogRelatedFile parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse ) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode);
        }

        public override List<CodeEditor2.CodeEditor.ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        //public new void AfterKeyPressed(KeyPressEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //public new void AfterKeyDown(KeyEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //public new void BeforeKeyPressed(KeyPressEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //public new void BeforeKeyDown(KeyEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //public override List<CodeEditor2.CodeEditor.PopupItem> GetPopupItems(ulong version, int index)
        //{
        //    return null;
        //}

        //public new List<AutocompleteItem> GetAutoCompleteItems(int index, out string cantidateText)
        //{
        //    throw new NotImplementedException();
        //}

        //public new List<ToolItem> GetToolItems(int index)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
