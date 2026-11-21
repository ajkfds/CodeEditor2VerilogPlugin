using Avalonia.Controls;
using CodeEditor2.LLM;
using pluginVerilog.NavigatePanel;
using System;

namespace pluginVerilog
{
    public class Plugin : CodeEditor2Plugin.IPlugin
    {
        public static string StaticID = "Verilog";
        public string Id { get { return StaticID; } }

        public static Avalonia.Media.Color ThemeColor = Avalonia.Media.Color.FromArgb(255, 255, 139, 0);

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
            {
                FileTypes.TestResultFile fileType = new FileTypes.TestResultFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }
            {
                // .rule : YAML rule set (name / severity) applied to every
                // source file below the directory that contains the file
                FileTypes.RuleFile fileType = new FileTypes.RuleFile();
                CodeEditor2.Global.FileTypes.Add(fileType.ID, fileType);
            }

            {// external libraries yaml
                CodeEditor2.Data.YamlFile.AcceptCustomYamlParsedDocument += Setups.ExternalLibrariesSetup.AcceptYamlParsedDocument;
                CodeEditor2.Parser.YamlParser.ParseCustomYaml += Setups.ExternalLibrariesSetup.ParseYaml;
            }

            if (!CodeEditor2.Global.ProjectPropertyDeserializers.ContainsKey(Id))
            {
                CodeEditor2.Global.ProjectPropertyDeserializers.Add(Id,
                    (je, op) => { return ProjectProperty.DeserializeSetup(je, op); }
                    );
            }
            // register project property creator
            CodeEditor2.Data.Project.Created += projectCreated;

            //{ // chat agent tab
            //    pluginAi.OpenRouterChat chat = new OpenRouterChat(OpenRouterModels.openai_gpt_oss_120b, false);

            //    LLMAgent agent = new LLMAgent();
            //    InitializeLLMAgent.Run(agent,false);

            //    RtlAgentControl = new ChatControl();
            //    RtlAgentControl.SetModel(chat, agent);

            //    chatTab = new TabItem()
            //    {
            //        Header = "RtlAgent",
            //        Name = "RtlAgent",
            //        FontSize = 12,
            //        Content = RtlAgentControl
            //    };
            //    CodeEditor2.Controller.Tabs.AddItem(chatTab);
            //}


            return true;
        }
        internal static Avalonia.Controls.TabItem? chatTab;
        public static ChatControl? RtlAgentControl;

        public bool Initialize()
        {
            // Tool Menu
            {
                MenuItem menuItem = CodeEditor2.Controller.Menu.Tool;
                MenuItem verilogToolMenuItem = CodeEditor2.Global.CreateMenuItem(
                    "Verilog",
                    "menuItem_verilogToolMenuItem",
                    "CodeEditor2VerilogPlugin/Assets/Icons/verilogDocument.svg",
                    ThemeColor
                    );
                menuItem.Items.Add(verilogToolMenuItem);


                {
                    MenuItem newMenuItem = CodeEditor2.Global.CreateMenuItem(
                        "StopParser",
                        "menuItem_StopParser",
                        "CodeEditor2/Assets/Icons/play.svg",
                        ThemeColor
                        );
                    menuItem.Items.Add(newMenuItem);
                    newMenuItem.Click += MenuItem_StopParse_Click;
                }

            }


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

            // context menu
            //
            // The navigate-node menu is driven by one catalog shared with VS Code
            // (CoreBridge.HierarchyMenuCatalog), so this installs the renderer
            // instead of subscribing to the per-node-type
            // CustomizeNavigateNodeContextMenu event. Rows that only the editor
            // can run declare Hosts.Desktop; VS Code simply never sees them.
            RegisterNavigateNodeMenuEntries();
            CodeEditor2.NavigatePanel.NavigatePanelNode.CreateContextMenuRows = CustomizeNavigateNodeContextMenuRows;
            return true;
        }

        /// <summary>
        /// Draws the navigate-node menu from the shared catalog. Registered once
        /// in <see cref="Initialize"/>; see
        /// <see cref="NavigatePanel.NavigateNodeMenuRenderer"/>.
        /// </summary>
        public static void CustomizeNavigateNodeContextMenuRows(
            Avalonia.Controls.ContextMenu contextMenu,
            CodeEditor2.NavigatePanel.NavigatePanelNode node)
        {
            NavigatePanel.NavigateNodeMenuRenderer.Render(contextMenu, node);
        }

        /// <summary>
        /// Registers "Parse All" as a catalog row instead of adding it to the
        /// context menu directly, so the same declaration feeds the desktop menu
        /// and (when it makes sense) any other host.
        /// </summary>
        private static void RegisterNavigateNodeMenuEntries()
        {
            CoreBridge.HierarchyMenuCatalog.Register(new CoreBridge.HierarchyMenuCatalog.Entry
            {
                Id = "parseAll",
                Label = "Parse All",
                Detail = "re-parse every file in the hierarchy",
                Targets = CoreBridge.HierarchyMenuCatalog.AppliesTo.File
                        | CoreBridge.HierarchyMenuCatalog.AppliesTo.Instance,
                Hosts = CoreBridge.HierarchyMenuCatalog.Hosts.Desktop,
                Group = 30,
                Action = CoreBridge.HierarchyMenuCatalog.ActionKind.DesktopHandler,
                IconPath = "CodeEditor2/Assets/Icons/flame.svg",
                IconColor = PackColor(ThemeColor),
                Handler = context => MenuItem_ParseHier(context),
            });
        }

        /// <summary>Packs an Avalonia colour into the catalog's 0xAARRGGBB form.</summary>
        private static uint PackColor(Avalonia.Media.Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }

        private static void MenuItem_ParseHier(CoreBridge.NavigateNodeMenuContext context)
        {
            try
            {
                // The hierarchy walk takes a source file; an instance node's
                // parse result is an InstanceTextFile, so unwrap it.
                Data.VerilogFile? vfile = context.File as Data.VerilogFile;
                if (vfile == null) return;

                Tool.ParseHierarchy.PostParseAsync(vfile, Tool.ParseHierarchy.ParseMode.ForceAllFiles);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
        }

        private static void MenuItem_ParseHier_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                Data.VerilogFile? vfile = (CodeEditor2.Controller.NavigatePanel.GetSelectedNode() as VerilogFileNode)?.VerilogFile;
                if (vfile == null) return;

                Tool.ParseHierarchy.PostParseAsync(vfile, Tool.ParseHierarchy.ParseMode.ForceAllFiles);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
        }


        internal static bool StopParse = false;
        private static void MenuItem_StopParse_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            StopParse = !StopParse;
        }

        private void projectCreated(CodeEditor2.Data.Project project, CodeEditor2.Data.Project.Setup? setup)
        {
            pluginVerilog.ProjectProperty.Setup? psetup = null;
            if (setup != null && setup.ProjectProperties.ContainsKey(Id))
            {
                psetup = setup?.ProjectProperties[Id] as pluginVerilog.ProjectProperty.Setup;
            }
            if (psetup == null) psetup = new pluginVerilog.ProjectProperty.Setup();
            project.ProjectProperties.Add(Id, new ProjectProperty(project, psetup));
        }

        private void MenuItem_CreateSnapShot_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Global.CreateSnapShot();
        }

    }
}
