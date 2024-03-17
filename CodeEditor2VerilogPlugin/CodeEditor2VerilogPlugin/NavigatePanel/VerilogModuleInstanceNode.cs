using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.NavigatePanel
{
    public class VerilogModuleInstanceNode : CodeEditor2.NavigatePanel.FileNode, IVerilogNavigateNode
    {
        public VerilogModuleInstanceNode(Data.VerilogModuleInstance verilogModuleInstance) : base(verilogModuleInstance)
        {
            moduleInstanceRef = new WeakReference<Data.VerilogModuleInstance>(verilogModuleInstance);
        }

        private System.WeakReference<Data.VerilogModuleInstance> moduleInstanceRef;
        public Data.VerilogModuleInstance ModuleInstance
        {
            get
            {
                Data.VerilogModuleInstance ret;
                if (!moduleInstanceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public override CodeEditor2.Data.File FileItem
        {
            get
            {
                Data.VerilogModuleInstance instance = ModuleInstance;
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

        //    // error mark
        //    if( VerilogModuleInstance != null && VerilogModuleInstance.VerilogParsedDocument != null && VerilogModuleInstance.VerilogParsedDocument.ErrorCount != 0)
        //    {
        //        graphics.DrawImage(Global.Icons.Exclamation.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
        //    }

        //    // dirty mark
        //    if (VerilogFile != null && VerilogFile.Dirty)
        //    {
        //        graphics.DrawImage(Global.Icons.NewBadge.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
        //    }
        //}

        public override void OnSelected()
        {
            //var menu = CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip();
            //if (menu.Items.ContainsKey("openWithExploererTsmi")) menu.Items["openWithExploererTsmi"].Visible = true;

            CodeEditor2.Controller.CodeEditor.SetTextFile(ModuleInstance);

            if (!ModuleInstance.ParseValid || ModuleInstance.ReparseRequested)
            {
                if (!CodeEditor2.Global.StopParse)
                {
                    CodeEditor2.Tools.ParseHierarchyForm pform = new CodeEditor2.Tools.ParseHierarchyForm(this);
                    CodeEditor2.Controller.ShowDialogForm(pform);
                }
            }

            // TODO
            //Module targetModule = null;
            //foreach (Module module in ModuleInstance.VerilogParsedDocument.Modules.Values)
            //{
            //    if(module.Name != ModuleInstance.ModuleName)
            //    {
            //        ModuleInstance.CodeDocument.CollapseBlock(ModuleInstance.CodeDocument.GetLineAt(module.BeginIndex));
            //    }
            //    else
            //    {
            //        ModuleInstance.CodeDocument.ExpandBlock(ModuleInstance.CodeDocument.GetLineAt(module.BeginIndex));
            //        targetModule = module;
            //    }
            //}

            //if(targetModule != null)
            //{
            //    if (
            //        ModuleInstance.CodeDocument.SelectionStart < targetModule.BeginIndex &&
            //        ModuleInstance.CodeDocument.SelectionLast < targetModule.BeginIndex
            //        )
            //    {
            //        ModuleInstance.CodeDocument.SelectionStart = targetModule.BeginIndex;
            //        ModuleInstance.CodeDocument.SelectionLast = targetModule.BeginIndex;
            //        ModuleInstance.CodeDocument.CaretIndex = targetModule.BeginIndex;
            //        CodeEditor2.Controller.CodeEditor.ScrollToCaret();
            //    }

            //    if (
            //        targetModule.LastIndex < ModuleInstance.CodeDocument.SelectionStart &&
            //        targetModule.LastIndex < ModuleInstance.CodeDocument.SelectionLast
            //        )
            //    {
            //        ModuleInstance.CodeDocument.SelectionStart = targetModule.BeginIndex;
            //        ModuleInstance.CodeDocument.SelectionLast = targetModule.BeginIndex;
            //        ModuleInstance.CodeDocument.CaretIndex = targetModule.BeginIndex;
            //        CodeEditor2.Controller.CodeEditor.ScrollToCaret();
            //    }
            //}

            //targetModule.
        }

        public override void Update()
        {
            if (VerilogModuleInstance == null) return;
            VerilogModuleInstance.Update();

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


        }

    }

}
