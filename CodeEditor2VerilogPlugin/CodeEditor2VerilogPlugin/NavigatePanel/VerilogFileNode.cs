using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel;
using CodeEditor2.Tools;
using System.Diagnostics.CodeAnalysis;
using DynamicData;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
using Avalonia.Controls;
using CodeEditor2.NavigatePanel;

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


        public override async void OnSelected()
        {
            try
            {
                base.OnSelected(); // update context menu

                if(TextFile == null)
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

                await Tool.ParseHierarchy.ParseAsync(TextFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
        }



        public override async Task UpdateAsync()
        {
            if(VerilogFile == null)
            {
                return;
            }
            await VerilogFile.UpdateAsync(); // UpdateVisual called in this method on the  UI thread
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
                lock (VerilogFile.Items)
                {
                    foreach (CodeEditor2.Data.Item item in VerilogFile.Items.Values)
                    {
                        newNodes.Add(item.NavigatePanelNode);
                    }
                }
            }

            List<CodeEditor2.NavigatePanel.NavigatePanelNode> removeNodes = new List<CodeEditor2.NavigatePanel.NavigatePanelNode>();
            lock (Nodes)
            {
                foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in Nodes)
                {
                    if (!newNodes.Contains(node))
                    {
                        removeNodes.Add(node);
                    }
                }
                foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in removeNodes)
                {
                    if (!Nodes.Contains(node))
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    Nodes.Remove(node);
                    node.Dispose();
                }

                foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in newNodes)
                {
                    if (Nodes.Contains(node)) continue;

                    int index = newNodes.IndexOf(node);
                    Nodes.Insert(index, node);
                    node.UpdateVisual();
                }
            }
        }

        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            string text = "-";
            if (FileItem != null) text=FileItem.Name;
            Text = text;

            if (VerilogFile == null) return;

            Image = GetIcon(VerilogFile);

            UpdateSubNodes();
        }

        public static IImage? GetIcon(IVerilogRelatedFile verilogRelatedFile)
        {
            // Icon badge will update only in UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI")
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

            Avalonia.Media.Color color = Avalonia.Media.Color.FromArgb(100, 200, 240, 240);

            if (verilogRelatedFile != null && verilogRelatedFile is InstanceTextFile)
            {
                if (((InstanceTextFile)verilogRelatedFile).ExternalProject)
                {
                    color = Avalonia.Media.Color.FromArgb(100, 250, 200, 200);
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

    }

}
