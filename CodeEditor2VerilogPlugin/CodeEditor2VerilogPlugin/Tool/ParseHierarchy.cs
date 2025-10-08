using Avalonia.Threading;
using CodeEditor2.Data;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace pluginVerilog.Tool
{
    public class ParseHierarchy
    {
        private static Task? _currentTask;
        private static CancellationTokenSource? _cts;
        public static async Task Parse(CodeEditor2.Data.TextFile textFile)
        {
            if (textFile.ParseValid && !textFile.ReparseRequested) return;

            if (_cts != null)
            {
                CodeEditor2.Controller.AppendLog("Cancelling previous parse...");
                _cts.Cancel();

                try
                {
//                    if (_currentTask != null)
//                        await _currentTask;
                }
                catch (OperationCanceledException) { }
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = Task.Run(async () =>
            {
                await runParse(textFile, token);
            }, token);

            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
            }
            return;
        }

        private static async Task runParse(CodeEditor2.Data.TextFile textFile, CancellationToken token)
        {
            for (int i = 0; i < 2; i++)
            {
                if (textFile is Data.VerilogModuleInstance)
                {
                    await parseVerilogModule((Data.VerilogModuleInstance)textFile,token);
                }
                else if (textFile is Data.VerilogFile)
                {
                    await parseVerilogModule((Data.VerilogFile)textFile, token);
                }
                else if (textFile is Data.InterfaceInstance)
                {
                    await parseVerilogModule((Data.InterfaceInstance)textFile, token);
                }
            }
        }

        private static async Task parseVerilogModule(Data.IVerilogRelatedFile verilogFile, CancellationToken token)
        {
            CodeEditor2.Controller.AppendLog("parseHier : " + verilogFile.ID);
            token.ThrowIfCancellationRequested();

            var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse);
            if (parser == null) return;

            parser.Parse();
            List<Item> items = new List<Item>();
            if (parser.ParsedDocument != null)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                    verilogFile.Update();
                    lock (verilogFile.Items)
                    {
                        foreach (var item in verilogFile.Items.Values)
                        {
                            items.Add(item);
                        }
                    }
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(
                        async () =>
                        {
                            await Task.Run(
                                () =>
                                {
                                    verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                                    verilogFile.Update();
                                    lock (verilogFile.Items)
                                    {
                                        foreach (var item in verilogFile.Items.Values)
                                        {
                                            items.Add(item);
                                        }
                                    }
                                }
                            );
                        }
                    );
                }
            }
            foreach (var item in items)
            {
                if(item is CodeEditor2.Data.TextFile)
                {
                    await Parse((CodeEditor2.Data.TextFile)item);
                }
            }
        }


   }
}
