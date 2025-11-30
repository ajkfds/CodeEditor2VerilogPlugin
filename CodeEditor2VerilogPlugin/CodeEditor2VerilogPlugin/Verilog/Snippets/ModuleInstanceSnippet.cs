using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using System.Drawing;
using pluginVerilog.Verilog.BuildingBlocks;
using Avalonia.Input;
using CodeEditor2.Views;
using Avalonia.Media;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;

namespace pluginVerilog.Verilog.Snippets
{
    public class ModuleInstanceSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModuleInstanceSnippet(string moduleName) : base(moduleName)
        {
            this.moduleName = moduleName;
            IconImage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/module.svg",
                    Plugin.ThemeColor
                    );
        }


        // initial value for {n}
        private List<string> initials = new List<string> { };

        private CodeDocument codeDocument;
        private CodeEditor2.Data.TextFile textFile;
        private string moduleName;

        public override void Apply()
        {
            CodeEditor2.Data.TextFile? textFile = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (textFile == null) return;
            codeDocument = textFile.CodeDocument;

            CodeEditor2.Data.Project project = textFile.Project;

            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return;

            Data.VerilogFile? targetFile = projectProperty.GetFileOfBuildingBlock(moduleName) as Data.VerilogFile;
            if (targetFile == null) return;

            string instanceName = moduleName + "_";
            {
                Data.IVerilogRelatedFile? vFile = CodeEditor2.Controller.CodeEditor.GetTextFile() as Data.IVerilogRelatedFile;
                if (vFile == null) return;

                ParsedDocument? parentParsedDocument = vFile.VerilogParsedDocument;
                if(parentParsedDocument == null) return;
                BuildingBlock? module = parentParsedDocument.GetBuildingBlockAt(vFile.CodeDocument.CaretIndex);
                if (module == null) return;

                int instanceCount = 0;
                while (module.NamedElements.ContainsIBuldingBlockInstantiation(moduleName + "_" + instanceCount.ToString()))
                {
                    instanceCount++;
                }
                instanceName = moduleName + "_" + instanceCount.ToString();
            }

            Verilog.ParsedDocument? targetParsedDocument = targetFile.ParsedDocument as Verilog.ParsedDocument;
            if (targetParsedDocument == null) return;

            Module? targetModule = targetParsedDocument.Root.BuildingBlocks[moduleName] as Module;
            if (targetModule == null) return;

            string replaceText = getReplaceText(targetModule, instanceName);


            int index = codeDocument.CaretIndex;

            if (initials.Count == 0)
            {
                codeDocument.Replace(index, 0, 0, replaceText);
                CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
            }
            else
            {
                for (int i = 0; i < initials.Count; i++)
                {
                    string target = "{" + i.ToString() + "}";
                    if (!replaceText.Contains(target)) break;
                    startIndexes.Add(index + replaceText.IndexOf(target));
                    lastIndexes.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
                    replaceText = replaceText.Replace(target, initials[i]);
                }

                codeDocument.Replace(index, 0, 0, replaceText);
                CodeEditor2.Controller.CodeEditor.SetCaretPosition(startIndexes[0]);
                CodeEditor2.Controller.CodeEditor.SetSelection(startIndexes[0], lastIndexes[0] + 1);

                CodeEditor2.Controller.CodeEditor.ClearHighlight();
                for (int i = 0; i < startIndexes.Count; i++)
                {
                    CodeEditor2.Controller.CodeEditor.AppendHighlight(startIndexes[i], lastIndexes[i]);
                }
            }

            base.Apply();
        }




        public override void Aborted()
        {
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            codeDocument = null;
            textFile = null;
            base.Aborted();
        }

        private List<int> startIndexes = new List<int>();
        private List<int> lastIndexes = new List<int>();

        public override void KeyDown(object? sender, KeyEventArgs e, PopupMenuView popupMenuView)
        {
            // overrider return & escape
            if (!CodeEditor2.Controller.CodeEditor.IsPopupMenuOpened)
            {
                if (e.Key == Key.Return || e.Key == Key.Escape)
                {
                    bool moved;
                    moveToNextHighlight(out moved);
                    if (!moved) CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    e.Handled = true;
                }
            }
        }

        public override void AfterKeyDown(object? sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {

        }

        public override void AfterAutoCompleteHandled(CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            if (codeDocument == null) return;
            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(codeDocument.CaretIndex);
            switch (i)
            {
                default:
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    break;
            }
    }

        private void moveToNextHighlight(out bool moved)
        {
            moved = false;
            if (codeDocument == null) return;
            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(codeDocument.CaretIndex);
            if (i == -1) return;
            i++;
            if (i >= initials.Count) return;

            CodeEditor2.Controller.CodeEditor.SelectHighlight(i);
            moved = true;
        }

        private void ApplyTool(object sender, EventArgs e)
        {

        }


        private string getReplaceText(Module module, string instanceName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(module.Name);
            sb.Append(" ");
            if (module.PortParameterNameList.Count > 0)
            {
                sb.Append("#(\r\n");
                bool first = true;
                foreach (string portName in module.PortParameterNameList)
                {
                    if (!first) sb.Append(",\r\n");
                    sb.Append("\t");
                    sb.Append(".");
                    sb.Append(portName);
                    sb.Append("\t( ");
                    sb.Append(" )");
                    first = false;
                }
                sb.Append("\r\n) ");
            }

            sb.Append("{0}");
            initials.Add(instanceName);

            sb.Append(" (\r\n");
            int j = 0;
            foreach (Verilog.DataObjects.Port port in module.Ports.Values)
            {
                sb.Append("\t.");
                sb.Append(port.Name);
                sb.Append("\t(  )");
                if (j != module.Ports.Count - 1) sb.Append(",");
                sb.Append("\r\n");
                j++;
            }
            sb.Append(");");

            return sb.ToString();
        }

    }
}
