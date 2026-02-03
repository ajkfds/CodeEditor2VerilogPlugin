using Avalonia.Controls.Documents;
using CodeEditor2.LLM.Tools;
using Microsoft.Extensions.AI;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.LLM.Tools
{
    public class GetModulePort:LLMTool
    {
        public GetModulePort(CodeEditor2.Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "get_module_definition"); }
        [Description("指定されたモジュールのポート定義を取得します")]
        public async Task<string> Run(
        [Description("module name")] string moduleName)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            var file = projectProperty.GetBuildingBlock(moduleName)?.File;
            if (file == null || file.CodeDocument == null) return "not found";
            Verilog.ParsedDocument? pdoc = file.VerilogParsedDocument;
            if (pdoc == null) return "not found";
            if (!pdoc.Root.BuildingBlocks.ContainsKey(moduleName)) return "not found";
            BuildingBlock buildingBlock = pdoc.Root.BuildingBlocks[moduleName];
            Verilog.IPortNameSpace? portNameSpace = buildingBlock as Verilog.IPortNameSpace;
            if (portNameSpace == null) return "not found";

            StringBuilder sb = new StringBuilder();
            sb.Append(file.RelativePath);
            sb.Append("```verilog");
            string? portGroup = null;
            foreach (var port in portNameSpace.PortsList)
            {
                if (port.PortGroupName != portGroup && portGroup != "")
                {
                    portGroup = port.PortGroupName;
                    sb.Append("// ");
                    sb.Append(portGroup);
                    sb.Append("\n");
                }
                sb.Append(port.CreateDefinitionString());
                sb.Append("\n");
            }
            sb.Append("```");

            await Task.Delay(0);
            return sb.ToString();
        }
    }
}
