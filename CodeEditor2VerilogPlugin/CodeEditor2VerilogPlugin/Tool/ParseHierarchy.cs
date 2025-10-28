using Avalonia.Threading;
using CodeEditor2.CodeEditor;
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
            ThisFileOnly
        }
        
        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile,ParseMode parseMode)
        {
            if(parseMode == ParseMode.ForceAllFiles) // dont cancel
            {
                await Task.Run(async () =>
                {
                    await runParse(textFile,parseMode, null);
                });
                return;
            }


            if (_cts != null)
            {
                textFile.ReparseRequested = true;
                _cts.Cancel();

                try
                {
                    // wait completion of the previous task
                    //if (_currentTask != null) await _currentTask;
                }
                catch (OperationCanceledException) { }
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = Task.Run(async () =>
            {
                await runParse(textFile,parseMode, token);
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

        private static async Task runParse(TextFile textFile, ParseMode parseMode, CancellationToken? token)
        {
            ConcurrentStack<TextFile> reparseTargetFiles = new ConcurrentStack<TextFile>();
            ConcurrentDictionary<string, byte> completeIDList = new ConcurrentDictionary<string, byte>();

            await parseTextFile(textFile,reparseTargetFiles,completeIDList, parseMode, token);

            while (reparseTargetFiles.Count > 0)
            {
                reparseTargetFiles.TryPop(out TextFile? tfile);
                if (tfile == null) continue;
                token?.ThrowIfCancellationRequested();
                await reparseText(tfile,parseMode, token);
            }
        }

        private static async Task parseTextFile(
            TextFile textFile,
            ConcurrentStack<TextFile> reparseTargetFiles,
            ConcurrentDictionary<string, byte> completeIDList,
            ParseMode parseMode,
            CancellationToken? token 
            )
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

            if (completeIDList.ContainsKey(verilogFile.ID)) return;
            completeIDList.TryAdd(verilogFile.ID,0);

            token?.ThrowIfCancellationRequested();

            //if (parseMode == ParseMode.SearchReparseReqestedTree
            //    && verilogFile.ParseValid && !verilogFile.ReparseRequested) return;


            bool doParse = false;
            if (!verilogFile.ParseValid) doParse = true;
            if(verilogFile.ReparseRequested) doParse = true;
            if (verilogFile.VerilogParsedDocument != null && verilogFile.VerilogParsedDocument.ErrorCount > 0) doParse = true;

            if (doParse)
            {
                var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse, token);
                if (parser == null) return;
                CodeEditor2.Controller.AppendLog("parseHier : " + verilogFile.ID);
                parser.Parse();
                if(parser.ParsedDocument != null)
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

            bool needReparse = false;
            if (!verilogFile.ParseValid) needReparse = true;
            if (verilogFile.ReparseRequested) needReparse = true;
            if (verilogFile.VerilogParsedDocument != null && verilogFile.VerilogParsedDocument.ErrorCount > 0) needReparse = true;
            if (needReparse) reparseTargetFiles.Push((TextFile)verilogFile);

            List<Item> items = new List<Item>();
            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    lock (verilogFile.Items)
                    {
                        foreach (var item in verilogFile.Items.Values)
                        {
                            items.Add(item);
                        }
                    }
                }
            );

            foreach (var item in items)
            {
                if(item is CodeEditor2.Data.TextFile tfile)
                {
                    await parseTextFile(tfile, reparseTargetFiles, completeIDList, parseMode, token);
                }
            }
        }

        private static async Task reparseText(TextFile textFile, ParseMode parseMode,CancellationToken? token)
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


            var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse, token);
            if (parser == null) return;

            CodeEditor2.Controller.AppendLog("reparseHier : " + verilogFile.ID);
            parser.Parse();
            if (parser.ParsedDocument != null)
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
