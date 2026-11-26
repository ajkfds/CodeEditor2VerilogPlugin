using Avalonia.Threading;
using CodeEditor2;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.CodeEditor;
using pluginVerilog.Data.VerilogCommon;
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Verilog.BuildingBlocks;
using SystemVerilogCore.Verilog.Statements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class VerilogFile : CodeEditor2.Data.TextFile, IVerilogRelatedFile,
        SystemVerilogCore.Verilog.IVerilogNameSpaceFile,
        // The write side of the instance-parse table. A host that drives a parse
        // itself (SystemVerilogCore.ParseEngine) records the parameter-overridden
        // result through this contract, the same table the editor fills while
        // accepting a parse result.
        SystemVerilogCore.Documents.IVerilogSourceFileInstanceCache
    {
        /// <summary>
        /// Records an instance parse result, as the UI-free cache contract.
        ///
        /// <para>
        /// Forwards to <see cref="RegisterInstanceParsedDocument(string,
        /// SystemVerilogCore.Parsing.IParsedDocument, InstanceTextFile?)"/>; no
        /// instance object is materialised, which is exactly what a UI-less host
        /// wants.
        /// </para>
        /// </summary>
        void SystemVerilogCore.Documents.IVerilogSourceFileInstanceCache.RegisterInstanceParsedDocument(
            string instanceKey, SystemVerilogCore.Parsing.IParsedDocument parsedDocument) =>
            RegisterInstanceParsedDocument(instanceKey, parsedDocument, null);
        /// <summary>
        /// The root-level named element called <paramref name="name"/>, as the
        /// UI-free name-space contract.
        ///
        /// <para>
        /// This is the only thing a
        /// <see cref="SystemVerilogCore.Verilog.RegisterNameSpace{T}"/> needs from
        /// a file. Exposing it as a contract member lets the name space live in
        /// the UI-free assembly while this class keeps its editor base type.
        /// </para>
        /// </summary>
        object? SystemVerilogCore.Verilog.IVerilogNameSpaceFile.FindRootNamedElement(string name)
        {
            Verilog.ParsedDocument? parsed = VerilogParsedDocument;
            if (parsed?.Root == null) return null;
            if (!parsed.Root.NamedElements.ContainsKey(name)) return null;
            return parsed.Root.NamedElements[name];
        }

        /// <summary>
        /// Name of the project this file belongs to, as the name-space contract.
        /// A file reference records it so the host can find the project again.
        /// </summary>
        /// <summary>Unique id of the file, as the name-space contract.</summary>
        string SystemVerilogCore.Verilog.IVerilogNameSpaceFile.Id => ID;

        string SystemVerilogCore.Verilog.IVerilogNameSpaceFile.ProjectName =>
            Project?.Name ?? "";

        /// <summary>Project-relative path, as the name-space contract.</summary>
        string SystemVerilogCore.Verilog.IVerilogNameSpaceFile.FileRelativePath =>
            RelativePath;

        /// <summary>
        /// This file's own parse result, as the name-space contract. Parse sites
        /// resolve a name to the declaring file and then read the declaration back
        /// out of it.
        /// </summary>
        SystemVerilogCore.Parsing.IParsedDocument?
            SystemVerilogCore.Verilog.IVerilogNameSpaceFile.ParsedResult =>
            VerilogParsedDocument;


        public new static async Task<VerilogFile> CreateAsync(string relativePath, CodeEditor2.Data.Project project)
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
            System.Diagnostics.Debug.Print(relativePath);
            CodeEditor2.FileTypes.FileType fileType = CodeEditor2.Global.FileTypes[FileTypes.VerilogFile.TypeID];
            VerilogFile fileItem = new VerilogFile() { Name = name, Project = project, RelativePath = relativePath };
            await fileItem.FileCheckAsync();

            return fileItem;
        }


        ~VerilogFile()
        {
            //if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
        }
        public override string Key
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return Verilog.ParsedDocument.KeyGenerator(this, null, null);
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }
        static VerilogFile()
        {
            CustomizeItemEditorContextMenu += (x => EditorContextMenu.CustomizeEditorContextMenu(x));
            CodeEditor2.Data.Item.PolymorphicResolver.DerivedTypes.Add(new JsonDerivedType(typeof(VerilogFile)));
        }

        public static async Task<VerilogFile> CreateSystemVerilogAsync(string relativePath, CodeEditor2.Data.Project project)
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

            CodeEditor2.FileTypes.FileType fileType = CodeEditor2.Global.FileTypes[FileTypes.SystemVerilogFile.TypeID];
            VerilogFile fileItem = new VerilogFile() { Name = name, Project = project, RelativePath = relativePath };
            fileItem.SystemVerilog = true;

            await fileItem.FileCheckAsync();
            return fileItem;
        }


        /// <summary>
        /// indicate this file is system verilog file.
        /// true : system verilog file
        /// failse : verilog file
        /// </summary>

        private bool _systemVerilog = false;
        public bool SystemVerilog
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _systemVerilog;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _systemVerilog = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        public string FileID
        {
            get
            {
                return RelativePath;
            }
        }

        protected override void CreateCodeDocument()
        {
            CodeDocument = new pluginVerilog.CodeEditor.CodeDocument(this);
        }

        /// <summary>
        /// Accdept new Parsed Document for this Verilog File
        /// </summary>
        /// <param name="newParsedDocument"></param>
        public override async Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.Parser.DocumentParser parser)
        {
            if (Plugin.StopParse) return;

            Verilog.ParsedDocument? newParsedDocument = parser.ParsedDocument as Verilog.ParsedDocument;
            if (newParsedDocument == null) return;

            SystemVerilogCore.Parsing.IParsedDocument? oldParsedDocument = ParsedDocument; // no lock is needed. ParsedDocument property is thread safe

            if (ReferenceEquals(oldParsedDocument, newParsedDocument)) return;

            if (newParsedDocument == null)
            {
                await UpdateAsync();
                return;
            }

            CodeEditor.CodeDocument? codeDoc;
            codeDoc = CodeDocument as pluginVerilog.CodeEditor.CodeDocument; // no lock is needed. CodeDocument property is thread safe

            if (codeDoc == null) return;
            if (newParsedDocument == null) return;

            if (codeDoc.Version != newParsedDocument.Version)
            {
                // if code document is updated during parsing, do not accept parsed document because it may be outdated.
                newParsedDocument.ReparseRequested = true;
                return;
            }
            else
            {
                codeDoc?.CopyColorMarkFrom(parser.Document);
            }

            textFileLock.EnterWriteLock();
            try
            {
                // Register New Building Block
                if (newParsedDocument.Root != null)
                {
                    foreach(INamedElement element in newParsedDocument.Root.NamedElements.Values)
                    {
                        Package? package = element as Package;
                        if(package != null)
                        {
                            ProjectProperty.PackageNameSpace.Register(package.Name, package, this);
                            continue;
                        }

                        Class? @class = element as Class;
                        if(@class != null)
                        {
                            ProjectProperty.UnitNameSpace.Register(@class.Name, @class, this);
                            continue;
                        }

                        // interface class is defined at compilation-unit scope: register to UnitNameSpace (not DefinitionNameSpace)
                        InterfaceClass? interfaceClass = element as InterfaceClass;
                        if(interfaceClass != null)
                        {
                            ProjectProperty.UnitNameSpace.Register(interfaceClass.Name, interfaceClass, this);
                            continue;
                        }

                        BuildingBlock? buildingBlock = element as BuildingBlock;
                        if(buildingBlock != null) {
                            ProjectProperty.DefinitionNameSpace.Register(buildingBlock.Name, buildingBlock, this);
                            continue;
                        }

                        ProjectProperty.UnitNameSpace.Register(element.Name, element, this);
                    }
                }

                _reparseRequested = newParsedDocument.ReparseRequested;

                // swap ParsedDocument
                _parsedDocument = newParsedDocument;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }


            // Now that all building blocks in this file are registered, resolve and apply @scope comment annotations.
            // This must happen after RegisterBuildingBlock so that @scope targets are findable via ProjectProperty.GetBuildingBlock().
            if (newParsedDocument.Root != null)
            {
                newParsedDocument.Root.ApplyCommentScopeReferences();
                foreach (var bbKvp in newParsedDocument.Root.BuildingBlocks)
                {
                    bbKvp.Value.ApplyCommentScopeReferences();
                }
            }


            // Editor-dependent update (color/message/refresh) must run only
            // when the editor UI is alive. In UI-less hosts (LSP server,
            // unit tests) the Avalonia Dispatcher does not exist and
            // GetTextFileAsync would block forever.
            if (!Dispatcher.UIThread.CheckAccess())
            {
                TextFile? currentTextFile = await Controller.CodeEditor.GetTextFileAsync();
                if (currentTextFile == this)
                {
                    currentTextFile.CodeDocument?.CopyColorMarkFrom(parser.Document);
                    CodeEditor2.Controller.MessageView.Update(newParsedDocument);
                    CodeEditor2.Controller.CodeEditor.PostRefresh();
                }
            }

            await updateIncludeFilesAsync(newParsedDocument, Items);
            
            // CodeEditor & NavigateNode & Messgae update will done in UpdateAsync
            await UpdateAsync();
        }

        private void update(Verilog.ParsedDocument? newParsedDocument)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {

            }

        }



        internal static async System.Threading.Tasks.Task updateIncludeFilesAsync(Verilog.ParsedDocument parsedDocument, ItemList items)
        {
            // create id table
            Dictionary<string, Data.VerilogHeaderInstance> headerItems = new Dictionary<string, VerilogHeaderInstance>();
            foreach (var item in items)
            {
                Data.VerilogHeaderInstance? vh = item as Data.VerilogHeaderInstance;
                if (vh == null) continue;
                headerItems.Add(item.ID, vh);
            }

            // get file selected in text editor (skip in UI-less hosts: the
            // dispatcher does not exist and these calls would block forever)
            CodeEditor2.NavigatePanel.NavigatePanelNode? node = null;
            CodeEditor2.Data.Item? currentItem = null;
            CodeEditor2.Data.ITextFile? currentTextFile = null;
            if (!Dispatcher.UIThread.CheckAccess())
            {
                node = await CodeEditor2.Controller.NavigatePanel.GetSelectedNodeAsync();
                currentItem = node?.Item;
                currentTextFile = await Controller.CodeEditor.GetTextFileAsync();
            }

            foreach (SystemVerilogCore.Documents.IVerilogIncludeFile includeFile in parsedDocument.IncludeFiles.Values)
            {
                // Narrow back to the editor's instance type: the item-tree node,
                // the colour-mark copy and the Messages view all need it. This is
                // the data layer, so the cast belongs here rather than in the
                // parser.
                if (includeFile is not Data.VerilogHeaderInstance header) continue;

                if (!headerItems.ContainsKey(header.ID)) continue;
                Data.VerilogHeaderInstance item = headerItems[header.ID];
                if (item.CodeDocument == null) continue;

                if (header.VerilogParsedDocument == null) continue;
                item.CodeDocument.CopyColorMarkFrom(header.VerilogParsedDocument.EditorCodeDocument());

                // If this include file is selected in the editor, update the editor display.
                if (item == currentItem && currentTextFile != null)
                {
                    Controller.CodeEditor.PostRefresh();
                    Controller.MessageView.Update(header.VerilogParsedDocument);
                }

                // update nested include file
                await updateIncludeFilesAsync(header.VerilogParsedDocument, item.Items);
            }
        }

        public SemaphoreSlim BaseParseSemapho = new SemaphoreSlim(1, 1);


        #region Instance ParsedDocument Registration
        // Instance ParsedDocument Registration
        // このファイルがインスタンスされたときにそのインスタンスのWeakReferenceを保持する

        private ConcurrentDictionary<string, System.WeakReference<SystemVerilogCore.Parsing.IParsedDocument>> instancedParsedDocumentRefs = new ConcurrentDictionary<string, WeakReference<SystemVerilogCore.Parsing.IParsedDocument>>();

        /// <summary>
        /// The instance parse result registered under <paramref name="key"/>,
        /// typed as the parser's own result.
        ///
        /// <para>
        /// The table itself stores the editor's <c>IParsedDocument</c> contract
        /// (so a <c>TextFile</c> can hold any plugin's result), but every
        /// caller in the parser immediately narrows it back to
        /// <see cref="Verilog.ParsedDocument"/>. Returning the narrowed type
        /// here keeps that downcast in this one editor-side place instead of
        /// spreading the editor contract through the parser.
        /// </para>
        /// </summary>
        public Verilog.ParsedDocument? GetInstancedVerilogParsedDocument(string key) =>
            GetInstancedParsedDocument(key) as Verilog.ParsedDocument;

        public SystemVerilogCore.Parsing.IParsedDocument? GetInstancedParsedDocument(string key)
        {
            SystemVerilogCore.Parsing.IParsedDocument? ret;
            if (key == "")
            {
                return ParsedDocument; // no lock is needed. ParsedDocument property is thread safe
            }
            else
            {
                if (instancedParsedDocumentRefs.TryGetValue(key, out WeakReference<SystemVerilogCore.Parsing.IParsedDocument>? weakRef))
                {
                    if (weakRef == null)
                    {
                        instancedParsedDocumentRefs.TryRemove(key, out _);
                        return null;
                    }
                    else
                    {
                        if (weakRef.TryGetTarget(out ret))
                        {
                            return ret;
                        }
                        else
                        {
                            // もしweakrefrenceの参照先が内容が失われているなら消しておく。
                            instancedParsedDocumentRefs.TryRemove(key, out _);
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Registers an instance's parse result under its parameter-qualified
        /// key (<see cref="Verilog.ParsedDocument.KeyGenerator"/>), which is
        /// what <see cref="InstanceTextFile.ParsedDocument"/> looks up.
        /// </summary>
        /// <remarks>
        /// <paramref name="moduleInstance"/> is the instance that produced the
        /// result; it is optional because a UI-less host (the LSP's
        /// CoreBridge.ParseEngine) registers the result straight from the
        /// parser without materialising a VerilogModuleInstance.
        /// </remarks>
        public void RegisterInstanceParsedDocument(string id, SystemVerilogCore.Parsing.IParsedDocument parsedDocument, InstanceTextFile? moduleInstance)
        {
            if (parsedDocument == null && System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            if (id == "")
            {
                ParsedDocument = parsedDocument; // no lock is needed. ParsedDocument property is thread safe
            }
            else
            {
                instancedParsedDocumentRefs.AddOrUpdate(id, new WeakReference<SystemVerilogCore.Parsing.IParsedDocument>(parsedDocument), (key, oldValue) => new WeakReference<SystemVerilogCore.Parsing.IParsedDocument>(parsedDocument));
            }
        }

        public void CleanWeakRef()
        {
            SystemVerilogCore.Parsing.IParsedDocument? ret;
            foreach (var r in instancedParsedDocumentRefs)
            {
                if (!r.Value.TryGetTarget(out ret)) instancedParsedDocumentRefs.TryRemove(r.Key, out _);
            }
        }

        /// <summary>
        /// Drops every per-instance parse result this file produced for its
        /// instances (module / interface instances keyed by module name +
        /// parameter overrides).
        /// </summary>
        /// <remarks>
        /// An instance caches the ParsedDocument parsed from its source file.
        /// When the source file's text changes, the cached results are stale:
        /// they were produced from the old text and the instance would keep
        /// showing the previous instantiations under every parent that
        /// expands it. A UI-less host (LSP server) has no editor re-parse loop
        /// to notice this, so it must invalidate the cache explicitly after it
        /// re-parses the source file.
        /// </remarks>
        public void ClearInstancedParsedDocuments()
        {
            instancedParsedDocumentRefs.Clear();
        }

        #endregion


        public override void Dispose()
        {
            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument; // no lock is needed. ParsedDocument property is thread safe
            if (vParsedDocument != null)
            {
                foreach (SystemVerilogCore.Documents.IVerilogIncludeFile incFile in vParsedDocument.IncludeFiles.Values)
                {
                    // Narrow to the editor's instance type: releasing the buffer
                    // is a data-layer concern (the contract only offers Close,
                    // which does not dispose the item).
                    (incFile as Data.VerilogHeaderInstance)?.Dispose();
                }
            }
            base.Dispose();
        }


        public Verilog.ParsedDocument? VerilogParsedDocument
        {
            get
            {
                return ParsedDocument as Verilog.ParsedDocument; // no lock is needed. ParsedDocument property is thread safe
            }
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




        #region instance file registration ----------------------------------------------------------------------------------------------------------

        // instance先からの逆weak reference
        // Reparse Requestの伝搬に使用する。
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

        #endregion


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
            Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = CodeDocument;
            if (codeDocument == null) return;
            if (codeDocument.Version != vParsedDocument.Version)
            {
                vParsedDocument.ReparseRequested = true;
            }
        }
        public void SetOutDated()
        {
            ReparseRequested = true;

            Verilog.ParsedDocument? vParsedDocument = VerilogParsedDocument;
            if (vParsedDocument == null) return;
            textFileLock.EnterWriteLock();
            try
            {
                List<string> blankId = new List<string>();
                foreach (var kvp in instanceDictionary)
                {
                    if (kvp.Value.TryGetTarget(out InstanceTextFile? instanceTextFile))
                    {
                        instanceTextFile.ReparseRequested = true;
                    }
                    else
                    {
                        blankId.Add(kvp.Key);
                    }
                }
                foreach (string id in blankId)
                {
                    instanceDictionary.Remove(id);
                }
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return CreateDocumentParser(parseMode, token, null);
        }

        /// <summary>
        /// UI-free parser factory (see
        /// <see cref="IVerilogRelatedFile.CreateVerilogDocumentParser"/>).
        /// Delegates to the editor-facing overload so both hosts share one
        /// implementation.
        /// </summary>
        public SystemVerilogCore.Parsing.IVerilogDocumentParser? CreateVerilogDocumentParser(
            SystemVerilogCore.Parsing.VerilogParseMode parseMode,
            System.Threading.CancellationToken? token)
        {
            return CreateDocumentParser(
                (DocumentParser.ParseModeEnum)(int)parseMode, token);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token, CodeEditor2.Data.ParseRuleDefinition.Rule? parseRuleOverride)
        {
            CheckDirty();

            // Get ParseRule from project settings
            CodeEditor2.Data.ParseRuleDefinition.Rule? rule = parseRuleOverride;
            if (rule == null)
            {
                rule = Project.GetParseRule(RelativePath);
            }

            // Apply ParseRule options before creating parser
            if (rule != null && rule.Options != null)
            {
                // Apply Disabled option (return null parser to disable parsing)
                if (rule.Options.Disabled)
                {
                    return null;
                }
            }

            // Create parser
            Parser.VerilogParser parser = new Parser.VerilogParser(this, parseMode, token);

            // Apply ParseRule options after creating parser
            if (rule != null && rule.Options != null)
            {
                // Apply SystemVerilog option
                if (rule.Options.SystemVerilog)
                {
                    parser.VerilogParsedDocument.SystemVerilog = true;
                }
            }

            return parser;
        }

        // update sub-items from ParsedDocument
        public override async Task UpdateAsync()
        {
            await VerilogCommon.Updater.UpdateAsync(this, itemUpdateSemaphore);
            await base.UpdateAsync();
        }

        public override Task ParseHierarchyAsync(Action<ITextFile> action)
        {
            Tool.ParseHierarchy.PostParseAsync(this, Tool.ParseHierarchy.ParseMode.ForceAllFiles);
            return Task.CompletedTask;
        }

        // UI override --------------------------------------------------------------------------------------
        
        public override PopupItem? GetPopupItem(ulong version, int index)
        {
            Verilog.ParsedDocument? parsedDoc = VerilogParsedDocument;
            if (parsedDoc == null) return null;
            return VerilogCommon.AutoComplete.GetPopupItem(this, parsedDoc, version, index);
        }

        public override List<ToolItem> GetToolItems(int index)
        {
            List<ToolItem> toolItems = new List<ToolItem>();
            if (CustomizeTooltem != null)
            {
                CustomizeTooltem?.Invoke(toolItems);
            }
            Verilog.ParsedDocument? parsedDoc = VerilogParsedDocument;
            if (parsedDoc == null) return toolItems;
            List<ToolItem> toolItems2 = VerilogCommon.AutoComplete.GetToolItems(this, index);
            foreach (ToolItem item in toolItems2)
            {
                toolItems.Add(item);
            }
            return toolItems;
        }
        public override SystemVerilogCore.Documents.VerilogCompletionContext? GetAutoCompleteItems(int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index);
        }


    }
}
