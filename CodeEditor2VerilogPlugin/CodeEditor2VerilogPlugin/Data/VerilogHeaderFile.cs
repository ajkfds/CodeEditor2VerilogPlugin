using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{

    public class VerilogHeaderFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile
    {
        public static new async Task<VerilogHeaderFile> CreateAsync(string relativePath, CodeEditor2.Data.Project project)
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
            //            fileItem.readFromFile();
            await fileItem.FileCheck();

            //            return System.Threading.Tasks.Task.FromResult(fileItem);
            return fileItem;
        }


        private string id = "";
        public override string ID
        {
            get
            {
                return id;
            }
        }

        Dictionary<string, WeakReference<InstanceTextFile>> instanceDictionary = new Dictionary<string, WeakReference<InstanceTextFile>>();
        public void RegisterInstanceFile(InstanceTextFile instanceTextFile)
        {
            textFileLock.EnterWriteLock();
            try
            {
                if (instanceDictionary.TryGetValue(instanceTextFile.ID, out WeakReference<InstanceTextFile>? wref))
                {
                    instanceDictionary.Remove(instanceTextFile.ID);
                }
                instanceDictionary.Add(instanceTextFile.ID, new WeakReference<InstanceTextFile>(instanceTextFile));
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }
        }

        public bool TryGetInstanceTextFile(string ID, out InstanceTextFile? instanceTextFile)
        {
            textFileLock.EnterReadLock();
            try
            {
                instanceTextFile = null;
                if (instanceDictionary.TryGetValue(ID, out WeakReference<InstanceTextFile>? wref))
                {
                    if (!wref.TryGetTarget(out instanceTextFile)) return false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                textFileLock.ExitReadLock();
            }
        }


        static VerilogHeaderFile()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }


        public override bool ReparseRequested
        {
            get
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return true;
                return vParsedDocument.ReparseRequested;
            }
            set
            {
                Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return;
                vParsedDocument.ReparseRequested = value;
            }
        }
        public void CheckDirty()
        {
            pluginVerilog.Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = CodeDocument;
            if (codeDocument == null) return;
            if (codeDocument.Version != vParsedDocument.Version)
            {
                ReparseRequested = true;
            }
        }
        protected override void CreateCodeDocument()
        {
            CodeDocument = new pluginVerilog.CodeEditor.CodeDocument(this);
        }
        public bool SystemVerilog { get { return false; } }

        // update sub-items from ParsedDocument
        private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
        public override async System.Threading.Tasks.Task UpdateAsync()
        {
            await base.UpdateAsync();
            await VerilogCommon.Updater.UpdateAsync(this, _updateSemaphore);
        }
        // read text document from file
        private bool readFromFile()
        {
            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(Project.GetAbsolutePath(RelativePath)))
                {
                    CodeEditor.CodeDocument doc = new CodeEditor.CodeDocument(this);
                    string text = sr.ReadToEnd();
                    doc.Replace(0, 0, 0, text);
                    doc.ClearHistory();
                    doc._tag = "readFormFile";
                    doc.Clean();
                    CodeDocument = doc;
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
                    throw;
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

        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument;
            }
        }

        public override async System.Threading.Tasks.Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            Data.IVerilogRelatedFile? parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return;

            await parentFile.AcceptParsedDocumentAsync(newParsedDocument);

            await UpdateAsync();
        }

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new NavigatePanel.VerilogHeaderNode(this);
        }

        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            Data.IVerilogRelatedFile? parentFile = Parent as Data.IVerilogRelatedFile;
            if (parentFile == null) return null;
            // do not parse again for background parse. header file is parsed with parent file.
            if (parseMode != DocumentParser.ParseModeEnum.EditParse) return null;

            // Use Parent File Parser for Edit Parse
            return parentFile.CreateDocumentParser(parseMode, token);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

    }
}
