using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    public class ImportedPackageNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public ImportedPackageNode(Data.ImportedPackage importedPackage) : base(importedPackage)
        {
            importedPackageRef = new WeakReference<Data.ImportedPackage>(importedPackage);
        }

        private System.WeakReference<Data.ImportedPackage> importedPackageRef;
        public Data.ImportedPackage? ImportedPackage
        {
            get
            {
                Data.ImportedPackage? ret;
                if (importedPackageRef == null) return null;
                if (!importedPackageRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public override CodeEditor2.Data.File? FileItem
        {
            get
            {
                Data.ImportedPackage? pkg = ImportedPackage;
                return pkg;
            }
        }

        public Data.IVerilogRelatedFile? VerilogRelatedFile
        {
            get
            {
                if (Item == null) return null;
                return (Data.IVerilogRelatedFile)Item;
            }
        }

        public CodeEditor2.Data.ITextFile? ITextFile
        {
            get { return Item as CodeEditor2.Data.ITextFile; }
        }

        public virtual Data.IVerilogRelatedFile? VerilogFile
        {
            get
            {
                Data.ImportedPackage? pkg = ImportedPackage;
                if (pkg == null) return null;
                return pkg.SourceTextFile as Data.IVerilogRelatedFile;
            }
        }

        public Data.ImportedPackage? VerilogImportedPackage
        {
            get
            {
                if (Item == null) return null;
                return (Data.ImportedPackage)Item;
            }
        }

        public override void OnDeSelected()
        {
            Data.ImportedPackage? pkg = ImportedPackage;
            if (pkg?.SourceTextFile == null) return;
            if (pkg?.ParsedDocument?.Version != null && pkg?.ParsedDocument?.Version == pkg?.CodeDocument?.Version) return;

            // 未parseのものが残っている場合はbackgroundでparseしておく
            pkg?.SourceTextFile.PostParse();
        }

        private volatile bool onSelecting = false;

#pragma warning disable VSTHRD100 // 理由: UIイベントの起点であり、内部で完全にtry-catchしているため安全
        public override async void OnSelected()
        {
            if (onSelecting) return;
            onSelecting = true;
            try
            {
                base.OnSelected(); // update context menu

                if (ImportedPackage == null)
                {
                    await UpdateAsync();
                    return;
                }

                // Open the source file containing the package definition.
                // The package's package body is rendered as part of the file's parsed view.
                Data.ImportedPackage? pkg = ImportedPackage;
                CodeEditor2.Data.TextFile? source = pkg?.SourceTextFile as CodeEditor2.Data.TextFile;
                if (source != null)
                {
                    await CodeEditor2.Controller.CodeEditor.SetTextFileAsync(source, true);
                }
                UpdateVisual();

                if (CodeEditor2.Global.StopParse) return;

                if (pkg != null)
                {
                    // post hier parse on background so the package body is up-to-date
                    Tool.ParseHierarchy.PostParseAsync(pkg, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
                }
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                onSelecting = false;
            }
        }

        public override async Task UpdateAsync()
        {
            if (VerilogImportedPackage == null) return;
            Data.IVerilogRelatedFile? source = VerilogFile;
            if (source == null)
            {
                return;
            }

            if (!Dispatcher.UIThread.CheckAccess())
            {
#pragma warning disable VSTHRD101 //try-catched
                Dispatcher.UIThread.Post(
                        new Action(async () =>
                        {
                            try
                            {
                                await VerilogImportedPackage.UpdateAsync(); // UpdateVisual called in this method on the UI thread
                            }
                            catch (Exception ex)
                            {
                                CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                            }
                        })
                    );
                return;
            }
            await VerilogImportedPackage.UpdateAsync();
            return;
        }


        public void UpdateSubNodes()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            List<CodeEditor2.NavigatePanel.NavigatePanelNode> newNodes = new List<CodeEditor2.NavigatePanel.NavigatePanelNode>();

            if (VerilogImportedPackage != null)
            {
                foreach (CodeEditor2.Data.Item item in VerilogImportedPackage.Items)
                {
                    newNodes.Add(item.NavigatePanelNode);
                }
            }

            System.Collections.ObjectModel.ObservableCollection<AjkAvaloniaLibs.Controls.TreeControls.TreeNode> nodes = new System.Collections.ObjectModel.ObservableCollection<AjkAvaloniaLibs.Controls.TreeControls.TreeNode>();
            foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in newNodes)
            {
                nodes.Add(node);
            }

            Nodes = nodes;
        }

        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            if (Item == null)
            {
                Text = "null";
            }
            else
            {
                Data.ImportedPackage pkg = (Data.ImportedPackage)Item;
                if (pkg.NameSpaceString != "")
                {
                    Text = pkg.NameSpaceString + "." + pkg.Name + " - " + pkg.PackageName;
                }
                else
                {
                    Text = pkg.Name + " - " + pkg.PackageName;
                }
            }

            if (VerilogFile == null) return;

            Image = GetIcon(VerilogFile);

            UpdateSubNodes();
        }

        public static IImage? GetIcon(Data.IVerilogRelatedFile verilogRelatedFile)
        {
            // Icon badge will update only in UI thread
            if (!Dispatcher.UIThread.CheckAccess())
            {
                throw new Exception();
            }

            List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon> overrideIcons = new List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon>();

            if (verilogRelatedFile.CodeDocument != null && verilogRelatedFile.CodeDocument.IsDirty)
            {
                overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                {
                    SvgPath = "CodeEditor2/Assets/Icons/shine.svg",
                    Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 200),
                    OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.UpRight
                });
            }

            if (verilogRelatedFile != null && verilogRelatedFile.VerilogParsedDocument != null)
            {
                if (verilogRelatedFile.VerilogParsedDocument.ErrorCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 20, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
                else if (verilogRelatedFile.VerilogParsedDocument.WarningCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
            }

            // Reuse the verilog document color so that packages are visually
            // consistent with regular Verilog source files.
            Avalonia.Media.Color color = Avalonia.Media.Color.FromArgb(100, 200, 240, 240);
            if (verilogRelatedFile is InstanceTextFile inst && inst.ExternalProject)
            {
                color = Avalonia.Media.Color.FromArgb(100, 250, 200, 200);
            }

            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                color,
                overrideIcons
                );
        }

        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;


    }
}
