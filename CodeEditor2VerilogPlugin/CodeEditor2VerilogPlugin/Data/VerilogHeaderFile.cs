using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.Verilog;
using static CodeEditor2.Controller;

namespace pluginVerilog.Data
{

    public class VerilogHeaderFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {
        public new static VerilogHeaderFile Create(string relativePath, CodeEditor2.Data.Project project)
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

            VerilogHeaderFile fileItem = new VerilogHeaderFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };
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
                if (value == null) throw new Exception();
                CodeEditor.CodeDocument? codeDocument = value as CodeEditor.CodeDocument;
                if (codeDocument == null) throw new Exception();
                document = codeDocument;
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

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new NavigatePanel.VerilogHeaderNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode)
        {
            Data.IVerilogRelatedFile parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse ) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

    }
}
