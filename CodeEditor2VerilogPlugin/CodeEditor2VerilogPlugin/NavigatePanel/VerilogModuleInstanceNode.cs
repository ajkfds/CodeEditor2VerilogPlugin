using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogModuleInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogModuleInstanceNode(Data.VerilogModuleInstance verilogModuleInstance) : base(verilogModuleInstance)
        {
            moduleInstanceRef = new WeakReference<Data.VerilogModuleInstance>(verilogModuleInstance);
        }
        private System.WeakReference<Data.VerilogModuleInstance> moduleInstanceRef;
        public Data.VerilogModuleInstance? ModuleInstance
        {
            get
            {
                Data.VerilogModuleInstance? ret;
                if (moduleInstanceRef == null) return null;
                if (!moduleInstanceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }
        public override CodeEditor2.Data.File? FileItem
        {
            get
            {
                Data.VerilogModuleInstance? instance = ModuleInstance;
                return instance;
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

        public virtual Data.VerilogFile? VerilogFile
        {
            get
            {
                if (ModuleInstance == null) return null;
                return ModuleInstance.SourceTextFile as Data.VerilogFile;
            }
        }
        public Data.VerilogModuleInstance? VerilogModuleInstance
        {
            get
            {
                if (Item == null) return null;
                return (Data.VerilogModuleInstance)Item;
            }
        }




        public override async void OnSelected()
        {
            base.OnSelected(); // update context enu

            if (ModuleInstance == null)
            {
                Update();
                return;
            }

            //System.Diagnostics.Debug.Print("## VerilogModuleInstanceNode.OnSelected");



            CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance, true);
            UpdateVisual();

            //            Update();

            //            foreach (NavigatePanelNode node in Nodes)
            //            {
            ////                if (node is VerilogModuleInstanceNode)
            ////                {
            ////                    ((VerilogModuleInstanceNode)node).Update();
            ////                }
            //                node.UpdateVisual();
            //            }
            if (CodeEditor2.Global.StopParse) return;

            //if (ModuleInstance.ParseValid & !ModuleInstance.ReparseRequested)
            //{
            //    // skip parse
            //}
            //else
            //{
                await Tool.ParseHierarchy.ParseAsync(ModuleInstance, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            //}
        }

        public override void Update()
        {
            if (VerilogModuleInstance == null) return;
            VerilogModuleInstance.Update(); // UpdateVisual called in this method on the  UI thread


//            UpdateVisual();
        }

        public override void UpdateVisual()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _updateVisual();
                }
                catch (Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                    throw;
                }
            });
        }
        public void _updateVisual()
        {
            if (Item == null)
            {
                Text = "null";
            }
            else
            {
                Data.VerilogModuleInstance instance = (Data.VerilogModuleInstance)Item;
                Text = instance.Name + " - " + instance.ModuleName;
            }

            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            if (VerilogModuleInstance != null)
            {
                lock (VerilogModuleInstance.Items)
                {
                    foreach (CodeEditor2.Data.Item item in VerilogModuleInstance.Items.Values)
                    {
                        targetDataItems.Add(item);
                        addDataItems.Add(item);
                    }
                }
            }

            List<CodeEditor2.NavigatePanel.NavigatePanelNode> removeNodes = new List<CodeEditor2.NavigatePanel.NavigatePanelNode>();
            lock (Nodes)
            {
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
                    if (!Nodes.Contains(node))
                    {
                        System.Diagnostics.Debugger.Break();
                    }
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

            if(VerilogModuleInstance != null) Image = VerilogFileNode.GetIcon(VerilogModuleInstance);

        }

    }

}
