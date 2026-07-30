using Avalonia.Controls;
using Avalonia.Threading;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.NavigatePanel
{
    public class InterfaceInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public InterfaceInstanceNode(Data.InterfaceInstance interfaceInstance) : base(interfaceInstance)
        {
            interfaceInstanceRef = new WeakReference<Data.InterfaceInstance>(interfaceInstance);
        }

        public bool NeedUpdate
        {
            get
            {
                return false;
            }
        }
        private System.WeakReference<Data.InterfaceInstance> interfaceInstanceRef;
        public Data.InterfaceInstance InterfaceInstance
        {
            get
            {
                Data.InterfaceInstance ret;
                if (interfaceInstanceRef == null) return null;
                if (!interfaceInstanceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public override CodeEditor2.Data.File FileItem
        {
            get
            {
                Data.InterfaceInstance instance = InterfaceInstance;
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
                if (InterfaceInstance == null) return null;
                return InterfaceInstance.SourceTextFile as Data.VerilogFile;
                //                Data.VerilogModuleInstance instance = Project.GetRegisterdItem(ID) as Data.VerilogModuleInstance;
                //                return Project.GetRegisterdItem(Data.VerilogFile.GetID(instance.RelativePath, Project)) as Data.VerilogFile;
                //                return null;
            }
        }

        public Data.InterfaceInstance VerilogModuleInstance
        {
            get
            {
                return Item as Data.InterfaceInstance;
            }
        }



        public override async void OnSelected()
        {
            try
            {
                base.OnSelected(); // update context menu
                if (InterfaceInstance == null)
                {
                    await UpdateAsync();
                    return;
                }


                await CodeEditor2.Controller.CodeEditor.SetTextFileAsync(InterfaceInstance, true);

                UpdateVisual();

                if (!InterfaceInstance.ReparseRequested)
                {
                    // skip parse
                }
                else
                {
                    if (InterfaceInstance == null) return;

                    try
                    {
                        Tool.ParseHierarchy.PostParseAsync(InterfaceInstance, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
                    }
                    catch (Exception ex)
                    {
                        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                        CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                        throw;
                    }
                }

            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
        }


        public override async Task UpdateAsync()
        {
            if (VerilogModuleInstance == null) return;

            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(
                        new Action(async () =>
                        {
                            try
                            {
                                await VerilogModuleInstance.UpdateAsync();
                                UpdateVisual();
                            }
                            catch (Exception ex)
                            {
                                CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                            }
                        })
                    );
                return;
            }
            await VerilogModuleInstance.UpdateAsync();
            UpdateVisual();
        }
        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            Data.InterfaceInstance instance = Item as Data.InterfaceInstance;
            Text = instance.Name + " - " + instance.ModuleName;


            List<CodeEditor2.Data.Item> targetDataItems = new List<CodeEditor2.Data.Item>();
            List<CodeEditor2.Data.Item> addDataItems = new List<CodeEditor2.Data.Item>();
            foreach (CodeEditor2.Data.Item item in VerilogModuleInstance.Items)
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

        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;

    }

}
