using CodeEditor2.CodeEditor.PopupMenu;
using System;
using System.Collections.Generic;


namespace pluginVerilog.Verilog.Snippets
{
    public class ModuleInstanceMenuSnippet : ToolItem
    {
        public ModuleInstanceMenuSnippet() : base("moduleInstance")
        {
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }


        public override async System.Threading.Tasks.Task ApplyAsync()
        {
            CodeEditor2.Data.TextFile? textFile = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
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
