using Avalonia.Platform;
using Avalonia.Threading;
using DynamicData;
using FaissNet;
using Microsoft.Extensions.AI;
using pluginVerilog.Verilog.BuildingBlocks;
using Svg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.LLM
{
    public static class InitializeLLMAgent
    {
        public static void Run(CodeEditor2.LLM.LLMAgent agent) 
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            agent.PersudoFunctionCallMode = true;

            string prompt;
            using (var stream = AssetLoader.Open(new Uri("avares://CodeEditor2VerilogPlugin/Assets/LLMPrompt/AgentBasePrompt.md")))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                var encoding = Encoding.GetEncoding("UTF-8");
                prompt = encoding.GetString(buffer);
            }
            agent.BasePrompt = prompt;

            agent.PromptParameters.Add("Role", "a highly skilled hardware engineer with extensive knowledge in many programming languages, frameworks, design patterns, and best practices");
            // agent functions

            { // GetModuleDefinition
                [Description("指定されたmoduleが定義されているrtlファイルの内容を取得します")]
                string GetModuleDefinition(
                [Description("module name")] string moduleName)
                {
                    var node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
                    if (node == null) return "illegal moduleName";
                    var project = node.GetProject();
                    ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                    if (projectProperty == null) throw new Exception();
                    var file = projectProperty.GetBuildingBlock(moduleName)?.File;
                    if (file == null || file.CodeDocument==null) return "not found";

                    StringBuilder sb = new StringBuilder();
                    sb.Append(file.RelativePath);
                    sb.Append("```verilog");
                    sb.Append(file.CodeDocument.CreateString());
                    sb.Append("```");

                    return sb.ToString();
                }
                AIFunction getModuleDefinition = AIFunctionFactory.Create(GetModuleDefinition, "GetModuleDefinition");
                agent.Tools.Add(getModuleDefinition);
            }

            { // GetModulePorts
                [Description("指定されたモジュールのポート定義を取得します")]
                string GetModulePorts(
                [Description("module name")] string moduleName)
                {
                    var node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
                    if (node == null) return "illegal moduleName";
                    var project = node.GetProject();
                    ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                    if (projectProperty == null) throw new Exception();
                    var file = projectProperty.GetBuildingBlock(moduleName)?.File;
                    if (file == null || file.CodeDocument == null) return "not found";
                    Verilog.ParsedDocument? pdoc = file.VerilogParsedDocument;
                    if(pdoc == null) return "not found";
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

                    return sb.ToString();
                }
                AIFunction getModulePorts = AIFunctionFactory.Create(GetModulePorts, "GetModulePorts");
                agent.Tools.Add(getModulePorts);
            }

        }
    }
}
