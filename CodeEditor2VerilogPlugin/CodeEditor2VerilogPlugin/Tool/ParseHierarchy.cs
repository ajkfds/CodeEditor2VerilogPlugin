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


        private static ParseMode _currentParseMode = ParseMode.ThisFileOnly;

        public enum ParseMode
        {
            SearchReparseReqestedTree,
            ForceAllFiles,
            ThisFileOnly
        }

        public static void PostParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            _ = Task.Run(
                async() => { await ParseAsync(textFile, parseMode); }
                );
        }

        private static CancellationTokenSource? cts = null;
        public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        {
            if(cts != null)
            {
                cts.Cancel();
            }

            CancellationTokenSource _cts = new CancellationTokenSource();
            cts = _cts;
            try
            {
                await runParallelAsync(textFile, parseMode, _cts.Token);
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                _cts.Dispose();
                if (cts == _cts)
                {
                    cts = null;
                }
            }
        }







        ///// <summary>
        ///// Parse request record for queue-based processing
        ///// </summary>
        //public record ParseRequest(
        //    CodeEditor2.Data.TextFile TextFile,
        //    ParseMode Mode,
        //    DateTime Timestamp
        //);

        ///// <summary>
        ///// Queue for pending parse requests
        ///// </summary>
        //private static readonly ConcurrentQueue<ParseRequest> _parseQueue = new();

        ///// <summary>
        ///// Indicates if a queue processor is running
        ///// </summary>
        //private static volatile bool _isProcessingQueue = false;

        ///// <summary>
        ///// Semaphore to ensure only one parse operation runs at a time
        ///// </summary>
        //private static readonly SemaphoreSlim _parseLock = new(1, 1);

        ///// <summary>
        ///// Timestamp of the currently executing parse request
        ///// </summary>
        //private static DateTime _currentParseTimestamp = DateTime.MinValue;

        ///// <summary>
        ///// CancellationTokenSource for the currently running parse operation.
        ///// Used to cancel running parses when a ForceAllFiles parse is requested.
        ///// </summary>
        //private static CancellationTokenSource? _currentParseCts = null;

        ///// <summary>
        ///// Lock object for synchronizing access to _currentParseCts
        ///// </summary>
        //private static readonly object _ctsLock = new object();

        ///// <summary>
        ///// Lock object for synchronizing access to the parse queue
        ///// </summary>
        //private static readonly object _queueLock = new object();

        ///// <summary>
        ///// Current parse mode of the running parse operation.
        ///// Used to determine which parse modes can cancel each other.
        ///// </summary>

        ///// <summary>
        ///// Enqueues a parse request and starts queue processing if not already running.
        ///// Replaces the immediate cancellation approach with queue-based sequential processing.
        ///// All parse modes (including ForceAllFiles) now go through the queue.
        ///// </summary>
        //public static void PostParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        //{
        //    Dispatcher.UIThread.Post(() =>
        //    {
        //        var request = new ParseRequest(textFile, parseMode, DateTime.UtcNow);
        //        _parseQueue.Enqueue(request);

        //        // Start queue processor if not already running
        //        if (!_isProcessingQueue)
        //        {
        //            _ = ProcessQueueAsync();
        //        }
        //    });
        //}

        ///// <summary>
        ///// Synchronous parse that waits for completion.
        ///// For backward compatibility - prefer using PostParseAsync for non-blocking behavior.
        ///// </summary>
        //public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        //{
        //    await _parseLock.WaitAsync();
        //    try
        //    {
        //        await ParseInternalAsync(textFile, parseMode);
        //    }
        //    finally
        //    {
        //        _parseLock.Release();
        //    }
        //}

        ///// <summary>
        ///// Semaphore to ensure only one queue processor runs at a time
        ///// </summary>
        //private static readonly SemaphoreSlim _queueProcessorLock = new(1, 1);

        ///// <summary>
        ///// Processes parse requests from the queue sequentially.
        ///// ForceAllFiles parse requests cancel any running non-ForceAll parse and clear the queue.
        ///// </summary>
        //private static async Task ProcessQueueAsync()
        //{
        //    // Try to acquire the queue processor lock to prevent concurrent execution
        //    if (!await _queueProcessorLock.WaitAsync(TimeSpan.FromMilliseconds(100)))
        //    {
        //        // Another queue processor is already running, just return
        //        return;
        //    }

        //    bool hasForceAllRunning = false;
        //    bool shouldReturn = false;

        //    try
        //    {
        //        _isProcessingQueue = true;
        //        while (_parseQueue.TryDequeue(out var request))
        //        {
        //            // Wait for any currently running parse to complete
        //            await _parseLock.WaitAsync();
        //            try
        //            {
        //                // Check if this is a ForceAllFiles request
        //                if (request.Mode == ParseMode.ForceAllFiles)
        //                {
        //                    // ForceAllFiles can only be cancelled by another ForceAllFiles
        //                    // If a non-ForceAll parse is running, cancel it
        //                    CancelCurrentParseIfNotForceAll();

        //                    // Clear the queue of any pending non-ForceAll requests
        //                    ClearNonForceAllFromQueue();

        //                    _currentParseTimestamp = request.Timestamp;

        //                    CodeEditor2.Controller.AppendLog("ForceAllFiles requested - processing now", Avalonia.Media.Colors.Yellow);

        //                    // Call ParseInternalAsync directly to avoid deadlock
        //                    await ParseInternalAsync(request.TextFile, request.Mode);
        //                }
        //                else
        //                {
        //                    // SearchReparseRequestedTree cancels any non-ForceAll parse
        //                    // but waits for ForceAllFiles to complete
        //                    lock (_ctsLock)
        //                    {
        //                        if (_currentParseMode == ParseMode.ForceAllFiles)
        //                        {
        //                            // ForceAllFiles is running, re-queue this request and return
        //                            // It will be processed after ForceAllFiles completes
        //                            // Use queue lock to prevent race condition
        //                            lock (_queueLock)
        //                            {
        //                                _parseQueue.Enqueue(request);
        //                            }
        //                            CodeEditor2.Controller.AppendLog("ForceAllFiles is running, re-queuing SearchReparseRequestedTree request", Avalonia.Media.Colors.Yellow);
        //                            shouldReturn = true;
        //                            return;
        //                        }
        //                    }

        //                    // No ForceAllFiles running, cancel any current parse and proceed
        //                    CancelCurrentParse();

        //                    _currentParseTimestamp = request.Timestamp;

        //                    // Call ParseInternalAsync directly to avoid deadlock
        //                    await ParseInternalAsync(request.TextFile, request.Mode);
        //                }
        //            }
        //            finally
        //            {
        //                _parseLock.Release();
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        _isProcessingQueue = false;
        //        _queueProcessorLock.Release();
        //    }
        //}

        ///// <summary>
        ///// Cancels the currently running parse operation if any
        ///// </summary>
        //private static void CancelCurrentParse()
        //{
        //    lock (_ctsLock)
        //    {
        //        if (_currentParseCts != null)
        //        {
        //            if (!_currentParseCts.IsCancellationRequested)
        //            {
        //                _currentParseCts.Cancel();
        //                CodeEditor2.Controller.AppendLog("Cancelled running parse operation", Avalonia.Media.Colors.Orange);
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Cancels the currently running parse operation only if it's not a ForceAllFiles parse.
        ///// ForceAllFiles parses are never cancelled by other parse modes.
        ///// </summary>
        //private static void CancelCurrentParseIfNotForceAll()
        //{
        //    lock (_ctsLock)
        //    {
        //        if (_currentParseMode == ParseMode.ForceAllFiles)
        //        {
        //            // Don't cancel ForceAllFiles parse
        //            CodeEditor2.Controller.AppendLog("ForceAllFiles is running, not cancelling", Avalonia.Media.Colors.Yellow);
        //            return;
        //        }
        //        CancelCurrentParse();
        //    }
        //}

        ///// <summary>
        ///// Clears all non-ForceAllFiles requests from the queue.
        ///// ForceAllFiles requests are kept (unlikely but possible edge case).
        ///// </summary>
        //private static void ClearNonForceAllFromQueue()
        //{
        //    var tempQueue = new ConcurrentQueue<ParseRequest>();
        //    while (_parseQueue.TryDequeue(out var request))
        //    {
        //        // Keep only ForceAllFiles requests
        //        if (request.Mode == ParseMode.ForceAllFiles)
        //        {
        //            tempQueue.Enqueue(request);
        //        }
        //    }
        //    // Put back the ForceAllFiles requests (if any)
        //    while (tempQueue.TryDequeue(out var request))
        //    {
        //        _parseQueue.Enqueue(request);
        //    }
        //}

        ///// <summary>
        ///// Internal implementation that performs the actual parsing.
        ///// Returns parsed files and include files for non-ForceAllFiles modes.
        ///// Creates a CancellationTokenSource that can be used to cancel the parse operation.
        ///// </summary>
        //private static async Task<(List<IVerilogRelatedFile> files, List<IVerilogRelatedFile> includeFiles)?> ParseInternalAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
        //{
        //    ConcurrentDictionary<string, IVerilogRelatedFile> filesDict = new ConcurrentDictionary<string, IVerilogRelatedFile>();
        //    ConcurrentDictionary<string, IVerilogRelatedFile> includeFilesDict = new ConcurrentDictionary<string, IVerilogRelatedFile>();

        //    // Create a new CancellationTokenSource for this parse operation
        //    CancellationTokenSource cts = new CancellationTokenSource();

        //    // Store the CTS and current parse mode so it can be cancelled by a subsequent request
        //    lock (_ctsLock)
        //    {
        //        // Dispose previous CTS if it was cancelled and not yet disposed
        //        if (_currentParseCts != null && _currentParseCts.IsCancellationRequested)
        //        {
        //            _currentParseCts.Dispose();
        //            _currentParseCts = null;
        //        }
        //        _currentParseCts = cts;
        //        _currentParseMode = parseMode;
        //    }

        //    try
        //    {
        //        await runParallelAsync(textFile, parseMode, filesDict, includeFilesDict, cts.Token);
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        CodeEditor2.Controller.AppendLog("Parse operation cancelled", Avalonia.Media.Colors.Orange);
        //    }
        //    catch (Exception ex)
        //    {
        //        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
        //        CodeEditor2.Controller.AppendLog("# Exception : " + ex.Message, Avalonia.Media.Colors.Red);
        //    }
        //    finally
        //    {
        //        // Clear the CTS reference after the parse completes
        //        // Only clear if this is still the current CTS (hasn't been replaced by a new parse)
        //        lock (_ctsLock)
        //        {
        //            if (_currentParseCts == cts)
        //            {
        //                _currentParseCts = null;
        //                _currentParseMode = ParseMode.ThisFileOnly;
        //            }
        //        }
        //        cts.Dispose();
        //    }

        //    return (filesDict.Values.ToList(), includeFilesDict.Values.ToList());
        //}

        public record ParseTask(
            string Id,
            CodeEditor2.Data.TextFile tarfgetTextFile,
            bool topLevel = false
            );

        private static async Task runParallelAsync(
            CodeEditor2.Data.TextFile textFile, 
            ParseMode parseMode,
            //ConcurrentDictionary<string, IVerilogRelatedFile> files,
            //ConcurrentDictionary<string, IVerilogRelatedFile> includeFiles,
            CancellationToken? token)
        {
            textFile.ReparseRequested = true;

            var workQueue = new ConcurrentQueue<ParseTask>();
            var reparseTargetFiles = new ConcurrentStack<CodeEditor2.Data.TextFile>();

            var completeIds = new ConcurrentDictionary<string, bool>();

            int workerCount = Environment.ProcessorCount;
            if (workerCount > 2) workerCount--;

            // 終了処理に使用する。
            int activeTaskCount = 0;

            // top fileをentry
            ParseTask task = new ParseTask(Id: textFile.Key, tarfgetTextFile: textFile, topLevel: true);
            EnqueueWork(task, workQueue, completeIds);

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

                        if (workQueue.TryDequeue(out var newTask))
                        {
                            Interlocked.Increment(ref activeTaskCount);

                            await parseDownwardAsync(index, newTask, reparseTargetFiles, workQueue, completeIds, parseMode, token);

                            var currentCount = Interlocked.Decrement(ref activeTaskCount);
                            if (currentCount == 0 && workQueue.IsEmpty)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if(token != null)
                            {
                                await Task.Delay(10, (System.Threading.CancellationToken)token);
                            }
                            else
                            {
                                await Task.Delay(1);
                            }
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
                await parseUpwardAsync(tfile,parseMode, token);
                token?.ThrowIfCancellationRequested();
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
            ConcurrentDictionary<string, bool> completeIds
            )
        {
            if (completeIds.TryAdd(parse.Id, true))
            {
                workQueue.Enqueue(parse);
            }
        }


        ///上位から下層にむけてのparse、並列に動作する
        private static async Task parseDownwardAsync(
            int index,
            ParseTask task,
            ConcurrentStack<CodeEditor2.Data.TextFile> reparseTargetFiles,
            ConcurrentQueue<ParseTask> workQueue,
            ConcurrentDictionary<string, bool> completeIds,
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

                // create fil e& include list
                Verilog.ParsedDocument? parsedDocument = parser.ParsedDocument as Verilog.ParsedDocument;

                if(parsedDocument !=null)
                {
                    await verilogFile.AcceptParsedDocumentAsync(parser);

                    //files.AddOrUpdate(verilogFile.RelativePath, verilogFile, (key, oldItem) => { return verilogFile; });
                    //foreach (var include in parsedDocument.IncludeFiles.Values)
                    //{
                    //    includeFiles.AddOrUpdate(include.RelativePath, include, (key, oldItem) => { return include; });
                    //}
                }
            }

            bool needReparse = false;
            if (verilogFile.ReparseRequested) needReparse = true;
            if (verilogFile.VerilogParsedDocument != null && verilogFile.VerilogParsedDocument.ErrorCount != 0) needReparse = true;

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
                    EnqueueWork(newTask, workQueue, completeIds);
                }
            }
        }

        // 下層から上層に向けての再parse。
        private static async Task parseUpwardAsync
            (CodeEditor2.Data.TextFile textFile,
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
                await verilogFile.AcceptParsedDocumentAsync(parser);
            }
        }

    }
}
