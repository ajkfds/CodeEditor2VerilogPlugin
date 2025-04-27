using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Media;
using System.Security.AccessControl;
using Avalonia.Threading;

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

        public override string Text
        {
            get
            {
                if (Item == null) return "null";
                Data.VerilogModuleInstance instance = (Data.VerilogModuleInstance)Item;
                return instance.Name + " - " + instance.ModuleName;
            }
        }



        public override async void OnSelected()
        {
            //var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
            //if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;


            System.Diagnostics.Debug.Print("## VerilogModuleInstanceNode.OnSelected");
            if (ModuleInstance != null && ModuleInstance.ParseValid & !ModuleInstance.ReparseRequested)
            {
                CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance, true);
                Update();
            }
            else
            {
                await parseHierarchy();
                if (ModuleInstance != null) CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance, true);
                Update();
            }

        }

        private async Task parseHierarchy()
        {
            System.Diagnostics.Debug.Print("## VerilogFileNode.OnSelected.PharseHier.Run");
            if(ModuleInstance == null) return;
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
            Dispatcher.UIThread.InvokeAsync(() => {
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
                Image = VerilogFileNode.GetIcon(VerilogFile);
            });

        }

    }

}
