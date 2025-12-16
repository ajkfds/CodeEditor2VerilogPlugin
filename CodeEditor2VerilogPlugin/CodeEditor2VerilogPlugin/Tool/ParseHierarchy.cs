using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.FileTypes;
using pluginVerilog.Data;
using pluginVerilog.FileTypes;
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

        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            if (parseMode == ParseMode.ForceAllFiles) // dont cancel
            {
                await Task.Run(async () =>
                {
                    await runParallel(textFile, parseMode, null);
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
                await runParallel(textFile, parseMode, token);
            }, token);

            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
                _currentTask = null;
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }
            finally
            {
                _currentTask = null;
            }
            return;
        }

        public record ParseTask(
            string Id,
            CodeEditor2.Data.TextFile tarfgetTextFile,
            bool topLevel = false
            );
        private static async Task runParallel(CodeEditor2.Data.TextFile textFile, ParseMode parseMode, CancellationToken? token)
        {
            textFile.ReparseRequested = true;

            var workQueue = new ConcurrentQueue<ParseTask>();
            var reparseTargetFiles = new ConcurrentStack<CodeEditor2.Data.TextFile>();
            var completeIds = new ConcurrentDictionary<string, bool>();
            var firstHierTaskCount = new ConcurrentStack<bool>();
            var signal = new SemaphoreSlim(0); // starter
            int workerCount = Environment.ProcessorCount;

            // entry first
            ParseTask task = new ParseTask(Id: textFile.Key, tarfgetTextFile: textFile, topLevel: true);
            firstHierTaskCount.Append(true);
            EnqueueWork(task, workQueue, completeIds, signal);

            // boot workers
            var workers = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
            {
                int index = i;
                workers[i] = Task.Run(async () =>
                {
                    while (true)
                    {
                        token?.ThrowIfCancellationRequested();
                        await signal.WaitAsync(); // wait fist task

                        if (workQueue.TryDequeue(out var newTask))
                        {
                            await parseTextFile(index,newTask, reparseTargetFiles, workQueue, completeIds, firstHierTaskCount, signal, parseMode, token);

                            // re-entry top level task after all 2nd level task complete
                            if(firstHierTaskCount.TryPop(out bool first))
                            {
                                if (first)
                                {
                                    ParseTask reEntryTask = new ParseTask(Id: textFile.Key, tarfgetTextFile: textFile, topLevel: false);
                                    ForceEnqueueWork(reEntryTask, workQueue, completeIds, signal);
                                }
                            }
                        }
                        if (completeIds.Count > 0 && workQueue.IsEmpty)
                        {
                            signal.Release(workerCount);
                            break;
                        }
                    }
                });
            }

            try
            {
                await Task.WhenAll(workers);
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            token?.ThrowIfCancellationRequested();

            // reparse
            while (reparseTargetFiles.Count > 0)
            {
                reparseTargetFiles.TryPop(out CodeEditor2.Data.TextFile? tfile);
                if (tfile == null) continue;
                await reparseText(tfile, parseMode, token);
            }

            if (parseMode == ParseMode.ForceAllFiles)
            {
                CodeEditor2.Controller.AppendLog("parseComplete : " + textFile.ID, Avalonia.Media.Colors.Cyan);
            }
            else
            {
                CodeEditor2.Controller.AppendLog("parseComplete : " + textFile.ID);
            }
        }

        static void EnqueueWork(ParseTask parse,
            ConcurrentQueue<ParseTask> workQueue,
            ConcurrentDictionary<string, bool> completeIds,
            SemaphoreSlim signal)
        {
            if (completeIds.TryAdd(parse.Id, true))
            {
                workQueue.Enqueue(parse);
                signal.Release(); // start worker
            }
        }
        static void ForceEnqueueWork(ParseTask parse,
            ConcurrentQueue<ParseTask> workQueue,
            ConcurrentDictionary<string, bool> completeIds,
            SemaphoreSlim signal)
        {
            workQueue.Enqueue(parse);
            signal.Release(); // start worker
        }
        private static async Task parseTextFile(
            int index, 
            ParseTask task,
            ConcurrentStack<CodeEditor2.Data.TextFile> reparseTargetFiles,
            ConcurrentQueue<ParseTask> workQueue,
            ConcurrentDictionary<string, bool> completeIds,
            ConcurrentStack<bool> firstHierTaskCount,
            SemaphoreSlim signal,
            ParseMode parseMode,
            CancellationToken? token
            )
        {
            CodeEditor2.Data.TextFile textFile = task.tarfgetTextFile;
            Data.IVerilogRelatedFile? verilogFile = null;
            if (textFile is Data.VerilogModuleInstance)
            {
                VerilogModuleInstance mInstance = (Data.VerilogModuleInstance)textFile;
                verilogFile = mInstance;
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

            token?.ThrowIfCancellationRequested();


            bool doParse = false;
            if (verilogFile.ReparseRequested) doParse = true;
//            if (verilogFile.VerilogParsedDocument != null && verilogFile.VerilogParsedDocument.ErrorCount > 0) doParse = true;

            if (doParse)
            {
                var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse, token);
                if (parser == null) return;

                if (parseMode == ParseMode.ForceAllFiles)
                {
                    CodeEditor2.Controller.AppendLog("parseHier "+index.ToString()+" : " + verilogFile.ID, Avalonia.Media.Colors.Cyan);
                }
                else
                {
                    CodeEditor2.Controller.AppendLog("parseHier "+index.ToString() + " : " + verilogFile.ID);
                }
                await parser.ParseAsync();
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

            bool needReparse = false;
            if (verilogFile.ReparseRequested) needReparse = true;
//            if (verilogFile.VerilogParsedDocument != null && verilogFile.VerilogParsedDocument.ErrorCount > 0) needReparse = true;
            if (needReparse) reparseTargetFiles.Push((CodeEditor2.Data.TextFile)verilogFile);

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
                if (item is CodeEditor2.Data.TextFile tfile)
                {
                    ParseTask newTask = new ParseTask(Id: tfile.Key, tarfgetTextFile: tfile);
                    if (task.topLevel)
                    {
                        firstHierTaskCount.Append(false);
                    }
                    EnqueueWork(newTask, workQueue, completeIds, signal);
                    
                }
            }
        }

        private static async Task reparseText(CodeEditor2.Data.TextFile textFile, ParseMode parseMode, CancellationToken? token)
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

            if (parseMode == ParseMode.ForceAllFiles)
            {
                CodeEditor2.Controller.AppendLog("reparseHier : " + verilogFile.ID, Avalonia.Media.Colors.Cyan);
            }
            else
            {
                CodeEditor2.Controller.AppendLog("reparseHier : " + verilogFile.ID);
            }
            await parser.ParseAsync();
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
