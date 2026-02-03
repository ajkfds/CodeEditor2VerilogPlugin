using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.LLM.Tools
{
    public class GetModuleDefinition : CodeEditor2.LLM.Tools.LLMTool
    {
        public GetModuleDefinition(CodeEditor2.Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "get_module_definition"); }

        [Description("指定されたmoduleが定義されているrtlファイルの内容を取得します")]
        public async Task<string> Run(
        [Description("module name")] string moduleName)
        {
            if (project == null) return "Failed to execute tool. Cannot get current project.";

            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            var file = projectProperty.GetBuildingBlock(moduleName)?.File;
            if (file == null || file.CodeDocument == null) return "not found";

            StringBuilder sb = new StringBuilder();
            sb.Append(file.RelativePath);
            sb.Append("```verilog");
            sb.Append(file.CodeDocument.CreateString());
            sb.Append("```");

            await Task.Delay(0);
            return sb.ToString();
        }
    }
}
