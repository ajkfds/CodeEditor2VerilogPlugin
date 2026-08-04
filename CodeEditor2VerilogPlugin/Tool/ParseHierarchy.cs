using Avalonia.Threading;
using CodeEditor2.Data;
using Microsoft.Extensions.AI;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
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

            if (verilogFile.VerilogParsedDocument != null)
            {
                foreach (string elementName in verilogFile.VerilogParsedDocument.ReferencedUnitNameSpace)
                {
                    pluginVerilog.ProjectProperty projectProperty = (ProjectProperty)verilogFile.Project.ProjectProperties[pluginVerilog.Plugin.StaticID];
                    TextFile? vFile = projectProperty.GetFileOfDefinitionNameSpace(elementName) as TextFile;
                    if (vFile == null) continue;
                    ParseTask newTask = new ParseTask(Id: vFile.ID, tarfgetTextFile: vFile);
                    EnqueueWork(newTask, workQueue, completeIds);

                }
            }

            // For @scope comment annotations, the referenced BuildingBlock may
            // live in a different file. That file is not reachable via the
            // `verilogFile.Items` chain above (which only descends into module
            // instances defined in this file), so we explicitly enqueue the
            // file that owns the @scope target. This makes the referenced
            // BuildingBlock get parsed, so that VirtualScopeNameSpace can
            // resolve identifiers from it.
            //
            // Skipped in ThisFileOnly mode because @scope references are
            // inherently cross-file and would need a separate parse pass.
            if (parseMode != ParseMode.ThisFileOnly
                && verilogFile is Data.VerilogFile scopeOwnerFile
                && scopeOwnerFile.VerilogParsedDocument?.Root != null)
            {
                EnqueueScopeReferencedFiles(
                    scopeOwnerFile.VerilogParsedDocument.Root.BuildingBlocks.Values,
                    scopeOwnerFile.ProjectProperty,
                    workQueue,
                    completeIds);
            }
        }

        /// <summary>
        /// Walks the CommentScopeReferences declared on each supplied
        /// BuildingBlock and enqueues the file that owns the referenced
        /// BuildingBlock into the parse work queue. Used by
        /// parseDownwardAsync to make sure that `// @scope` targets
        /// (which are normally in a different file and therefore not
        /// reachable from the current file's instance hierarchy) are
        /// also parsed.
        /// </summary>
        private static void EnqueueScopeReferencedFiles(
            IEnumerable<pluginVerilog.Verilog.BuildingBlocks.BuildingBlock> buildingBlocks,
            ProjectProperty projectProperty,
            ConcurrentQueue<ParseTask> workQueue,
            ConcurrentDictionary<string, bool> completeIds)
        {
            if (projectProperty == null) return;

            // Track files we have already enqueued in this call so we don't
            // re-enqueue the same file repeatedly when several scope
            // references point at the same target.
            HashSet<CodeEditor2.Data.TextFile> enqueuedFiles = new HashSet<CodeEditor2.Data.TextFile>();


            foreach (var buildingBlock in buildingBlocks)
            {
                if (buildingBlock == null) continue;
                foreach (var scopeRef in buildingBlock.CommentScopeReferences)
                {
                    if (scopeRef == null) continue;
                    if (string.IsNullOrEmpty(scopeRef.BuildingBlockName)) continue;

                    // Look up the target file. We use GetFileOfBuildingBlock
                    // (rather than GetBuildingBlock) so that we enqueue the
                    // file even if the target BuildingBlock has not yet been
                    // parsed in this run -- the file parse is what causes
                    // the target to be registered. The File-side parse
                    // (VerilogFile.AcceptParsedDocumentAsync) will then
                    // RegisterBuildingBlock, after which the
                    // VirtualScopeNameSpace's late-binding lookup can find it.
                    Data.IVerilogRelatedFile? targetFile =
                        projectProperty.GetFileOfDefinitionNameSpace(scopeRef.BuildingBlockName);
                    if (targetFile == null) continue;

                    // The target file must be a TextFile for the parse queue
                    // (workers call verilogFile.Items etc. on it). VerilogFile
                    // is the only such implementation in the Verilog plugin;
                    // skip other types defensively.
                    if (!(targetFile is CodeEditor2.Data.TextFile targetTextFile)) continue;

                    if (!enqueuedFiles.Add(targetTextFile)) continue;

                    ParseTask newTask = new ParseTask(
                        Id: targetTextFile.Key,
                        tarfgetTextFile: targetTextFile);
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
