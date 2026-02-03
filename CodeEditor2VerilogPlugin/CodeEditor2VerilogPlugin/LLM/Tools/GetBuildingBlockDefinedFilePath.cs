using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.LLM.Tools
{
    public class GetBuildingBlockDefinedFilePath : CodeEditor2.LLM.Tools.LLMTool
    {
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "get_buildingblock_defined_filepath"); }
        public override string XmlExample { get; } = """
            ```xml
            <get_buildingblock_defined_filepath>
            <buildingBlockName> verilog/systemverilog building block name </buildingBlockName>
            </get_buildingblock_defined_filepath>         
            ```
            """;

        [Description("指定されたbuilding block(module,class,program)が定義されているrtlファイルのpathを取得します。pathはproject rootに対する相対パスです。")]
        public async Task<string> Run(
        [Description("building block name")] string buildingBlockName)
        {
            var node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return "illegal moduleName";

            CodeEditor2.Data.Project? project = GetProject();
            if (project == null) return "Failed to execute tool. Cannot get current project.";

            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            var file = projectProperty.GetBuildingBlock(buildingBlockName)?.File;
            if (file == null || file.CodeDocument == null) return "not found";

            
            StringBuilder sb = new StringBuilder();
            sb.Append(file.RelativePath);

            await Task.Delay(0);
            return sb.ToString();
        }
    }
}
