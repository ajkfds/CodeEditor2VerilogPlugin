using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
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

        public override void OnDeSelected()
        {
            if (ModuleInstance?.SourceTextFile == null) return;
            if (ModuleInstance?.ParsedDocument?.Version != null && ModuleInstance?.ParsedDocument?.Version == ModuleInstance?.CodeDocument?.Version) return;

            // 未parseのものが残っている場合はbackgroundでparseしておく
            ModuleInstance?.SourceTextFile.PostParse();
        }


        private volatile bool onSelecting = false;

        #pragma warning disable VSTHRD100 // 理由: UIイベントの起点であり、内部で完全にtry-catchしているため安全
        public override async void OnSelected()
        {
            if (onSelecting) return;
            onSelecting = true;
            try
            {
                base.OnSelected(); // update context enu

                if (ModuleInstance == null)
                {
                    await UpdateAsync();
                    return;
                }

                await CodeEditor2.Controller.CodeEditor.SetTextFileAsync(ModuleInstance, true);
                UpdateVisual();

                if (CodeEditor2.Global.StopParse) return;

                // post hier parse on background
                Tool.ParseHierarchy.PostParseAsync(ModuleInstance, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                onSelecting = false;
            }
        }

        public override async Task UpdateAsync()
        {
            if (VerilogModuleInstance == null) return;
            if (VerilogFile == null)
            {
                return;
            }


            if (!Dispatcher.UIThread.CheckAccess())
            {
                #pragma warning disable VSTHRD101 //try-catched
                Dispatcher.UIThread.Post(
                        new Action(async () =>
                        {
                            try
                            {
                                await VerilogModuleInstance.UpdateAsync(); // UpdateVisual called in this method on the  UI thread
                            }
                            catch (Exception ex)
                            {
                                CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                            }
                        })
                    );
                return;
            }
            await VerilogModuleInstance.UpdateAsync(); // UpdateVisual called in this method on the  UI thread
            return;
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
                foreach (CodeEditor2.Data.Item item in VerilogModuleInstance.Items)
                {
                    newNodes.Add(item.NavigatePanelNode);
                }
            }

            System.Collections.ObjectModel.ObservableCollection<AjkAvaloniaLibs.Controls.TreeControls.TreeNode> nodes = new System.Collections.ObjectModel.ObservableCollection<AjkAvaloniaLibs.Controls.TreeControls.TreeNode>();
            foreach (CodeEditor2.NavigatePanel.NavigatePanelNode node in newNodes)
            {
                nodes.Add(node);
            }

            Nodes = nodes;
        }
        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            if (Item == null)
            {
                Text = "null";
            }
            else
            {
                Data.VerilogModuleInstance instance = (Data.VerilogModuleInstance)Item;
                if (instance.NameSpaceString != "")
                {
                    Text = instance.NameSpaceString+"."+ instance.Name + " - " + instance.ModuleName;
                }
                else
                {
                    Text = instance.Name + " - " + instance.ModuleName;
                }
            }

            if (VerilogFile == null) return;

            if (VerilogModuleInstance != null) Image = VerilogFileNode.GetIcon(VerilogModuleInstance);

            UpdateSubNodes();
        }
        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;


    }

}
