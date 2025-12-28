using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Views;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AjkAvaloniaLibs.Libs.Icons;
using static pluginVerilog.Tool.ParseHierarchy;

namespace pluginVerilog.Verilog.Snippets
{
    public class AutoConnectSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AutoConnectSnippet() : base("autoConnect")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        private CodeEditor2.CodeEditor.CodeDocument? document;

        public override void Apply()
        {
            List<int> startIndexes = new List<int>();
            List<int> lastIndexes = new List<int>();

            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            document = file.CodeDocument;
            if (document == null) return;

            ParsedDocument? parsedDocument = file.ParsedDocument as ParsedDocument;


            string indent = "";
            if (document.GetCharAt(document.GetLineStartIndex(document.GetLineAt(document.CaretIndex))) == '\t')
            {
                indent = "\t";
            }

            base.Apply();

            // run async task
            System.Threading.Tasks.Task.Run(RunAsync);
        }

        // backrtound thread ------------------------------------------------------

        private static System.Threading.Tasks.Task? _currentTask;
        private static CancellationTokenSource? _cts;
        private async System.Threading.Tasks.Task RunAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = System.Threading.Tasks.Task.Run(async () =>
            {
                await runBackGround(token);
            }, token);

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
            return;
        }
        private async System.Threading.Tasks.Task runBackGround(CancellationToken token)
        {
            try
            {
                ModuleInstantiation? moduleInstantiation = null;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (document == null) return;
                    CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
                    if (file == null) return;
                    document = file.CodeDocument;
                    if (document == null) return;
                    pluginVerilog.CodeEditor.CodeDocument? vDocument = document as pluginVerilog.CodeEditor.CodeDocument;
                    if (vDocument == null) return;


                    ParsedDocument? parsedDocument = file.ParsedDocument as ParsedDocument;
                    int index = document.CaretIndex;

                    if (parsedDocument == null) return;
                    BuildingBlocks.BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(index);
                    if (buildingBlock == null) return;
                    IndexReference iref = IndexReference.Create(parsedDocument, vDocument, index);

                    List<IBuildingBlockInstantiation> instances = buildingBlock.GetBuildingBlockInstantiations();

                    foreach (IBuildingBlockInstantiation instance in instances)
                    {
                        if (iref.IsSmallerThan(instance.BeginIndexReference)) continue;
                        if (instance.LastIndexReference == null) continue;
                        if (iref.IsGreaterThan(instance.LastIndexReference)) continue;
                        if (instance is not ModuleInstantiation) continue;
                        moduleInstantiation = (ModuleInstantiation)instance;
                    }
                });
                if (moduleInstantiation == null) return;

                await Dispatcher.UIThread.InvokeAsync(async () => {
                    Views.AutoConnectWindow autoConnectWindow = new Views.AutoConnectWindow(moduleInstantiation);
                    autoConnectWindow.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner;
                    Avalonia.Controls.Window window = CodeEditor2.Controller.GetMainWindow();
                    if (autoConnectWindow.Ready)
                    {
                        await autoConnectWindow.ShowDialog(window);
                    }
                    if (!autoConnectWindow.Accept) return;
                });

               

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (document == null) return;
                    CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
                    if (file == null) return;
                    document = file.CodeDocument;
                    if (document == null) return;
                    pluginVerilog.CodeEditor.CodeDocument? vDocument = document as pluginVerilog.CodeEditor.CodeDocument;
                    if (vDocument == null) return;

                    string indent = vDocument.GetIndentString(vDocument.CaretIndex);

                    string? moduleString = moduleInstantiation.CreateString("\t");
                    if (moduleString == null)
                    {
                        CodeEditor2.Controller.AppendLog("illegal module instance", Avalonia.Media.Colors.Red);
                        return;
                    }
                    if (moduleInstantiation.LastIndexReference == null) return;

                    CodeEditor2.Controller.CodeEditor.SetCaretPosition(moduleInstantiation.BeginIndexReference.Indexes.Last());
                    document.Replace(
                        moduleInstantiation.BeginIndexReference.Indexes.Last(),
                        moduleInstantiation.LastIndexReference.Indexes.Last() - moduleInstantiation.BeginIndexReference.Indexes.Last() + 1,
                        0,
                        moduleString
                        );
                    CodeEditor2.Controller.CodeEditor.SetSelection(document.CaretIndex, document.CaretIndex);
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
            return;
        }

        // UI thread handler ------------------------------------------------------

        public override void Aborted()
        {
            if (_cts != null) _cts.Cancel();
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            document = null;
            base.Aborted();
        }

        public override void KeyDown(object? sender, KeyEventArgs e, PopupMenuView popupMenuView)
        {
        }
        public override void BeforeKeyDown(object? sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {
        }
        public override void AfterKeyDown(object? sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {
        }
        public override void AfterAutoCompleteHandled(CodeEditor2.Views.PopupMenuView popupMenuView)
        {
        }

        // return @ carlet line changed
        public override void Caret_PositionChanged(object? sender, EventArgs e)
        {

        }

    }
}

