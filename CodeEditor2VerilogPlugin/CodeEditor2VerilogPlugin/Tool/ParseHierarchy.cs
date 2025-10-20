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

        public enum ParseMode
        {
            SearchReparseReqestedTree,
            ForceAllFiles,
            ThisFileOnly,
            SearchAllAndParseReparseReqested
        }


        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile,ParseMode parseMode)
        {
            if (textFile.ParseValid && !textFile.ReparseRequested) return;

            if (_cts != null)
            {
                textFile.ReparseRequested = true;
                _cts.Cancel();

                try
                {
                    // wait completion of the previous task
                    if (_currentTask != null) await _currentTask;
                }
                catch (OperationCanceledException) { }
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = Task.Run(async () =>
            {
                await runParse(textFile, token, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            }, token);

            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
                _currentTask = null;
            }
            finally
            {
                _currentTask = null;
            }
            return;
        }

        private static async Task runParse(TextFile textFile, CancellationToken token, ParseMode parseMode)
        {
            List<TextFile> fileStack = new List<TextFile>(); 

            await parseText(textFile,token, fileStack,parseMode);

            while(fileStack.Count != 0)
            {
                TextFile reparseTextFile = fileStack.Last();
                fileStack.RemoveAt(fileStack.Count - 1);
                token.ThrowIfCancellationRequested();
                await reparseText(reparseTextFile,parseMode);
            }
        }

        private static async Task parseText(TextFile textFile, CancellationToken token, List<TextFile> fileStack, ParseMode parseMode)
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

            token.ThrowIfCancellationRequested();
            if (fileStack.Contains((TextFile)verilogFile)) return;

            if (parseMode == ParseMode.SearchReparseReqestedTree
                && verilogFile.ParseValid && !verilogFile.ReparseRequested) return;

            CodeEditor2.Controller.AppendLog("parseHier : " + verilogFile.ID);

            if (!fileStack.Contains(textFile))
            {
                fileStack.Add(textFile);
            }

            var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse);
            if (parser == null) return;

            if(parseMode == ParseMode.SearchAllAndParseReparseReqested
                && verilogFile.ParseValid && !verilogFile.ReparseRequested
                )
            { // skip parse

            }
            else
            {
                parser.Parse();
            }

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
                            verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                            await verilogFile.UpdateAsync();
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
            }
            textFile.ReparseRequested = true;
            if (parseMode == ParseMode.ThisFileOnly) return;

                foreach (var item in items)
            {
                if(item is CodeEditor2.Data.TextFile)
                {
                    await parseText((CodeEditor2.Data.TextFile)item,token,fileStack, parseMode);
                }
            }
        }

        private static async Task reparseText(TextFile textFile, ParseMode parseMode)
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
            if (parser.ParsedDocument != null)
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                    verilogFile.Update();
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(
                        async () =>
                        {
                            verilogFile.AcceptParsedDocument(parser.ParsedDocument);
                            await verilogFile.UpdateAsync();
                        }
                    );
                }
            }
        }

    }
}
