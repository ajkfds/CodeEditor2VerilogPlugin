using Avalonia.Input;
using CodeEditor2.Views;
using pluginVerilog.Verilog.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace pluginVerilog.Verilog.Snippets
{
    public class ModPortSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModPortSnippet() : base("modport generate")
        {
        }

        private CodeEditor2.CodeEditor.CodeDocument document;

        // initial value for {n}
        private List<string> initials = new List<string> { "modportname" };


        public override void Apply()
        {
            CodeEditor2.Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            document = file.CodeDocument;

            CodeEditor2.Data.Project project = file.Project;

            Data.VerilogFile? vFile = file as Data.VerilogFile;
            if (vFile == null) return;

            Verilog.ParsedDocument? parsedDocument = vFile.VerilogParsedDocument;
            if (parsedDocument == null) return;

            Verilog.BuildingBlocks.BuildingBlock? buildingBlock = parsedDocument.GetBuildingBlockAt(document.CaretIndex);
            if (buildingBlock == null) return;

            Verilog.BuildingBlocks.Interface? interface_ = buildingBlock as Verilog.BuildingBlocks.Interface;
            if (interface_ == null) return;

            string indent = "";
            if (document.GetCharAt(document.GetLineStartIndex(document.GetLineAt(document.CaretIndex))) == '\t')
            {
                indent = "\t";
            }

            Dictionary<string, ModPort.Port.DirectionEnum> newModPortDirection = new Dictionary<string, ModPort.Port.DirectionEnum>();
            foreach (var dataObject in interface_.NamedElements.Values)
            {
                if (dataObject is Verilog.DataObjects.Variables.Logic)
                {
                    newModPortDirection.Add(dataObject.Name, ModPort.Port.DirectionEnum.Output);
                }
            }

            foreach(ModPort modport in interface_.NamedElements.Values.OfType<ModPort>())
            {
                foreach(var port in modport.Ports.Values)
                {
                    string expName = port.Expression.CreateString();

                    if (!newModPortDirection.ContainsKey(expName)) continue;
                    if(
                        port.Direction == ModPort.Port.DirectionEnum.Output ||
                        port.Direction == ModPort.Port.DirectionEnum.Inout
                        )
                    {
                        newModPortDirection[expName] = ModPort.Port.DirectionEnum.Input;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(indent+ "modport {0} (\n");

            bool firstPort = true;

            foreach(var nameDirection in newModPortDirection)
            {
                if (!firstPort) sb.Append(",\n");

                // modport definition
                switch(nameDirection.Value)
                {
                    case ModPort.Port.DirectionEnum.Input:
                        sb.Append("input\t." + nameDirection.Key);
                        break;
                    case ModPort.Port.DirectionEnum.Output:
                        sb.Append("output\t." + nameDirection.Key);
                        break;
                    case ModPort.Port.DirectionEnum.Inout:
                        sb.Append("inout\t." + nameDirection.Key);
                        break;
                }

                // port name
                sb.Append("\t(" + nameDirection.Key + ")");
                firstPort = false;
            }

            if (!firstPort) sb.Append("\n");
            sb.Append(")\n");

            string replaceText = sb.ToString();


            int index = document.CaretIndex;

            for (int i = 0; i < initials.Count; i++)
            {
                string target = "{" + i.ToString() + "}";
                if (!replaceText.Contains(target)) break;
                startIndexes.Add(index + replaceText.IndexOf(target));
                lastIndexes.Add(index + replaceText.IndexOf(target) + initials[i].Length - 1);
                replaceText = replaceText.Replace(target, initials[i]);
            }

            document.Replace(index, 0, 0, replaceText);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(startIndexes[0]);
            CodeEditor2.Controller.CodeEditor.SetSelection(startIndexes[0], lastIndexes[0]);

            // set highlights for {n} texts
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            for (int i = 0; i < startIndexes.Count; i++)
            {
                CodeEditor2.Controller.CodeEditor.AppendHighlight(startIndexes[i], lastIndexes[i]);
            }

            base.Apply();
        }




        public override void Aborted()
        {
            CodeEditor2.Controller.CodeEditor.ClearHighlight();
            document = null;
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
                    CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
                    e.Handled = true;
                }
            }
        }
        public override void BeforeKeyDown(object sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {
        }
        public override void AfterKeyDown(object sender, TextInputEventArgs e, CodeEditor2.Views.PopupMenuView popupMenuView)
        {

        }
        public override void AfterAutoCompleteHandled(CodeEditor2.Views.PopupMenuView popupMenuView)
        {
            CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        }

    }
}
