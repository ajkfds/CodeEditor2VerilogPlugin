using Avalonia.Controls;
using CodeEditor2;
using CodeEditor2.FileTypes;
using CodeEditor2.Views;
using CodeEditor2Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

            if (!CodeEditor2.Global.ProjectPropertyDeserializers.ContainsKey(Id))
            {
                CodeEditor2.Global.ProjectPropertyDeserializers.Add(Id,
                    (je, op) => { return ProjectProperty.DeserializeSetup(je, op); }
                    );
            }
            // register project property creator
            CodeEditor2.Data.Project.Created += projectCreated;

            return true;
        }

        private void projectCreated(CodeEditor2.Data.Project project)
        {
            project.ProjectProperties.Add(Id, new ProjectProperty(project));
        }


        public bool Initialize()
        {
            {
                MenuItem menuItem = CodeEditor2.Controller.Menu.Tool;
                MenuItem newMenuItem = CodeEditor2.Global.CreateMenuItem(
                    "Create Snapshot",
                    "menuItem_CreateSnapShot",
                    "CodeEditor2/Assets/Icons/play.svg",
                    Avalonia.Media.Colors.Red
                    );
                menuItem.Items.Add(newMenuItem);
                newMenuItem.Click += MenuItem_CreateSnapShot_Click;
            }
            return true;
        }
        private void MenuItem_CreateSnapShot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Global.CreateSnapShot();
        }

    }
}
