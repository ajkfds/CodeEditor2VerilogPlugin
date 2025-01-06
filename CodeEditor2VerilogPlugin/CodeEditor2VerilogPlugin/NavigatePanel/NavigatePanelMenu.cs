using AjkAvaloniaLibs.Controls;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using CodeEditor2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.NavigatePanel
{
    public static class NavigatePanelMenu
    {
        public static void Register()
        {
            MenuItem? menuItem_Add = CodeEditor2.Controller.NavigatePanel.GetContextMenuItem(new List<string> { "Add" });
            if (menuItem_Add == null) return;

            {
                MenuItem menuItem_AddFile = CodeEditor2.Global.CreateMenuItem("verilog module", "MenuItem_AddVerilogModule",
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                    );
                menuItem_AddFile.Click += menuItem_AddVerilogModule_Click;
                menuItem_Add.Items.Add(menuItem_AddFile);
            }

            {
                MenuItem menuItem_AddFile = CodeEditor2.Global.CreateMenuItem("SystemVerilog module", "MenuItem_AddSystemVerilogModule",
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                    );
                menuItem_AddFile.Click += menuItem_AddSystemVerilogModule_Click;
                menuItem_Add.Items.Add(menuItem_AddFile);
            }

            {
                MenuItem menuItem_AddFile = CodeEditor2.Global.CreateMenuItem("SystemVerilog interface", "MenuItem_AddSystemVerilogInterface",
                    "CodeEditor2VerilogPlugin/Assets/Icons/systemVerilogDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 240, 240)
                    );
                menuItem_AddFile.Click += menuItem_AddSystemVerilogInterface_Click;
                menuItem_Add.Items.Add(menuItem_AddFile);
            }
        }

        private static async Task generateFile(
            string typeName,
            string extension,
            Action<System.IO.StreamWriter,string> streamWriter
            )
        {
            NavigatePanelNode? node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return;

            Project project = CodeEditor2.Controller.NavigatePanel.GetProject(node);

            string relativePath = getRelativeFolderPath(node);
            if (!relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) relativePath += System.IO.Path.DirectorySeparatorChar;

            CodeEditor2.Tools.InputWindow window = new CodeEditor2.Tools.InputWindow("Create new "+typeName, "new "+typeName+" name");
            await window.ShowDialog(Controller.GetMainWindow());

            if (window.Cancel) return;
            string name = window.InputText.Trim();

            // duplicate check
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as pluginVerilog.ProjectProperty;
            if (projectProperty == null) return;
            BuildingBlock? buildingBlock = projectProperty.GetBuildingBlock(name);
            if (buildingBlock != null)
            {
                CodeEditor2.Controller.AppendLog("Duplicate BuildingBlock Name ;" + name);
                return;
            }

            // create file
            string fileName = name + "."+ extension;
            string path = project.GetAbsolutePath(relativePath + fileName);
            if (System.IO.File.Exists(path))
            {
                CodeEditor2.Controller.AppendLog("! already exist " + path, Avalonia.Media.Colors.Red);
            }
            else
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                {
                    streamWriter(sw, name);
                }
            }

            CodeEditor2.Controller.NavigatePanel.UpdateFolder(node);
        }

        private static async void menuItem_AddVerilogModule_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await generateFile("verilog module", "v",
                (sw,name) => {
                    sw.Write("`timescale 1ns / 1ps\n");
                    sw.Write("\n");
                    sw.Write("module " + name + ";\n");
                    sw.Write("\n");
                    sw.Write("endmodule\n");
                }
            );
        }
        private static async void menuItem_AddSystemVerilogModule_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await generateFile("system verilog module", "sv",
                (sw, name) => {
                    sw.Write("`timescale 1ns / 1ps\n");
                    sw.Write("\n");
                    sw.Write("module " + name + ";\n");
                    sw.Write("\n");
                    sw.Write("endmodule\n");
                }
            );
        }

        private static async void menuItem_AddSystemVerilogInterface_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await generateFile("system verilog interface", "sv",
                (sw, name) => {
                    sw.Write("interface " + name + ";\n");
                    sw.Write("\n");
                    sw.Write("endinterface\n");
                }
            );
        }
        private static string getRelativeFolderPath(NavigatePanelNode node)
        {
            FileNode? fileNode = node as FileNode;
            if (fileNode != null)
            {
                NavigatePanelNode? parentNode = fileNode.Parent as NavigatePanelNode;
                if (parentNode == null) throw new System.Exception();
                return getRelativeFolderPath(parentNode);
            }

            FolderNode? folderNode = node as FolderNode;
            if (folderNode != null)
            {
                return folderNode.Folder.RelativePath;
            }
            return "";
        }


    }
}
