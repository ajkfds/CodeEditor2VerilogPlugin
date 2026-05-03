using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class VerilogModuleInstance : InstanceTextFile, IVerilogRelatedFile
    {
        // locked properties --------------------------------------------------

        private string _moduleName = "";
        public required string ModuleName
        {
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _moduleName = value;
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
                    return _moduleName;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        private Dictionary<string, Verilog.Expressions.Expression> _parameterOverrides = new Dictionary<string, Verilog.Expressions.Expression>();
        public required Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides
        {
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _parameterOverrides = value;
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
                    return _parameterOverrides;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        // ModuleInstaceでparsedDocumentを保持しないとSourceFile側で保持するのはWeakReferenceだけなので消えてしまう。
        private CodeEditor2.CodeEditor.ParsedDocument? _parsedDocument;
        public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                Data.VerilogFile source = SourceVerilogFile;
                ParsedDocument? parsedDocument = source.GetInstancedParsedDocument(_getKey());

                // 取得するたびに保持しているparsedDocumentを最新のものに置き換える。
                // 不要なparsedDocumentのGC回収を促進するため。
                _parsedDocument = parsedDocument;
                return parsedDocument;
            }
            set
            {
                // ModuleInstanceで保持する。instanceが消えないようにするために。
                _parsedDocument = value;
                // Sourceへの登録はAcceptParseDocument側で行う
            }
        }

        public override string Key
        {
            get
            {
                return _getKey();
            }
        }
        /// <summary>
        /// Atomically generates the key from module name and parameter overrides while holding the read lock.
        /// This ensures that the key is consistent - both values are read in a single atomic operation.
        /// </summary>
        private string _getKey()
        {
            textFileLock.EnterReadLock();
            try
            {
                // Read both values atomically within the same lock acquisition
                return Verilog.ParsedDocument.KeyGenerator(this, _moduleName, _parameterOverrides);
            }
            finally
            {
                textFileLock.ExitReadLock();
            }
        }

        // ----------------------------------------------



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
            if (moduleInstantiation.Project != project)
            {
                fileItem.ExternalProject = true;
            }

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                //vFile.RegisterModuleInstance(fileItem);

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }

            fileItem.weakInstantiating = new WeakReference<ModuleInstantiation>(moduleInstantiation);
            return fileItem;
        }
        static VerilogModuleInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }



        protected WeakReference<Verilog.ModuleItems.ModuleInstantiation> weakInstantiating;
        public Verilog.ModuleItems.ModuleInstantiation? GetParentInstanciatiation()
        {
            if (weakInstantiating == null) return null;
            weakInstantiating.TryGetTarget(out var moduleInstantiation);
            return moduleInstantiation;
        }

        public Module? Module
        {
            get
            {
                Verilog.ParsedDocument? vpd = VerilogParsedDocument; // no lock is needed. VerilogParsedDocument property is thread safe
                if (vpd?.Root == null) return null;
                
                // Atomically read module name while holding read lock
                textFileLock.EnterReadLock();
                string moduleName;
                try
                {
                    moduleName = _moduleName;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
                
                if (vpd.Root.BuildingBlocks.TryGetValue(moduleName, out var block))
                {
                    return block as Module;
                }
                return null;
            }
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
        public override void CheckDirty()
        {
            ParsedDocument? vParsedDocument = VerilogParsedDocument;
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

        public bool ReplaceBy(
            Verilog.ModuleItems.ModuleInstantiation moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {

            System.Diagnostics.Debug.Print("### VerilogModuleInstance ReplaceBy " + ID);
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();

            Data.IVerilogRelatedFile? ivFile = projectProperty.GetFileOfBuildingBlock(moduleInstantiation.SourceName);
            if (ivFile == null) return false;

            CodeEditor2.Data.File? file;
            file = ivFile as CodeEditor2.Data.File;

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
            }
            weakInstantiating = new WeakReference<ModuleInstantiation>(moduleInstantiation);

            return true;
        }

        public override Task<CodeEditor2.CodeEditor.CodeDocument> GetCodeDocumentAsync()
        {
            return SourceVerilogFile.GetCodeDocumentAsync();
        }

        public override CodeEditor2.CodeEditor.CodeDocument? CodeDocument
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
            Verilog.ParsedDocument? vParsedDoc = null;
            CodeEditor2.CodeEditor.ParsedDocument? parsedDoc = null;
            vParsedDoc = VerilogParsedDocument;
            parsedDoc = ParsedDocument;

            if (vParsedDoc != null)
            {
                // Atomically read parameter overrides while holding read lock
                Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
                textFileLock.EnterReadLock();
                try
                {
                    parameterOverrides = _parameterOverrides;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }

                if (parsedDoc != null && parameterOverrides.Count != 0)
                {
                    foreach (var incFile in vParsedDoc.IncludeFiles.Values)
                    {
                        incFile.Dispose();
                    }
                }
            }

            ParsedDocument = null;
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
            Verilog.ParsedDocument? vParsedDoc = VerilogParsedDocument;
            if (vParsedDoc != null) vParsedDoc.ReloadIncludeFiles();
            SourceVerilogFile.Close();
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


        public override Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument; // no lock is needed. ParsedDocument property is thread safe
            }
        }

        public override async Task AcceptParsedDocumentAsync(ParsedDocument newParsedDocument)
        {

            if (newParsedDocument == null) throw new Exception();
            Data.VerilogFile source = SourceVerilogFile;
            if (source == null) return;

            string key = _getKey();

            ParsedDocument? oldParsedDocument;
            oldParsedDocument = ParsedDocument;
            ParsedDocument = newParsedDocument; // should keep parseddocument 1st

            {
                source.RegisterInstanceParsedDocument(key, newParsedDocument, this);
                await acceptParameterizedParsedDocumentAsync(newParsedDocument);
            }

            if (source.ParsedDocument != null)// && source.ParsedDocument.Version != newParsedDocument.Version)
            {
                Verilog.ParsedDocument vParsedDocument = (Verilog.ParsedDocument)newParsedDocument;
                Verilog.ParsedDocument sourceParsedDocument = (Verilog.ParsedDocument)source.ParsedDocument;
                if (sourceParsedDocument.Root != null && sourceParsedDocument.Root.BuildingBlocks.Count == 1)
                {
                    await source.AcceptParsedDocumentAsync(newParsedDocument);
                }
                else
                {
                    source.ReparseRequested = true;
                }
            }
            {
                Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;

                if (vParsedDocument != null)
                {
                    ReparseRequested = vParsedDocument.ReparseRequested;
                }
            }

            NavigatePanelNode.UpdateVisual();
        }


        private async Task acceptParameterizedParsedDocumentAsync(ParsedDocument newParsedDocument)
        {

            // copy include files

            ParsedDocument = newParsedDocument;

            Verilog.ParsedDocument? vParsedDoc = VerilogParsedDocument;

            if (vParsedDoc == null)
            {
                await UpdateAsync();
                return;
            }

            Verilog.ParsedDocument? vParsedDocument = newParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument != null)
            {
                ReparseRequested = vParsedDocument.ReparseRequested;
            }

            await VerilogFile.updateIncludeFilesAsync(vParsedDoc, Items);

            await UpdateAsync(); // eliminated here
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


        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            CheckDirty();
            
            // Atomically read both module name and parameter overrides while holding read lock
            string moduleName;
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
            
            textFileLock.EnterReadLock();
            try
            {
                moduleName = _moduleName;
                parameterOverrides = _parameterOverrides;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            return new Parser.VerilogParser(this, moduleName, parameterOverrides, parseMode, token);
        }


        // update sub-items from ParsedDocument
        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();

            CodeEditor2.CodeEditor.ParsedDocument? parsedDoc;
            parsedDoc = ParsedDocument;

            await Dispatcher.UIThread.InvokeAsync(
                async () =>
                {
                    await VerilogCommon.Updater.UpdateAsync(this, itemUpdateSemaphore);
                    NavigatePanelNode.UpdateVisual();
                    if (CodeEditor2.Controller.NavigatePanel.GetSelectedFile() == this)
                    {
                        CodeEditor2.Controller.CodeEditor.PostRefresh();
                        if (parsedDoc != null) CodeEditor2.Controller.MessageView.Update(parsedDoc);
                    }
                });
        }


        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            NavigatePanel.VerilogModuleInstanceNode node = new NavigatePanel.VerilogModuleInstanceNode(this);
            return node;
        }




        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            Verilog.ParsedDocument? vParsedDoc = VerilogParsedDocument;

            if (vParsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, vParsedDoc, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            return VerilogCommon.AutoComplete.GetToolItems(this, index);
        }

        public override List<AutocompleteItem>? GetAutoCompleteItems(int index, out string candidateWord)
        {
            candidateWord = "";
            Verilog.ParsedDocument? vParsedDoc = VerilogParsedDocument;

            if (vParsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, vParsedDoc, index, out candidateWord);
        }


    }
}

