using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Input;
using CodeEditor2.Views;
using Avalonia.Media;
using static System.Net.Mime.MediaTypeNames;
using static pluginVerilog.Verilog.Snippets.ModuleInstanceSnippet;
using System.Reflection.Metadata;
using CodeEditor2.CodeEditor.PopupMenu;


namespace pluginVerilog.Verilog.Snippets
{
    public class ModuleInstanceMenuSnippet : ToolItem
    {
        public ModuleInstanceMenuSnippet() : base("moduleInstance")
        {
        }


        public override void Apply()
        {
            CodeEditor2.Data.TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (textFile == null) return;

            List<ToolItem> items = new List<ToolItem>();

            CodeEditor2.Data.Project project = textFile.Project;
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();

            List<string> moduleNames = projectProperty.GetModuleNameList();
            foreach (string moduleName in moduleNames)
            {
                items.Add(new ModuleInstanceSnippet(moduleName));
            }

            CodeEditor2.Controller.CodeEditor.ForceOpenCustomSelection(items);
        }
    }
}
