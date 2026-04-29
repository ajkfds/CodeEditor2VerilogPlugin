using Avalonia.Threading;
using CodeEditor2.Data;
using Microsoft.Extensions.AI;
using pluginVerilog.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Tool
{
    public class ParseHierarchy
    {
        /// <summary>
        /// Parse request record for queue-based processing
        /// </summary>
        public record ParseRequest(
            CodeEditor2.Data.TextFile TextFile,
            ParseMode Mode,
            DateTime Timestamp
        );

        /// <summary>
        /// Queue for pending parse requests
        /// </summary>
        private static readonly ConcurrentQueue<ParseRequest> _parseQueue = new();

        /// <summary>
        /// Indicates if a queue processor is running
        /// </summary>
        private static volatile bool _isProcessingQueue = false;

        /// <summary>
        /// Semaphore to ensure only one parse operation runs at a time
        /// </summary>
        private static readonly SemaphoreSlim _parseLock = new(1, 1);

        /// <summary>
        /// Timestamp of the currently executing parse request
        /// </summary>
        private static DateTime _currentParseTimestamp = DateTime.MinValue;

        public enum ParseMode
        {
            SearchReparseReqestedTree,
            ForceAllFiles,
            ThisFileOnly
        }

        /// <summary>
        /// Enqueues a parse request and starts queue processing if not already running.
        /// Replaces the immediate cancellation approach with queue-based sequential processing.
        /// All parse modes (including ForceAllFiles) now go through the queue.
        /// </summary>
        public static void PostParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var request = new ParseRequest(textFile, parseMode, DateTime.UtcNow);
                _parseQueue.Enqueue(request);

                // Start queue processor if not already running
                if (!_isProcessingQueue)
                {
                    _ = ProcessQueueAsync();
                }
            });
        }

        /// <summary>
        /// Synchronous parse that waits for completion.
        /// For backward compatibility - prefer using PostParseAsync for non-blocking behavior.
        /// </summary>
        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            await _parseLock.WaitAsync();
            try
            {
                await ParseInternalAsync(textFile, parseMode);
            }
            finally
            {
                _parseLock.Release();
            }
        }

        /// <summary>
        /// Processes parse requests from the queue sequentially.
        /// Only one parse operation runs at a time, ensuring consistent results.
        /// </summary>
        private static async Task ProcessQueueAsync()
        {
            _isProcessingQueue = true;
            try
            {
                while (_parseQueue.TryDequeue(out var request))
                {
                    // Wait for any currently running parse to complete
                    await _parseLock.WaitAsync();
                    try
                    {
                        _currentParseTimestamp = request.Timestamp;
                        await ParseAsync(request.TextFile, request.Mode);
                    }
                    finally
                    {
                        _parseLock.Release();
                    }
                }
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        /// <summary>
        /// Internal implementation that performs the actual parsing.
        /// Returns parsed files and include files for non-ForceAllFiles modes.
        /// </summary>
        private static async Task<(List<IVerilogRelatedFile> files, List<IVerilogRelatedFile> includeFiles)?> ParseInternalAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            ConcurrentDictionary<string, IVerilogRelatedFile> filesDict = new ConcurrentDictionary<string, IVerilogRelatedFile>();
            ConcurrentDictionary<string, IVerilogRelatedFile> includeFilesDict = new ConcurrentDictionary<string, IVerilogRelatedFile>();

            try
            {
                await runParallel(textFile, parseMode, filesDict, includeFilesDict, null);
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
            }

            return (filesDict.Values.ToList(), includeFilesDict.Values.ToList());
        }

        public record ParseTask(
            string Id,
            CodeEditor2.Data.TextFile tarfgetTextFile,
            bool topLevel = false
            );
        private static async Task runParallel(CodeEditor2.Data.TextFile textFile, ParseMode parseMode,
            ConcurrentDictionary<string, IVerilogRelatedFile> files,
            ConcurrentDictionary<string, IVerilogRelatedFile> includeFiles,
            CancellationToken? token)
        {
            textFile.ReparseRequested = true;

            var workQueue = new ConcurrentQueue<ParseTask>();
            var reparseTargetFiles = new ConcurrentStack<CodeEditor2.Data.TextFile>();
            var completeIds = new ConcurrentDictionary<string, bool>();
            var firstHierTaskCount = new ConcurrentStack<bool>();
            var signal = new SemaphoreSlim(0); // starter
            int workerCount = Environment.ProcessorCount;
            if (workerCount > 2) workerCount--;
            int activeTaskCount = 0;

            // entry first
            ParseTask task = new ParseTask(Id: textFile.Key, tarfgetTextFile: textFile, topLevel: true);
            firstHierTaskCount.Append(true);
            EnqueueWork(task, workQueue, completeIds, signal);

            for (int i = 0; i < workerCount; i++)
            { // pend reparse first task
                firstHierTaskCount.Append(false);
            }

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
                        if (await signal.WaitAsync(TimeSpan.FromMicroseconds(500)))
                        {
                            if (workQueue.TryDequeue(out var newTask))
                            {
                                Interlocked.Increment(ref activeTaskCount);

                                await parseTextFile(index, newTask, reparseTargetFiles, workQueue, completeIds, firstHierTaskCount,files,includeFiles, signal, parseMode, token);

                                // re-entry top level task after all 2nd level task complete
                                if (firstHierTaskCount.TryPop(out bool first))
                                {
                                    if (first)
                                    {
                                        textFile.PostParse();
                                    }
                                }

                                var currentCount = Interlocked.Decrement(ref activeTaskCount);
                                if (currentCount == 0 && workQueue.IsEmpty)
                                {
                                    break;
                                }
                            }
                        }
                        else if (activeTaskCount == 0 && workQueue.IsEmpty)
                        {
                            break;
                        }
                    }
                });
            }

            await Task.WhenAll(workers);

            token?.ThrowIfCancellationRequested();

            // reparse
            while (reparseTargetFiles.Count > 0)
            {
                reparseTargetFiles.TryPop(out CodeEditor2.Data.TextFile? tfile);
                if (tfile == null) continue;
                await reparseText(tfile,files,includeFiles, parseMode, token);
            }

            if (parseMode == ParseMode.ForceAllFiles)
            {
                CodeEditor2.Controller.AppendLog("parseComplete : " + textFile.ID, Avalonia.Media.Colors.Violet);
            }
            else
            {
                CodeEditor2.Controller.AppendLog("parseComplete : " + textFile.ID, Avalonia.Media.Colors.Orange);
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
            ConcurrentDictionary<string, IVerilogRelatedFile> files,
            ConcurrentDictionary<string, IVerilogRelatedFile> includeFiles,
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
            CodeEditor2.Controller.AppendLog("-- parse " + index.ToString() + " : " + verilogFile.ID);

            token?.ThrowIfCancellationRequested();


            bool doParse = false;
            if (verilogFile.ReparseRequested) doParse = true;
            if (parseMode == ParseMode.ForceAllFiles) doParse = true;

            if (doParse)
            {
                var parser = verilogFile.CreateDocumentParser(CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse, token);
                if (parser == null) return;

                if (parseMode == ParseMode.ForceAllFiles)
                {
                    CodeEditor2.Controller.AppendLog("parseHier " + index.ToString() + " : " + verilogFile.ID, Avalonia.Media.Colors.Cyan);
                }
                else
                {
                    CodeEditor2.Controller.AppendLog("parseHier " + index.ToString() + " : " + verilogFile.ID);
                }
                await parser.ParseAsync();
                if (parser.ParsedDocument != null)
                {
                    await verilogFile.AcceptParsedDocumentAsync(parser.ParsedDocument);
                    //                    await verilogFile.UpdateAsync();
                }

                Verilog.ParsedDocument? parsedDocument = parser.ParsedDocument as Verilog.ParsedDocument;
                if(parsedDocument !=null)
                {
                    files.AddOrUpdate(verilogFile.RelativePath, verilogFile, (key, oldItem) => { return verilogFile; });
                    foreach (var include in parsedDocument.IncludeFiles.Values)
                    {
                        includeFiles.AddOrUpdate(include.RelativePath, include, (key, oldItem) => { return include; });
                    }
                }
            }

            bool needReparse = false;
            if (verilogFile.ReparseRequested) needReparse = true;

            if (needReparse)
            {
                reparseTargetFiles.Push((CodeEditor2.Data.TextFile)verilogFile);
            }

            List<Item> items = new List<Item>();
            items = verilogFile.Items.ToList();

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

        private static async Task reparseText(CodeEditor2.Data.TextFile textFile,
            ConcurrentDictionary<string, IVerilogRelatedFile> files,
            ConcurrentDictionary<string, IVerilogRelatedFile> includeFiles,
            ParseMode parseMode, CancellationToken? token)
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
            Verilog.ParsedDocument? parsedDocument = parser.ParsedDocument as Verilog.ParsedDocument;
            if (parsedDocument != null)
            {
                await verilogFile.AcceptParsedDocumentAsync(parsedDocument);

                files.AddOrUpdate(verilogFile.RelativePath, verilogFile, (key, oldItem) => { return verilogFile; });
                foreach (var include in parsedDocument.IncludeFiles.Values)
                {
                    files.AddOrUpdate(include.RelativePath, include, (key, oldItem) => { return include; });
                }
            }
        }

    }
}
