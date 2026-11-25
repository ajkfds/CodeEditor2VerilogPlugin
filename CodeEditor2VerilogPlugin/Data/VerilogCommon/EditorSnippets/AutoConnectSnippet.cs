using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.Views;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items;
// SnippetBuilder now lives in the parent namespace
// (pluginVerilog.Data.VerilogCommon), which is already in scope here.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "Auto Connect Ports" tool.
    ///
    /// <para>
    /// The decision of <b>which signal matches a port</b> lives in
    /// <see cref="CoreBridge.AutoConnectAdapter.GetCandidates"/>, which the VS
    /// Code webview (<c>rtl/autoConnect</c>) also calls, so both hosts offer the
    /// same candidates. This class is the desktop renderer: it opens
    /// <see cref="Views.AutoConnectWindow"/> for the instantiation under the
    /// caret and writes the result back through <c>CreateString</c>.
    /// </para>
    /// </summary>
    public class AutoConnectSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AutoConnectSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.AutoConnect)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        private CodeEditor2.CodeEditor.CodeDocument? document;

        public override async Task ApplyAsync()
        {
            CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (file == null) return;
            document = file.CodeDocument;
            if (document == null) return;

            await base.ApplyAsync();
            await RunAsync();
        }

        private static Task? _currentTask;
        private static CancellationTokenSource? _cts;

        private async Task RunAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = Task.Run(async () => await RunBackGroundAsync(token), token);
            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _currentTask = null;
                await CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippetAsync();
            }
        }

        private async Task RunBackGroundAsync(CancellationToken token)
        {
            try
            {
                ModuleInstantiation? moduleInstantiation = null;
                BuildingBlock? buildingBlock = null;

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (document == null) return;
                    CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
                    if (file == null) return;
                    document = file.CodeDocument;
                    if (document == null) return;
                    if (document is not pluginVerilog.CodeEditor.CodeDocument vDocument) return;
                    if (file is not Data.IVerilogRelatedFile verilogFile) return;

                    ParsedDocument? parsedDocument = verilogFile.VerilogParsedDocument;
                    if (parsedDocument == null) return;

                    int index = document.CaretIndex;
                    buildingBlock = parsedDocument.GetBuildingBlockAt(index);
                    if (buildingBlock == null) return;

                    IndexReference iref = IndexReference.Create(parsedDocument, vDocument, index);
                    foreach (IBuildingBlockInstantiation instance in buildingBlock.GetBuildingBlockInstantiations())
                    {
                        if (instance is not ModuleInstantiation candidate) continue;
                        if (candidate.BeginIndexReference == null) continue;
                        if (candidate.LastIndexReference == null) continue;
                        if (iref.IsSmallerThan(candidate.BeginIndexReference)) continue;
                        if (iref.IsGreaterThan(candidate.LastIndexReference)) continue;
                        moduleInstantiation = candidate;
                    }
                });

                if (moduleInstantiation == null || buildingBlock == null) return;

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    Views.AutoConnectWindow autoConnectWindow = new Views.AutoConnectWindow(moduleInstantiation, buildingBlock);
                    autoConnectWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
                    if (!autoConnectWindow.Ready) return;
                    await CodeEditor2.Controller.ShowDialogAsync(autoConnectWindow);
                });

                // AutoConnectWindow.Complete() already wrote the accepted
                // connections into the instantiation's PortConnection, so the
                // text is regenerated here from the parse tree - the same way
                // rtl/autoConnectApply answers the VS Code webview.
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (document == null) return;
                    CodeEditor2.Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
                    if (file == null) return;
                    document = file.CodeDocument;
                    if (document == null) return;
                    if (document is not pluginVerilog.CodeEditor.CodeDocument vDocument) return;
                    if (moduleInstantiation.LastIndexReference == null) return;

                    string indent = vDocument.GetIndentString(vDocument.CaretIndex);
                    string? moduleString = moduleInstantiation.CreateString(indent);
                    if (moduleString == null)
                    {
                        CodeEditor2.Controller.AppendLog("illegal module instance", Avalonia.Media.Colors.Red);
                        return;
                    }

                    int start = moduleInstantiation.BeginIndexReference.Indexes[moduleInstantiation.BeginIndexReference.Indexes.Count - 1];
                    int last = moduleInstantiation.LastIndexReference.Indexes[moduleInstantiation.LastIndexReference.Indexes.Count - 1];
                    CodeEditor2.Controller.CodeEditor.SetCaretPosition(start);
                    document.Replace(start, last - start + 1, 0, moduleString);
                    CodeEditor2.Controller.CodeEditor.SetSelection(document.CaretIndex, document.CaretIndex);
                    CodeEditor2.Controller.CodeEditor.EntryParse();
                });
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _currentTask = null;
                await CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippetAsync();
            }
        }

        public override void Aborted()
        {
            _cts?.Cancel();
            CodeEditor2.Controller.CodeEditor.PostClearHighlight();
            document = null;
            base.Aborted();
        }
    }
}
