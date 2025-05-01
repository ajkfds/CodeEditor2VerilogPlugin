using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Avalonia.Media;
using pluginVerilog.Data;
using Avalonia.Threading;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogHeaderInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogHeaderInstanceNode(Data.VerilogHeaderInstance vhFile, CodeEditor2.Data.Project project) : base(vhFile)
        {

        }
        public Action? NodeSelected;

        public Data.IVerilogRelatedFile? VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }
        public CodeEditor2.Data.TextFile? TextFile
        {
            get { return Item as CodeEditor2.Data.TextFile; }
        }

        public Data.VerilogHeaderInstance? VerilogHeaderInstance
        {
            get
            {
                return Item as Data.VerilogHeaderInstance;
            }
        }

        public override void OnSelected()
        {
            if(TextFile == null) return;

            CodeEditor2.Controller.CodeEditor.SetTextFile(TextFile);
            if (NodeSelected != null) NodeSelected();
        }


        public override void Update()
        {
            if (VerilogHeaderInstance == null) return;
            //VerilogHeaderInstance.Update();

            UpdateVisual();
        }

        public override void UpdateVisual()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                _updateVisual();
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _updateVisual();
                });
            }
        }
        public void _updateVisual()
        {
            if (FileItem == null)
            {
                Text = "?";
            }
            else
            {
                Text = FileItem.Name;
            }


            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            VerilogHeaderInstance? header = VerilogHeaderInstance;
            if (header == null) return;

            foreach (CodeEditor2.Data.Item item in header.Items.Values)
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



            // Icon badge will update only in the UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI") return;

            // // Select the same icon as VerilogHeaderNode
            IVerilogRelatedFile? verilogRelatedFile = TextFile as IVerilogRelatedFile;
            if (verilogRelatedFile == null) return;
            Image = VerilogHeaderNode.GetIcon(verilogRelatedFile);
        }

    }
}
