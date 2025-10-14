using Avalonia.Threading;
using CodeEditor2.Data;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Concurrent;
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
        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile)
        {
            if (textFile.ParseValid && !textFile.ReparseRequested) return;

//            if (_cts != null)
//            {
//                CodeEditor2.Controller.AppendLog("Cancelling previous parse...");
//                textFile.ReparseRequested = true;
//                _cts.Cancel();

//                try
//                {
//                    // wait completion of the previous task
////                    if (_currentTask != null) await _currentTask;
//                }
//                catch (OperationCanceledException) { }
//            }

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

        private static async Task runParse(TextFile textFile, CancellationToken token)
        {
            List<TextFile> fileStack = new List<TextFile>(); 

            await parseText(textFile,token, fileStack);

            while(fileStack.Count != 0)
            {
                TextFile reparseTextFile = fileStack.Last();
                fileStack.RemoveAt(fileStack.Count - 1);
                token.ThrowIfCancellationRequested();
                await reparseText(reparseTextFile);
            }
        }

        private static async Task parseText(TextFile textFile, CancellationToken token, List<TextFile> fileStack)
        {
            Data.IVerilogRelatedFile? verilogFile = null;
            if (textFile is Data.VerilogModuleInstance)
            {
                verilogFile = (Data.VerilogModuleInstance)textFile;
            }
            else if (textFile is Data.VerilogFile)
            {
                verilogFile = (Data.VerilogFile)textFile;
            }
            else if (textFile is Data.InterfaceInstance)
            {
                verilogFile = (Data.InterfaceInstance)textFile;
            }
            if (verilogFile == null) return;

            CodeEditor2.Controller.AppendLog("parseHier : " + verilogFile.ID);
            token.ThrowIfCancellationRequested();

            if (!fileStack.Contains(textFile))
            {
                fileStack.Add(textFile);
            }

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
            textFile.ReparseRequested = true;

            foreach (var item in items)
            {
                if(item is CodeEditor2.Data.TextFile)
                {
                    await ParseAsync((CodeEditor2.Data.TextFile)item);
                }
            }
        }

        private static async Task reparseText(TextFile textFile)
        {
            Data.IVerilogRelatedFile? verilogFile = null;
            if (textFile is Data.VerilogModuleInstance)
            {
                verilogFile = (Data.VerilogModuleInstance)textFile;
            }
            else if (textFile is Data.VerilogFile)
            {
                verilogFile = (Data.VerilogFile)textFile;
            }
            else if (textFile is Data.InterfaceInstance)
            {
                verilogFile = (Data.InterfaceInstance)textFile;
            }
            if (verilogFile == null) return;

            CodeEditor2.Controller.AppendLog("reparseHier : " + verilogFile.ID);

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
        }

    }
}
