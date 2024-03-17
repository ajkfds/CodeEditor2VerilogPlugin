using CodeEditor2.FileTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class Plugin : CodeEditor2Plugin.IPlugin
    {
        public static string StaticID = "Verilog";
        public string Id { get { return StaticID; } }

        public bool Register()
        {
            // register filetypes
            {
                FileTypes.VerilogFile fileType = new FileTypes.VerilogFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }
            {
                FileTypes.VerilogHeaderFile fileType = new FileTypes.VerilogHeaderFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }
            {
                FileTypes.SystemVerilogFile fileType = new FileTypes.SystemVerilogFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }
            {
                FileTypes.SystemVerilogHeaderFile fileType = new FileTypes.SystemVerilogHeaderFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }

            // append navigate context menu items
            //System.Windows.Forms.ContextMenuStrip menu = CodeEditor.Controller.NavigatePanel.GetContextMenuStrip();
            //menu.Items.Insert(0,Global.SetupForm.IcarusVerilogTsmi);
            //menu.Items.Insert(0, Global.SetupForm.VerilogDebugTsmi);

            //foreach (var menuItem in menu.Items)
            //{
            //    if(menuItem is System.Windows.Forms.ToolStripMenuItem)
            //    {
            //        var tsmi = menuItem as System.Windows.Forms.ToolStripMenuItem;
            //        if(tsmi.Text == "Add")
            //        {
            //            tsmi.DropDownItems.Add(Global.SetupForm.CreateVerilogFileTsmi);
            //        }
            //    }
            //}

            // register project property creator
            //CodeEditor2.Data.Project.Created += projectCreated;

            return true;
        }

        private void projectCreated(CodeEditor2.Data.Project project)
        {
            project.ProjectProperties.Add(Id, new ProjectProperty(project));
        }

        public bool Initialize()
        {
            // register project property form tab
//            CodeEditor.Tools.ProjectPropertyForm.FormCreated += Tools.ProjectPropertyTab.ProjectPropertyFromCreated;

            return true;
        }
    }
}
