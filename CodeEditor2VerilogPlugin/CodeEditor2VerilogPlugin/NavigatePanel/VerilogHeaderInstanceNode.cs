using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Avalonia.Media;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogHeaderInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogHeaderInstanceNode(Data.VerilogHeaderInstance vhFile, CodeEditor2.Data.Project project) : base(vhFile)
        {

        }
        public Action NodeSelected;

        public Data.IVerilogRelatedFile VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }
        public CodeEditor2.Data.TextFile TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public Data.VerilogHeaderInstance VerilogHeaderInstance
        {
            get
            {
                return Item as Data.VerilogHeaderInstance;
            }
        }

        public override void OnSelected()
        {
            // activate navigate panel context menu
//            var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
//            if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;

            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile);
            if (NodeSelected != null) NodeSelected();
        }

        public override string Text
        {
            get { return FileItem.Name; }
        }

        public override void Update()
        {
            if (VerilogHeaderInstance == null) return;
            //VerilogHeaderInstance.Update();

            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            foreach (CodeEditor2.Data.Item item in VerilogHeaderInstance.Items.Values)
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

            UpdateVisual();
        }

        public override void UpdateVisual()
        {
            if (TextFile.CodeDocument.IsDirty)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 255, 255),
                    "CodeEditor2/Assets/Icons/shine.svg",
                    Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                    );
            }
            else
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogHeaderDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 255, 255)
                    );
            }
        }

    }
}
