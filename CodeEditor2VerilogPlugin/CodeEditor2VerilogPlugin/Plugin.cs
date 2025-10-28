﻿using Avalonia.Controls;
using CodeEditor2;
using CodeEditor2.FileTypes;
using CodeEditor2.Views;
using CodeEditor2Plugin;
using pluginVerilog.NavigatePanel;
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

        public static Avalonia.Media.Color ThemeColor = Avalonia.Media.Color.FromArgb(255, 50, 150,150);

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

        public bool Initialize()
        {
            // Menu
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

            pluginVerilog.NavigatePanel.VerilogFileNode.CustomizeNavigateNodeContextMenu += CustomizeNavigateNodeContextMenuHandler;
            return true;
        }

        public static void CustomizeNavigateNodeContextMenuHandler(Avalonia.Controls.ContextMenu contextMenu)
        {
            MenuItem menuItem_ParseHier = CodeEditor2.Global.CreateMenuItem(
                "Parse All", "menuItem_ParseHier",
                "CodeEditor2/Assets/Icons/flame.svg",
                ThemeColor
                );
            contextMenu.Items.Add(menuItem_ParseHier);
            menuItem_ParseHier.Click += MenuItem_ParseHier_Click;
        }

        private static async void MenuItem_ParseHier_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Data.VerilogFile? vfile = (CodeEditor2.Controller.NavigatePanel.GetSelectedNode() as VerilogFileNode)?.VerilogFile;
            if (vfile == null) return;

            await Tool.ParseHierarchy.ParseAsync(vfile, Tool.ParseHierarchy.ParseMode.ForceAllFiles);
        }

        private void projectCreated(CodeEditor2.Data.Project project, CodeEditor2.Data.Project.Setup? setup)
        {
            pluginVerilog.ProjectProperty.Setup? psetup = setup?.ProjectProperties[Id] as pluginVerilog.ProjectProperty.Setup;
            if (psetup == null) psetup = new pluginVerilog.ProjectProperty.Setup();
            project.ProjectProperties.Add(Id, new ProjectProperty(project, psetup));
        }

        private void MenuItem_CreateSnapShot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Global.CreateSnapShot();
        }

    }
}
