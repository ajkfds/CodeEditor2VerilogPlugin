using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Media;
using Avalonia.Threading;

namespace pluginVerilog.NavigatePanel
{
    public class InterfaceInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public InterfaceInstanceNode(Data.InterfaceInstance verilogModuleInstance) : base(verilogModuleInstance)
        {
            moduleInstanceRef = new WeakReference<Data.InterfaceInstance>(verilogModuleInstance);
        }

        public bool NeedUpdate
        {
            get
            {
                return false;
            }
        }
        private System.WeakReference<Data.InterfaceInstance> moduleInstanceRef;
        public Data.InterfaceInstance ModuleInstance
        {
            get
            {
                Data.InterfaceInstance ret;
                if (moduleInstanceRef == null) return null;
                if (!moduleInstanceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public override CodeEditor2.Data.File FileItem
        {
            get
            {
                Data.InterfaceInstance instance = ModuleInstance;
                return instance;
            }
        }

        public Data.IVerilogRelatedFile VerilogRelatedFile
        {
            get { return Item as Data.IVerilogRelatedFile; }
        }
        public CodeEditor2.Data.ITextFile ITextFile
        {
            get { return Item as CodeEditor2.Data.ITextFile; }
        }

        public virtual Data.VerilogFile VerilogFile
        {
            get
            {
                if (ModuleInstance == null) return null;
                return ModuleInstance.SourceTextFile as Data.VerilogFile;
                //                Data.VerilogModuleInstance instance = Project.GetRegisterdItem(ID) as Data.VerilogModuleInstance;
                //                return Project.GetRegisterdItem(Data.VerilogFile.GetID(instance.RelativePath, Project)) as Data.VerilogFile;
                //                return null;
            }
        }

        public Data.VerilogModuleInstance VerilogModuleInstance
        {
            get
            {
                return Item as Data.VerilogModuleInstance;
            }
        }

        public override string Text
        {
            get
            {
                Data.VerilogModuleInstance instance = Item as Data.VerilogModuleInstance;
                return instance.Name + " - " + instance.ModuleName;
            }
        }



        public override async void OnSelected()
        {
            //var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
            //if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;


//            System.Diagnostics.Debug.Print("## VerilogModuleInstanceNode.OnSelected");

            if (ModuleInstance.ParseValid & !ModuleInstance.ReparseRequested)
            {
                CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance, true);
                Update();
            }
            else
            {
                await parseHierarchy();
                CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance, true);
                Update();
            }

        }

        private async Task parseHierarchy()
        {
//            System.Diagnostics.Debug.Print("## VerilogFileNode.OnSelected.PharseHier.Run");
            await CodeEditor2.Tools.ParseHierarchy.Run(ModuleInstance.NavigatePanelNode);
        }

        public override void Update()
        {
            if (VerilogModuleInstance == null) return;
            VerilogModuleInstance.Update();


            UpdateVisual();
        }
        public override void UpdateVisual()
        {
            if (Dispatcher.UIThread.CheckAccess()){
                _updateVisual();
            }
            else {
                Dispatcher.UIThread.Post(() =>
                {
                    _updateVisual();
                });
            }
        }

        public void _updateVisual()
        {
            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            foreach (CodeEditor2.Data.Item item in VerilogModuleInstance.Items.Values)
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

            if (VerilogFile == null) return;

            // Icon badge will update only in UI thread
            if (System.Threading.Thread.CurrentThread.Name != "UI") return;
            Image = VerilogFileNode.GetIcon(VerilogFile);

        }

    }

}
