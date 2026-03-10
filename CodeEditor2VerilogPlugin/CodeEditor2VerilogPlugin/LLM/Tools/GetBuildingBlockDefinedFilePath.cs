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
            謖・ｮ壹＆繧後◆building block(module,class,program)縺悟ｮ夂ｾｩ縺輔ｌ縺ｦ縺・ｋrtl繝輔ぃ繧､繝ｫ縺ｮfile path繧貞叙蠕励＠縺ｾ縺吶・
            file path縺ｯproject root縺ｫ蟇ｾ縺吶ｋ逶ｸ蟇ｾ繝代せ縺ｧ縺吶・
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
