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
        public GetBuildingBlockDefinedFilePath(CodeEditor2.Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "get_buildingblock_defined_filepath"); }
        public override string XmlExample { get; } = """
            ```xml
            <get_buildingblock_defined_filepath>
            <buildingBlockName> verilog/systemverilog building block name </buildingBlockName>
            </get_buildingblock_defined_filepath>         
            ```
            """;

        [Description("""
            指定されたbuilding block(module,class,program)が定義されているrtlファイルのfile pathを取得します。
            file pathはproject rootに対する相対パスです。
            """)]
            
        public async Task<string> Run(
        [Description("building block name")] string buildingBlockName)
        {
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
