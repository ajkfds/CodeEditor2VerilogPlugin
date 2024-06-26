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

namespace pluginVerilog.NavigatePanel
{
    public class VerilogFileNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
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
            else if (CodeEditor2.Global.StopParse)
            {
                CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile, true);
                if (NodeSelected != null) NodeSelected();
                Update();
            }
            else
            {
                var _ = parseHierarchy();
            }
        }

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

            //    if (Link) graphics.DrawImage(CodeEditor2.Global.IconImages.Link.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));

            //    if (VerilogFile != null && VerilogFile.ParsedDocument != null && VerilogFile.VerilogParsedDocument.ErrorCount != 0)
            //    {
            //        graphics.DrawImage(Global.Icons.Exclamation.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
            //    }

            //    if (VerilogFile != null && VerilogFile.Dirty)
            //    {
            //        graphics.DrawImage(Global.Icons.NewBadge.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
            //    }

            if (VerilogFile.SystemVerilog)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                    );
            }
            else
            {
                if (TextFile.CodeDocument != null && TextFile.CodeDocument.IsDirty)
                {
                    Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                        "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                        Avalonia.Media.Color.FromArgb(100, 200, 240, 240),
                        "CodeEditor2/Assets/Icons/shine.svg",
                        Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                        );
                }
                else
                {
                    Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                        "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                        Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                        );
                }
            }

        }

    }

}
