using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class VerilogModuleInstance : InstanceTextFile, IVerilogRelatedFile
    {
        // locked properties --------------------------------------------------



        public string NameSpaceString { set; get; } = "";

        // ModuleInstaceでparsedDocumentを保持しないとSourceFile側で保持するのはWeakReferenceだけなので消えてしまう。

        // ----------------------------------------------



        protected VerilogModuleInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }
        
        /// <summary>
        /// Create a VerilogModuleInstance from a ModuleInstantiation
        /// </summary>
        /// <param name="moduleInstantiation">The module instantiation</param>
        /// <param name="instanceIndex">Instance index for array instances (0 for non-array)</param>
        /// <param name="isArrayInstance">True if this is part of an instance array</param>
        /// <returns>The created VerilogModuleInstance</returns>
        public static VerilogModuleInstance? Create(
            Verilog.Items.ModuleInstantiation moduleInstantiation,
            int instanceIndex = 0,
            bool isArrayInstance = false
            )
        {
            CodeEditor2.Data.Project project = moduleInstantiation.GetInstancedBuildingBlockProject();
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            Data.IVerilogRelatedFile? file = projectProperty.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName);
            if (file == null) return null;

            CodeEditor2.Data.TextFile? textFile = file as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();

            // Generate instance name with array index if part of array
            string instanceName = moduleInstantiation.Name;
            if (isArrayInstance)
            {
                instanceName = $"{moduleInstantiation.Name}[{instanceIndex}]";
            }

            VerilogModuleInstance fileItem = new VerilogModuleInstance(textFile)
            {
                InstanceName = moduleInstantiation.SourceName,
                ParameterOverrides = moduleInstantiation.ParameterOverrides,
                Project = project,
                RelativePath = file.RelativePath,
                Name = instanceName,
                InstanceIndex = instanceIndex,
                IsArrayInstance = isArrayInstance
            };
            if (moduleInstantiation.Project != project)
            {
                fileItem.ExternalProject = true;
            }

            if (moduleInstantiation.InstancedNameSpaceNames.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach( var i in moduleInstantiation.InstancedNameSpaceNames)
                {
                    sb.Append(i);
                    if (moduleInstantiation.InstancedNameSpaceNames.Last() != i)
                    {
                        sb.Append(",");
                    }
                }
                fileItem.NameSpaceString = sb.ToString();
            }

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                //vFile.RegisterModuleInstance(fileItem);

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }

            fileItem.weakInstantiating = new WeakReference<Verilog.Items.ModuleInstantiation>(moduleInstantiation);
            return fileItem;
        }

        /// <summary>
        /// Create multiple VerilogModuleInstance objects for an instance array
        /// </summary>
        /// <param name="moduleInstantiation">The module instantiation with array range</param>
        /// <returns>List of created VerilogModuleInstances</returns>
        public static List<VerilogModuleInstance>? CreateArray(
            Verilog.Items.ModuleInstantiation moduleInstantiation
            )
        {
            int instanceCount = moduleInstantiation.InstanceCount;
            List<VerilogModuleInstance> instances = new List<VerilogModuleInstance>();

            for (int i = 0; i < instanceCount; i++)
            {
                VerilogModuleInstance? instance = Create(moduleInstantiation, i, true);
                if (instance == null) return null;
                instances.Add(instance);
            }

            return instances;
        }
        static VerilogModuleInstance()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
        }



        protected WeakReference<Verilog.Items.ModuleInstantiation> weakInstantiating;
        public Verilog.Items.ModuleInstantiation? GetParentInstanciatiation()
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
                    moduleName = _instanceName;
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

        public override string ID
        {
            get
            {
                return Key;
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
        }



        public override void Close()
        {
            Verilog.ParsedDocument? vParsedDoc = VerilogParsedDocument;
            if (vParsedDoc != null) vParsedDoc.ReloadIncludeFiles();
            SourceVerilogFile?.Close();
        }


        public override async System.Threading.Tasks.Task SaveAsync()
        {
            if (CodeDocument == null) return;

            // Save through the source file and refresh this instance's navigate panel
            // node (the source file's SaveAsync only refreshes its own node, which
            // left this node's dirty badge stale after Save).
            await SaveSourceTextFileAsync();
        }



        public override async Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.Parser.DocumentParser parser)
        {
            if (Plugin.StopParse) return;

            Verilog.ParsedDocument? newParsedDocument = parser.ParsedDocument as Verilog.ParsedDocument;
            if (newParsedDocument == null) return;

            Data.VerilogFile? source = SourceVerilogFile;
            if (source == null) return;

            string key = _getKey();

            Verilog.ParsedDocument? oldParsedDocument = VerilogParsedDocument;

            source.RegisterInstanceParsedDocument(key, newParsedDocument, this);
            source.RegisterInstanceFile(this);

            if (source.ParsedDocument != null)// && source.ParsedDocument.Version != newParsedDocument.Version)
            {
                Verilog.ParsedDocument vParsedDocument = (Verilog.ParsedDocument)newParsedDocument;
                Verilog.ParsedDocument sourceParsedDocument = (Verilog.ParsedDocument)source.ParsedDocument;
                if (sourceParsedDocument.Root != null && sourceParsedDocument.Root.BuildingBlocks.Count == 1)
                {
                    await source.AcceptParsedDocumentAsync(parser);
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



            _parsedDocument = newParsedDocument;

            Dispatcher.UIThread.Post(() =>
            {
                _ = postUpdateAsync(newParsedDocument);
            });
        }

        private async Task postUpdateAsync(Verilog.ParsedDocument newParsedDocument)
        {
            try
            {
                TextFile? textFile = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
                if (textFile == this)
                {
                    if(CodeDocument!=null) textFile.CodeDocument?.CopyColorMarkFrom(newParsedDocument.CodeDocument);
                    if(ParsedDocument != null) CodeEditor2.Controller.MessageView.Update(newParsedDocument);
                    CodeEditor2.Controller.CodeEditor.PostRefresh();
                }

                await UpdateAsync(); // update navigate panel node and message view

                // refresh the navigate panel node icon (error/warning badge, ...)
                // after the parsed document is updated. Without this, the icon of a
                // deeply parsed VerilogModuleInstance stays stale until the node is
                // selected (same as VerilogHeaderInstance.AcceptParsedDocumentAsync).
                // Post (not Invoke) to avoid blocking this background parse thread
                // on the UI thread.
                NavigatePanelNode.UpdateVisual();
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog(ex);
            }
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
            
            // Atomically read both module name and parameter overrides while holding read lock
            string moduleName;
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides;
            
            textFileLock.EnterReadLock();
            try
            {
                moduleName = _instanceName;
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
            await VerilogCommon.Updater.UpdateAsync(this, itemUpdateSemaphore);
            await base.UpdateAsync();
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

        public override CodeEditor2.CodeEditor.CodeComplete.CompletionContext? GetAutoCompleteItems(int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index);
        }

    }
}

