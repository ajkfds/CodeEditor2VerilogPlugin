using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VerilogDocument = pluginVerilog.Verilog.ParsedDocument;

namespace pluginVerilog.Data
{
    /// <summary>
    /// Represents an imported SystemVerilog package as a sub-item of a VerilogFile/VerilogModuleInstance/InterfaceInstance.
    /// Mirrors <see cref="VerilogModuleInstance"/> but holds a reference to a <see cref="BuildingBlocks.Package"/>
    /// (resolved via <c>PackageNameSpace</c>) instead of a Module/Interface.
    /// </summary>
    public class ImportedPackage : InstanceTextFile, IVerilogRelatedFile
    {
        // locked properties --------------------------------------------------

        private string _packageName = "";
        public required string PackageName
        {
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _packageName = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _packageName;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        public string NameSpaceString { set; get; } = "";

        // ImportedPackageでparsedDocumentを保持しないとSourceFile側で保持するのはWeakReferenceだけなので消えてしまう。
        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                Data.VerilogFile? vFile = SourceVerilogFile;
                if (SourceVerilogFile == null) return null;
                return SourceVerilogFile.ParsedDocument;
            }
            set
            {
                Data.VerilogFile? vFile = SourceVerilogFile;
                if (SourceVerilogFile == null) return;
                SourceVerilogFile.ParsedDocument = value;
            }
        }

        // ----------------------------------------------

        protected ImportedPackage(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }

        /// <summary>
        /// Create a ImportedPackage for the given package name. The package must be
        /// registered in <c>ProjectProperty.PackageNameSpace</c> at the time of this call.
        /// Returns null if no corresponding file can be resolved.
        /// </summary>
        public static ImportedPackage? Create(
            string packageName,
            CodeEditor2.Data.Project project
            )
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();

            Data.IVerilogRelatedFile? file = projectProperty.PackageNameSpace.GetFile(packageName);
            if (file == null) return null;

            CodeEditor2.Data.TextFile? textFile = file as CodeEditor2.Data.TextFile;
            if (textFile == null) return null;

            ImportedPackage fileItem = new ImportedPackage(textFile)
            {
                PackageName = packageName,
                Project = project,
                RelativePath = file.RelativePath,
                Name = packageName,
            };

            if (file is Data.VerilogFile vFile && vFile.SystemVerilog)
            {
                fileItem.SystemVerilog = true;
            }

            return fileItem;
        }

        static ImportedPackage()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }

        public Package? Package
        {
            get
            {
                VerilogDocument? vpd = VerilogParsedDocument;
                if (vpd?.Root == null) return null;

                string packageName;
                textFileLock.EnterReadLock();
                try
                {
                    packageName = _packageName;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }

                if (vpd.Root.BuildingBlocks.TryGetValue(packageName, out var block))
                {
                    return block as Package;
                }
                return null;
            }
        }

        public override bool ReparseRequested
        {
            get
            {
                VerilogDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return true;
                return vParsedDocument.ReparseRequested;
            }
            set
            {
                VerilogDocument? vParsedDocument = VerilogParsedDocument;
                if (vParsedDocument == null) return;
                vParsedDocument.ReparseRequested = value;
            }
        }

        public override void CheckDirty()
        {
            VerilogDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = CodeDocument;
            if (codeDocument == null) return;
            if (codeDocument.Version != vParsedDocument.Version)
            {
                ReparseRequested = true;
            }
        }

        public override string ID
        {
            get
            {
                return Key;
            }
        }

        public bool ReplaceBy(ImportedPackage other)
        {
            if (other == null) return false;

            CodeEditor2.Data.TextFile? otherFile = other.SourceTextFile;
            if (otherFile == null) return false;
            if (SourceTextFile == null) return false;
            if (!SourceTextFile.IsSameAs(otherFile)) return false;
            if (Project != other.Project) return false;
            if (PackageName != other.PackageName) return false;

            // Re-use source file reference. ParsedDocument is owned by the source.
            return true;
        }

        public override Task<CodeEditor2.CodeEditor.CodeDocument> GetCodeDocumentAsync()
        {
            Data.VerilogFile? vFile = SourceVerilogFile;
            if (vFile == null) throw new Exception();
            return vFile.GetCodeDocumentAsync();
        }

        public override CodeEditor2.CodeEditor.CodeDocument? CodeDocument
        {
            get
            {
                Data.VerilogFile? vFile = SourceVerilogFile;
                if (vFile == null) return null;
                return vFile.CodeDocument;
            }
        }

        public override void Dispose()
        {
            disposeItems();
        }

        private void disposeItems()
        {
            // ParsedDocument is owned by the source file; nothing to do here.
        }

        public Data.VerilogFile? SourceVerilogFile
        {
            get
            {
                return SourceTextFile as Data.VerilogFile;
            }
        }


        public override void Close()
        {
            VerilogDocument? vParsedDoc = VerilogParsedDocument;
            if (vParsedDoc != null) vParsedDoc.ReloadIncludeFiles();
            Data.VerilogFile? source = SourceVerilogFile;
            if (source != null) source.Close();
        }


        public override async System.Threading.Tasks.Task SaveAsync()
        {
            CodeEditor2.CodeEditor.CodeDocument? codeDoc;
            CodeEditor2.Data.TextFile? sourceTextFile;
            codeDoc = CodeDocument;
            sourceTextFile = SourceTextFile;

            if (codeDoc == null) return;
            if (sourceTextFile == null) return;
            await sourceTextFile.SaveAsync();
        }


        public override VerilogDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as VerilogDocument; // no lock is needed. ParsedDocument property is thread safe
            }
        }

        public override async Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.Parser.DocumentParser parser)
        {
            if(SourceVerilogFile == null) return;
            await SourceVerilogFile.AcceptParsedDocumentAsync(parser);

            await UpdateAsync();
        }



        public override ProjectProperty ProjectProperty
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


        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            CheckDirty();

            string packageName;
            textFileLock.EnterReadLock();
            try
            {
                packageName = _packageName;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            return new Parser.VerilogParser(this, packageName, null, parseMode, token);
        }


        // update sub-items from ParsedDocument
        public override async Task UpdateAsync()
        {
            await VerilogCommon.Updater.UpdateAsync(this, itemUpdateSemaphore);
            await base.UpdateAsync();
        }


        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            NavigatePanel.ImportedPackageNode node = new NavigatePanel.ImportedPackageNode(this);
            return node;
        }




        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            VerilogDocument? vParsedDoc = VerilogParsedDocument;

            if (vParsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, vParsedDoc, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>? GetAutoCompleteItems(int index, out string candidateWord)
        {
            candidateWord = "";
            VerilogDocument? vParsedDoc = VerilogParsedDocument;

            if (vParsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, vParsedDoc, index, out candidateWord);
        }


    }
}
