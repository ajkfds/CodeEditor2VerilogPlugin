using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogFileNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        static VerilogFileNode()
        {
            CustomizeNavigateNodeContextMenu += CustomizeNavigateNodeContextMenuHandler;
        }
        public static void CustomizeNavigateNodeContextMenuHandler(ContextMenu contextMenu)
        {
            NavigatePanelMenu.Customize(contextMenu);
        }

        [SetsRequiredMembers]
        public VerilogFileNode(Data.VerilogFile verilogFile) : base(verilogFile)
        {
            Dispatcher.UIThread.Post(UpdateVisual);

            if (NodeCreated != null) NodeCreated(this);
        }

        public static Action<VerilogFileNode>? NodeCreated;

        public Action? NodeSelected;

        public Data.IVerilogRelatedFile? VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }

        public CodeEditor2.Data.TextFile? TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public virtual Data.VerilogFile? VerilogFile
        {
            get { return Item as Data.VerilogFile; }
        }


        private volatile bool onSelecting = false;
        public override async void OnSelected()
        {
            if (onSelecting) return;    // select re-entrantの抑止
            onSelecting = true;
            try
            {
                base.OnSelected(); // update context menu

                if (TextFile == null)
                {
                    if (NodeSelected != null) NodeSelected();
                    await UpdateAsync();
                    return;
                }

                await CodeEditor2.Controller.CodeEditor.SetTextFileAsync(TextFile, true);
                if (NodeSelected != null) NodeSelected();

                UpdateVisual();
                if (CodeEditor2.Global.StopParse) return;

                if (TextFile == null) return;

                // post hier parse on background
                Tool.ParseHierarchy.PostParseAsync(TextFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
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
            return;
        }

        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            string text = "-";
            if (FileItem != null) text = FileItem.Name;
            Text = text;

            if (VerilogFile == null) return;

            Image = GetIcon(VerilogFile);

            UpdateSubNodes();
        }

        public void UpdateSubNodes()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            List<CodeEditor2.NavigatePanel.NavigatePanelNode> newNodes = new List<CodeEditor2.NavigatePanel.NavigatePanelNode>();

            if (VerilogFile != null)
            {
                foreach (CodeEditor2.Data.Item item in VerilogFile.Items)
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



        public static IImage? GetIcon(IVerilogRelatedFile verilogRelatedFile)
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
                    Color = CodeEditor2.Global.Color_Shine,
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
                        Color = CodeEditor2.Global.Color_Error,
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
                else if (verilogRelatedFile.VerilogParsedDocument.WarningCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = CodeEditor2.Global.Color_Warning,
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
            }

            Avalonia.Media.Color color = Global.Color_Verilog;

            if (verilogRelatedFile != null && verilogRelatedFile is InstanceTextFile)
            {
                if (((InstanceTextFile)verilogRelatedFile).ExternalProject)
                {
                    color = Global.Color_VerilogExternalProject;
                }
            }

            if (verilogRelatedFile != null && verilogRelatedFile.SystemVerilog)
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    color,
                    overrideIcons
                    );
            }
            else
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                    color,
                    overrideIcons
                    );
            }
        }
        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;



    }

}
