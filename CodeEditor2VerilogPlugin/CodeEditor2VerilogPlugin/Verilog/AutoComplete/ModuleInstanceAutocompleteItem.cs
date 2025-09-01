using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class ModuleInstanceAutocompleteItem : AutocompleteItem
    {
        public ModuleInstanceAutocompleteItem(string text, byte colorIndex, Color color, CodeEditor2.Data.Project project) : base(text, colorIndex, color)
        {
            this.project = project;
        }
        CodeEditor2.Data.Project project;

        public override void Apply()
        {
            if (codeDocument == null) return;
            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);

            char currentChar = codeDocument.GetCharAt(codeDocument.CaretIndex);
            if (currentChar != '\r' && currentChar != '\n') return;

            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return;

            CodeEditor2.Data.ITextFile iText = CodeEditor2.Controller.CodeEditor.GetTextFile();

            if (!(iText is Data.IVerilogRelatedFile)) return;
            var vFile = iText as Data.IVerilogRelatedFile;
            if (vFile == null) return;
            ParsedDocument? parsedDocument = vFile.VerilogParsedDocument;
            if (parsedDocument == null) return;

            BuildingBlock? module = parsedDocument.GetBuildingBlockAt(vFile.CodeDocument.GetLineStartIndex(vFile.CodeDocument.GetLineAt(vFile.CodeDocument.CaretIndex)));
            if (module == null) return;

            Data.VerilogFile? instancedFile = projectProperty.GetFileOfBuildingBlock(Text) as Data.VerilogFile;
            if (instancedFile == null) return;
            Verilog.ParsedDocument? instancedParsedDocument = instancedFile.ParsedDocument as Verilog.ParsedDocument;
            if (instancedParsedDocument == null) return;
            Module? instancedModule = instancedParsedDocument.Root.BuildingBlocks[Text] as Module;
            if (instancedModule == null) return;

            string instanceName;
            int i = 0;
            while (true)
            {
                instanceName = Text + "_" + i.ToString();
                if (!module.NamedElements.ContainsIBuldingBlockInstantiation(instanceName)) break;
                i++;
            }

            // create code
            StringBuilder sb = new StringBuilder();

            // module name
            sb.Append(" ");

            // parameters
            if (instancedModule.PortParameterNameList.Count > 0)
            {
                sb.Append("#(\r\n");
                bool first = true;
                foreach (string portName in instancedModule.PortParameterNameList)
                {
                    if (!first) sb.Append(",\r\n");
                    sb.Append("\t");
                    sb.Append(".");
                    sb.Append(portName);
                    sb.Append("\t(  )");
                    first = false;
                }
                sb.Append("\r\n) ");
            }

            int carletOffset = Text.Length + sb.Length;
            sb.Append(instanceName);
            sb.Append(" (\r\n");

            // ports
            i = 0;
            foreach (Verilog.DataObjects.Port port in instancedModule.Ports.Values)
            {
                sb.Append("\t.");
                sb.Append(port.Name);
                sb.Append("\t(  )");
                if (i != instancedModule.Ports.Count - 1) sb.Append(",");
                sb.Append("\r\n");
                i++;
            }
            sb.Append(");");

            codeDocument.Replace(headIndex, length, ColorIndex, Text + sb.ToString());
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + carletOffset + instanceName.Length);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + carletOffset,headIndex + carletOffset + instanceName.Length);

//            e.Handled = true;

            CodeEditor2.Controller.CodeEditor.RequestReparse();
        }


    }
}
