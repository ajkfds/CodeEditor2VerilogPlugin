using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Media;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogFileNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogFileNode(Data.VerilogFile verilogFile) : base(verilogFile)
        {
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

        public override IImage? Image
        {
            get
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2VerilogPlugin/Assets/Icons/verilogPaper.svg");
            }
        }

        //public override void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected)
        //{
        //    graphics.DrawImage(Global.Icons.Verilog.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));
        //    Color bgColor = backgroundColor;
        //    if (selected) bgColor = selectedColor;
        //    System.Windows.Forms.TextRenderer.DrawText(
        //        graphics,
        //        Text,
        //        font,
        //        new Point(x + lineHeight + (lineHeight >> 2), y),
        //        color,
        //        bgColor,
        //        System.Windows.Forms.TextFormatFlags.NoPadding
        //        );

        //    if (Link) graphics.DrawImage(CodeEditor2.Global.IconImages.Link.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Blue), new Point(x, y));

        //    if (VerilogFile != null && VerilogFile.ParsedDocument != null && VerilogFile.VerilogParsedDocument.ErrorCount != 0)
        //    {
        //        graphics.DrawImage(Global.Icons.Exclamation.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
        //    }

        //    if (VerilogFile != null && VerilogFile.Dirty)
        //    {
        //        graphics.DrawImage(Global.Icons.NewBadge.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
        //    }
        //}

        public override void OnSelected()
        {
            // activate navigate panel context menu
            //var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
            //if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;
            //if (menu.Items.ContainsKey("icarusVerilogTsmi")) menu.Items["icarusVerilogTsmi"].Visible = true;
            //if (menu.Items.ContainsKey("VerilogDebugTsmi")) menu.Items["VerilogDebugTsmi"].Visible = true;

            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile);

            if (!TextFile.ParseValid | TextFile.ReparseRequested)
            {
                if (!CodeEditor2.Global.StopParse)
                {
                    CodeEditor2.Tools.ParseHierarchyForm pform = new CodeEditor2.Tools.ParseHierarchyForm(this);
                    CodeEditor2.Controller.ShowDialogForm(pform);
                }
            }

            //foreach (BuildingBlock module in VerilogFile.VerilogParsedDocument.Root.BuldingBlocks.Values)
            //{
            //    VerilogFile.CodeDocument.ExpandBlock(VerilogFile.CodeDocument.GetLineAt(module.BeginIndexReference.Indexs.Last() ));
            //}

            if (NodeSelected != null) NodeSelected();
        }


        public override void Update()
        {
            if(VerilogFile == null)
            {
                return;
            }
            VerilogFile.Update();

            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
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

    }

}
