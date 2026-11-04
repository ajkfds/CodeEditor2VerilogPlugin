using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using Avalonia.Threading;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class InterfaceInstance : InstanceTextFile, IVerilogRelatedFile
    {

        public InterfaceInstance(CodeEditor2.Data.TextFile sourceTextFile) : base(sourceTextFile)
        {

        }
        public static InterfaceInstance? Create(
            Verilog.Items.InterfaceInstance moduleInstantiation,
            CodeEditor2.Data.Project project
            )
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            Data.IVerilogRelatedFile? file = projectProperty.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName);
            if (file == null) return null;

            CodeEditor2.Data.TextFile? textFile = file as CodeEditor2.Data.TextFile;
            if (textFile == null) throw new Exception();
            InterfaceInstance fileItem = new InterfaceInstance(textFile)
            {
                InstanceName = moduleInstantiation.SourceName,
                ParameterOverrides = moduleInstantiation.ParameterOverrides,
                Project = project,
                RelativePath = file.RelativePath,
                Name = moduleInstantiation.Name
            };

            if (file is Data.VerilogFile)
            {
                Data.VerilogFile? vFile = file as Data.VerilogFile;
                if (vFile == null) throw new Exception();

                if (vFile.SystemVerilog) fileItem.SystemVerilog = true;
            }
            return fileItem;
        }

        public override string ID
        {
            get
            {
                return _getKey();
            }
        }

        protected WeakReference<Verilog.Items.InterfaceInstance> weakInstantiating;

        /// <summary>
        /// The interface instantiation this instance was created from (null for
        /// instances built without one). Mirrors
        /// <see cref="VerilogModuleInstance.GetParentInstanciatiation"/> so hosts
        /// (e.g. the LSP RTL hierarchy) can inspect the instantiating item.
        /// </summary>
        public Verilog.Items.InterfaceInstance? GetParentInstanciatiation()
        {
            if (weakInstantiating == null) return null;
            weakInstantiating.TryGetTarget(out var interfaceInstantiation);
            return interfaceInstantiation;
        }

        public override void Dispose()
        {
            disposeItems();
        }

        private void disposeItems()
        {
            if (VerilogParsedDocument != null)
            {
                if (ParsedDocument != null && ParameterOverrides.Count != 0)
                {
                    foreach (var incFile in VerilogParsedDocument.IncludeFiles.Values)
                    {
                        incFile.Dispose();
                    }
                }
            }

            //ParsedDocument = null;
            //            SourceVerilogFile.RemoveModuleInstance(this);
        }





        public override void Close()
        {
            if (VerilogParsedDocument != null) VerilogParsedDocument.ReloadIncludeFiles();
            SourceVerilogFile.Close();
        }


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
        public override async Task SaveAsync()
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

            Data.VerilogFile source = SourceVerilogFile;
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
            //CodeEditor2.CodeEditor.ParsedDocument? newParsedDocument = parser.ParsedDocument;
            //if (newParsedDocument == null) return;

            //Data.VerilogFile source = SourceVerilogFile;
            //if (source == null) return;

            //if (ParameterOverrides.Count == 0)
            //{
            //    await source.AcceptParsedDocumentAsync(parser);
            //}
            //else
            //{
            //    source.RegisterInstanceParsedDocument(ModuleName + ":" + ParameterId, newParsedDocument, this);
            //    source.RegisterInstanceFile(this);
            //    await acceptParameterizedParsedDocument(newParsedDocument);
            //}

            //// Editor-dependent update must run only when the editor UI is
            //// alive (see VerilogFile.AcceptParsedDocumentAsync).
            //TextFile? currentTextFile = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            //if (currentTextFile == this)
            //{
            //    currentTextFile.CodeDocument?.CopyColorMarkFrom(parser.Document);
            //    CodeEditor2.Controller.MessageView.Update(newParsedDocument);
            //    CodeEditor2.Controller.CodeEditor.PostRefresh();
            //}

            //// refresh the navigate panel node icon (error/warning badge, ...)
            //// after the parsed document is updated (same as
            //// VerilogHeaderInstance.AcceptParsedDocumentAsync).
            //// Post (not Invoke) to avoid blocking this background parse
            //// thread on the UI thread.
            //Dispatcher.UIThread.Post(() => NavigatePanelNode.UpdateVisual());
        }
        private async Task postUpdateAsync(Verilog.ParsedDocument newParsedDocument)
        {
            try
            {
                TextFile? textFile = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
                if (textFile == this)
                {
                    if (CodeDocument != null) textFile.CodeDocument?.CopyColorMarkFrom(newParsedDocument.CodeDocument);
                    if (ParsedDocument != null) CodeEditor2.Controller.MessageView.Update(newParsedDocument);
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
            NavigatePanel.InterfaceInstanceNode node = new NavigatePanel.InterfaceInstanceNode(this);
            return node;
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            //            return new Parser.VerilogParser(this, ModuleName, ParameterOverrides, parseMode);
            return new Parser.VerilogParser(this.SourceVerilogFile, InstanceName, ParameterOverrides, parseMode, token);
        }

        private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
        public override async Task UpdateAsync()
        {
            await VerilogCommon.Updater.UpdateAsync(this, _updateSemaphore);
            await base.UpdateAsync();
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

        public override CodeEditor2.CodeEditor.CodeComplete.CompletionContext? GetAutoCompleteItems(int index)
        {
            if (VerilogParsedDocument == null) return null;
            return VerilogCommon.AutoComplete.GetAutoCompleteItems(this, VerilogParsedDocument, index);
        }


    }
}

