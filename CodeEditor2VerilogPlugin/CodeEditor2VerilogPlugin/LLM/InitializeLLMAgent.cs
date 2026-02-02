using Avalonia.Platform;
using Avalonia.Threading;
using DynamicData;
using FaissNet;
using Microsoft.Extensions.AI;
using pluginVerilog.LLM.Tools;
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
        public static void Run(CodeEditor2.LLM.LLMAgent agent,bool useFunctioncallApi) 
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            agent.PersudoFunctionCallMode = true;

            string prompt;

            string promptPath = "";
            if (useFunctioncallApi)
            {
                promptPath = "avares://CodeEditor2VerilogPlugin/Assets/LLMPrompt/AgentBasePromptWFunctionCall.md";
            }
            else
            {
                promptPath = "avares://CodeEditor2VerilogPlugin/Assets/LLMPrompt/AgentBasePrompt.md";
            }

            using (var stream = AssetLoader.Open(new Uri(promptPath)))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                var encoding = Encoding.GetEncoding("UTF-8");
                prompt = encoding.GetString(buffer);
            }
            agent.BasePrompt = prompt;

            agent.PromptParameters.Add("Role", "a highly skilled hardware engineer with extensive knowledge in many programming languages, frameworks, design patterns, and best practices");
            // agent functions

            CodeEditor2.LLM.Tools.ReadFile readFile = new CodeEditor2.LLM.Tools.ReadFile();
            agent.Tools.Add(readFile.GetAIFunction());

            CodeEditor2.LLM.Tools.ReplaceInFile replaceInFile = new CodeEditor2.LLM.Tools.ReplaceInFile();
            agent.Tools.Add(replaceInFile.GetAIFunction());

            CodeEditor2.LLM.Tools.SearchFiles searchFiles = new CodeEditor2.LLM.Tools.SearchFiles();
            agent.Tools.Add(searchFiles.GetAIFunction());

            CodeEditor2.LLM.Tools.WriteToFile writeToFile = new CodeEditor2.LLM.Tools.WriteToFile();
            agent.Tools.Add(writeToFile.GetAIFunction());

            CodeEditor2.LLM.Tools.ListFiles listFiles = new CodeEditor2.LLM.Tools.ListFiles();
            agent.Tools.Add(listFiles.GetAIFunction());
            GetBuildingBlockDefinedFilePath getBuildingBlockDefinedFilePath = new GetBuildingBlockDefinedFilePath();
            agent.Tools.Add(getBuildingBlockDefinedFilePath.GetAIFunction());

            //GetModuleDefinition getModuleDefinition = new GetModuleDefinition();
            //agent.Tools.Add(getModuleDefinition.GetAIFunction());

            //GetModulePort getModulePort = new GetModulePort();
            //agent.Tools.Add(getModulePort.GetAIFunction());
        }
    }
}
