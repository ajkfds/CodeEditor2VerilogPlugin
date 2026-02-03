using CodeEditor2.Data;
using CodeEditor2.LLM.Tools;
using Microsoft.Extensions.AI;
using pluginVerilog.Tool;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.LLM.Tools
{
    public class RunVerilogSimulation : LLMTool
    {
        public RunVerilogSimulation(CodeEditor2.Data.Project project,ISimulation simulation) : base(project) 
        {
            this.simulation = simulation;
        }

        private ISimulation simulation;
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "run_verilog_simulation"); }
        [Description("指定されたモジュールをtop moduleとしてsimulationを流し、結果を取得する")]
        public async Task<string> Run(
        [Description("module name")]
        string moduleName,
        CancellationToken cancellationToken
        ) {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) throw new Exception();
            var file = projectProperty.GetBuildingBlock(moduleName)?.File;
            Data.VerilogFile? verilogFile = file as Data.VerilogFile;
            if (verilogFile == null) return "failed to run simulation";

            simulation.TopFile = verilogFile;
            string log;
            try
            {
                log = await simulation.RunSimulationAsync(cancellationToken);
            }catch (OperationCanceledException)
            {
                return "simulation calceled.";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(verilogFile.RelativePath+" simulation result");
            sb.Append("\n");
            sb.Append("```");
            sb.Append(log);
            sb.Append("```");

            return sb.ToString();
        }
    }
}
