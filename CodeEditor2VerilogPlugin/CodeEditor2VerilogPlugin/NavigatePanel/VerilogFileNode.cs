﻿using System;
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

namespace pluginVerilog.NavigatePanel
{
    public class VerilogFileNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        [SetsRequiredMembers]
        public VerilogFileNode(Data.VerilogFile verilogFile) : base(verilogFile)
        {
            UpdateVisual();
            if (NodeCreated != null) NodeCreated(this);
        }
        public static Action<VerilogFileNode> NodeCreated;

        public Action NodeSelected;

        public Data.IVerilogRelatedFile VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }

        public CodeEditor2.Data.TextFile TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public virtual Data.VerilogFile VerilogFile
        {
            get { return Item as Data.VerilogFile; }
        }

        public override string Text
        {
            get {
                if (FileItem == null) return "-";
                return FileItem.Name;
            }
        }


        public override async void OnSelected()
        {
            // activate navigate panel context menu
            //var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
            //if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;
            //if (menu.Items.ContainsKey("icarusVerilogTsmi")) menu.Items["icarusVerilogTsmi"].Visible = true;
            //if (menu.Items.ContainsKey("VerilogDebugTsmi")) menu.Items["VerilogDebugTsmi"].Visible = true;

            System.Diagnostics.Debug.Print("## VerilogFileNode.OnSelected");

            if(TextFile.ParseValid & !TextFile.ReparseRequested)
            {
                CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile,true);
                if (NodeSelected != null) NodeSelected();
                Update();
            }
            else
            {
                CodeEditor2.Global.StopBackGroundParse = true;
                await parseHierarchy();
                CodeEditor2.Global.StopBackGroundParse = false;
                CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile, true);
                if (NodeSelected != null) NodeSelected();
                Update();
            }
        }

        // stop edit and parse all hierachy module
        private async Task parseHierarchy()
        {
            System.Diagnostics.Debug.Print("## VerilogFileNode.OnSelected.PharseHier.Run");
            await CodeEditor2.Tools.ParseHierarchy.Run(this);
            if (NodeSelected != null) NodeSelected();
            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile, true);
        }


        public override void Update()
        {
            if(VerilogFile == null)
            {
                return;
            }
            VerilogFile.Update();
            UpdateVisual();
        }

        public override void UpdateVisual()
        {
            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            lock (VerilogFile.Items)
            {
                foreach (CodeEditor2.Data.Item item in VerilogFile.Items.Values)
                {
                    targetDataItems.Add(item);
                    addDataItems.Add(item);
                }

                List<CodeEditor2.NavigatePanel.NavigatePanelNode> removeNodes = new List<CodeEditor2.NavigatePanel.NavigatePanelNode>();
                foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in Nodes)
                {
                    if (node.Item != null && targetDataItems.Contains(node.Item))
                    {
                        addDataItems.Remove(node.Item);
                    }
                    else
                    {
                        removeNodes.Add(node);
                    }
                }

                foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in removeNodes)
                {
                    Nodes.Remove(node);
                    node.Dispose();
                }

                int treeIndex = 0;
                foreach (CodeEditor2.Data.Item item in targetDataItems)
                {
                    if (item == null) continue;
                    if (addDataItems.Contains(item))
                    {
                        Nodes.Insert(treeIndex, item.NavigatePanelNode);
                    }
                    treeIndex++;
                }
            }

            if (VerilogFile == null) return;

            // Icon badge will update only in UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI") return;
            Image = GetIcon(VerilogFile);
        }

        public static IImage? GetIcon(Data.VerilogFile verilogFile)
        {
            // Icon badge will update only in UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI")
            {
                throw new Exception();
            }

            List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon> overrideIcons = new List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon>();

            if (verilogFile.CodeDocument != null && verilogFile.CodeDocument.IsDirty)
            {
                overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                {
                    SvgPath = "CodeEditor2/Assets/Icons/shine.svg",
                    Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 200),
                    OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.UpRight
                });
            }

            if (verilogFile != null && verilogFile.VerilogParsedDocument != null)
            {
                if (verilogFile.VerilogParsedDocument.ErrorCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 20, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
                else if (verilogFile.VerilogParsedDocument.WarningCount > 0)
                {
                    overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                    {
                        SvgPath = "CodeEditor2VerilogPlugin/Assets/Icons/exclamation_triangle.svg",
                        Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 20),
                        OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.DownLeft
                    });
                }
            }


            if (verilogFile != null && verilogFile.SystemVerilog)
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240),
                    overrideIcons
                    );
            }
            else
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240),
                    overrideIcons
                    );
            }
        }

    }

}
