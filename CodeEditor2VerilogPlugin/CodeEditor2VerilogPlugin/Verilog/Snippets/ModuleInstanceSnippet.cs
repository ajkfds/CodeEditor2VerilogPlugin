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

namespace pluginVerilog.Verilog.Snippets
{
    public class ModuleInstanceSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModuleInstanceSnippet() : base("moduleInstance")
        {
        }

        private CodeDocument document;

        // initial value for {n}
        private List<string> initials = new List<string> { };

        public override void Apply(CodeDocument codeDocument)
        {
            base.Apply(codeDocument);

            List<ToolItem> items = new List<ToolItem>();

            CodeEditor2.Data.Project project = codeDocument.TextFile.Project;
            ProjectProperty projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;

            List<string> moduleNmaes = projectProperty.GetModuleNameList();
            foreach (string moduleName in moduleNmaes)
            {
                items.Add(new ModuleInstanceSelectionItem(moduleName, this));
            }

            CodeEditor2.Controller.CodeEditor.ForceOpenCustomSelection(items);
        }

        public class ModuleInstanceSelectionItem : CodeEditor2.CodeEditor.ToolItem
        {
            public ModuleInstanceSelectionItem(string text, ModuleInstanceSnippet moduleInstanceSnippet) : base(text)
            {
                snippet = moduleInstanceSnippet;
                Foreground = new SolidColorBrush(Global.CodeDrawStyle.GetColor(CodeDrawStyle.ColorType.Keyword));
            }

            private ModuleInstanceSnippet snippet;
            public override void Apply(CodeDocument codeDocument)
            {
                snippet.ApplyModuleInstance(codeDocument, Text);
            }
        }

        public void ApplyModuleInstance(CodeDocument codeDocument, string Text)
        {
            CodeEditor2.Data.Project project = codeDocument.TextFile.Project;

            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return;

            Data.VerilogFile? targetFile = projectProperty.GetFileOfBuildingBlock(Text) as Data.VerilogFile;
            if (targetFile == null) return;

            string instanceName = Text + "_";
            {
                Data.IVerilogRelatedFile? vfile = CodeEditor2.Controller.CodeEditor.GetTextFile() as Data.IVerilogRelatedFile;
                if (vfile == null) return;

                ParsedDocument parentParsedDocument = vfile.VerilogParsedDocument;
                BuildingBlock? module = parentParsedDocument.GetBuildingBlockAt(vfile.CodeDocument.CaretIndex);
                if (module == null) return;

                int instanceCount = 0;
                while (module.Instantiations.ContainsKey(Text + "_" + instanceCount.ToString()))
                {
                    instanceCount++;
                }
                instanceName = Text + "_" + instanceCount.ToString();
            }

            Verilog.ParsedDocument? targetParsedDocument = targetFile.ParsedDocument as Verilog.ParsedDocument;
            if (targetParsedDocument == null) return;

            Module? targetModule = targetParsedDocument.Root.BuldingBlocks[Text] as Module;
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
                    startIndexs.Add(index + replaceText.IndexOf(target));
                    lastIndexs.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
                    replaceText = replaceText.Replace(target, initials[i]);
                }

                codeDocument.Replace(index, 0, 0, replaceText);
                codeDocument.CaretIndex = startIndexs[0];
                codeDocument.SetSelection( startIndexs[0],lastIndexs[0] + 1);

                CodeEditor2.Controller.CodeEditor.ClearHighlight();
                for (int i = 0; i < startIndexs.Count; i++)
                {
                    CodeEditor2.Controller.CodeEditor.AppendHighlight(startIndexs[i], lastIndexs[i]);
                }
            }

            base.Apply(codeDocument);
        }


        public override void Aborted()
        {
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            document = null;
            base.Aborted();
        }

        private List<int> startIndexs = new List<int>();
        private List<int> lastIndexs = new List<int>();

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

        public override void AfterKeyDown(object sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {

        }

        public override void AfterAutoCompleteHandled(CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
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
            if (document == null) return;
            int i = CodeEditor2.Controller.CodeEditor.GetHighlightIndex(document.CaretIndex);
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
