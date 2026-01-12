using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.AI;
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
        public static void Run(pluginAi.LLMChat llmChat) 
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            string prompt;
            using (var stream = AssetLoader.Open(new Uri("avares://CodeEditor2VerilogPlugin/Assets/LLMPrompt/AgentBasePrompt.md")))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                var encoding = Encoding.GetEncoding("UTF-8");
                prompt = encoding.GetString(buffer);
            }
            llmChat.BasePrompt = prompt;

            llmChat.PromptParameters.Add("Role", "a highly skilled hardware engineer with extensive knowledge in many programming languages, frameworks, design patterns, and best practices");
            // agent functions
            /*
            {
                [Description("指定された場所の現在の天気を取得します")]
                string GetWeather(
                [Description("都市名 (例: 東京)")] string location, string unit = "celsius")
                {
                    return $"{location}の天気は晴れ、気温は25度です。";
                }
                AIFunction weatherFunction = AIFunctionFactory.Create(GetWeather, "GetWeather");
                llmChat.Tools.Add(weatherFunction);
            }
            */

            { // GetRtl
                [Description("指定されたモジュール名の定義rtlを取得します")]
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
                    sb.Append("```verilog");
                    sb.Append(file.CodeDocument.CreateString());
                    sb.Append("```");


                    return sb.ToString();
                }
                AIFunction getModuleDefinition = AIFunctionFactory.Create(GetModuleDefinition, "GetModuleDefinition");
                llmChat.Tools.Add(getModuleDefinition);
            }

            Dispatcher.UIThread.Invoke(async () =>
            {
                await llmChat.ResetAsync(cancellationToken);
            });
        }
    }
}
