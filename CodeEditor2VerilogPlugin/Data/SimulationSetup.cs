using Avalonia;
using CodeEditor2.Data;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Data
{
    public class SimulationSetup
    {
        protected SimulationSetup() { }

        public string TopName;
        public IVerilogRelatedFile TopFile;
        public List<IVerilogRelatedFile> Files = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> IncludeFiles = new List<IVerilogRelatedFile>();
        public List<string> IncludePaths = new List<string>();

        public List<IVerilogRelatedFile> ImportFiles = new List<IVerilogRelatedFile>();

        public List<IVerilogRelatedFile> ClassFiles = new List<IVerilogRelatedFile>();

        public bool Failed = false;

        /// <summary>
        /// 循環参照 (相互参照) を持つクラスファイルに対して、
        /// シミュレーションコマンドに渡す前にファイル先頭に前置すべき
        /// "typedef class X;" 宣言のソーステキスト。
        /// key: 前方宣言が必要なファイル (IVerilogRelatedFile)
        /// value: ファイル先頭に前置するテキスト (改行区切り)
        ///
        /// シミュレーション backend (IcarusVerilogSimulation など) は、
        /// このマップを参照し、必要に応じてラッパー / prepend で
        /// typedef class 行を挿入すること。
        /// </summary>
        public Dictionary<IVerilogRelatedFile, string> RequiredForwardDeclarations = new Dictionary<IVerilogRelatedFile, string>();


        public List<string> ExternalLibraryPathList = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        public CodeEditor2.Data.Project Project;

        /// <summary>
        /// 参照している外部プロジェクトのサブセットアップ (参照関係の有向エッジ)。
        ///
        /// 実体 (1 プロジェクトにつき 1 セットアップ) は <see cref="ProjectSetups"/>
        /// が保持し、このリストは「この setup からどの外部プロジェクトを参照しているか」
        /// だけを表す。シミュレーション backend はこのエッジを分割コンパイル /
        /// ライブラリ参照の境界として使う。
        /// </summary>
        public List<ExternalProjectReference> ExternalProjectReferences = new List<ExternalProjectReference>();

        /// <summary>
        /// ルートセットアップへの参照。サブセットアップは生成時に設定され、
        /// ルートセットアップ自身は null。ルートは
        /// 「1 プロジェクトにつき 1 セットアップ」を保証するための
        /// <see cref="ProjectSetups"/> の所有者。
        /// </summary>
        public SimulationSetup? RootSetup = null;

        /// <summary>
        /// プロジェクトごとのセットアップ (ルートセットアップのみが実体を持つ)。
        ///
        /// 同じ外部プロジェクトを別の instance path から参照しても、
        /// そのプロジェクトのソース / include / package / class は 1 回だけ
        /// 収集される。instance path 単位の情報は
        /// <see cref="ExternalProjectReferences"/> の
        /// <see cref="ExternalProjectReference.InstancePath"/> が持つ。
        /// </summary>
        public Dictionary<CodeEditor2.Data.Project, SimulationSetup> ProjectSetups =
            new Dictionary<CodeEditor2.Data.Project, SimulationSetup>();

        public class ExternalProjectReference
        {
            public CodeEditor2.Data.Project Project;
            public SimulationSetup Setup;

            /// <summary>
            /// この外部プロジェクトを参照している instance path
            /// (root 直下なら instance 名、深い場合は "a.b.c")。
            /// 同一プロジェクトを複数箇所から参照した場合は、
            /// 最初に到達した path が残る。
            /// </summary>
            public string InstancePath;
        }



        /// <summary>
        /// ファイルが属するプロジェクトの絶対パスを返す。
        /// Files はプロジェクトを跨ぐため、トップの Project.GetAbsolutePath では
        /// 外部プロジェクトのファイルパスを解決できない。
        /// </summary>
        public static string? GetAbsolutePath(IVerilogRelatedFile file)
        {
            CodeEditor2.Data.Project? owner = null;
            try { owner = file.Project; } catch { owner = null; }
            if (owner == null) return null;
            try { return owner.GetAbsolutePath(file.RelativePath); } catch { return null; }
        }

        /// <summary>
        /// シミュレーションに渡すファイル一覧を依存順で返す。
        ///
        /// 外部プロジェクト (<c>// @project &lt;name&gt;</c>) のソースは
        /// <see cref="ExternalProjectReferences"/> のサブセットアップにのみ入るため、
        /// <see cref="Files"/> だけを列挙する backend では外部プロジェクトを
        /// instance した設計がコンパイルできない。backend はこのメソッド経由で
        /// 全ファイルを取得すること。
        /// 外部プロジェクトは先に、次に自プロジェクトの順 (自プロジェクトの
        /// module が外部プロジェクトの module を instance するため)。
        /// </summary>
        public List<IVerilogRelatedFile> GetAllFiles()
        {
            List<IVerilogRelatedFile> results = new List<IVerilogRelatedFile>();
            collectAllFiles(this, results, new List<SimulationSetup>());
            return results;
        }

        private static void collectAllFiles(SimulationSetup setup, List<IVerilogRelatedFile> results, List<SimulationSetup> visited)
        {
            if (visited.Contains(setup)) return;
            visited.Add(setup);

            foreach (ExternalProjectReference reference in setup.ExternalProjectReferences)
            {
                if (reference.Setup == null) continue;
                collectAllFiles(reference.Setup, results, visited);
            }
            foreach (IVerilogRelatedFile file in setup.Files)
            {
                if (!results.Contains(file)) results.Add(file);
            }
        }

        /// <summary>
        /// シミュレーションに渡す include ディレクトリ一覧
        /// (このプロジェクト + 外部プロジェクトのサブセットアップ)。
        /// </summary>
        public List<string> GetAllIncludePaths()
        {
            List<string> results = new List<string>();
            collectIncludePaths(this, results, new List<SimulationSetup>());
            return results;
        }

        private static void collectIncludePaths(SimulationSetup setup, List<string> results, List<SimulationSetup> visited)
        {
            if (visited.Contains(setup)) return;
            visited.Add(setup);

            foreach (string path in setup.IncludePaths)
            {
                if (!results.Contains(path)) results.Add(path);
            }
            foreach (ExternalProjectReference reference in setup.ExternalProjectReferences)
            {
                if (reference.Setup == null) continue;
                collectIncludePaths(reference.Setup, results, visited);
            }
        }

        public static async Task<SimulationSetup?> CreateAsync(pluginVerilog.Data.VerilogFile verilogFile)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Tool.ParseHierarchy.ParseAsync(verilogFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            SimulationSetup? setup = Create(verilogFile);
            return setup;
        }
        public static SimulationSetup? Create(pluginVerilog.Data.VerilogFile verilogFile)
        {
            if (verilogFile.VerilogParsedDocument == null) return null;
            if (verilogFile.VerilogParsedDocument.Root == null) return null;

            // get target top block
            BuildingBlock? buildingBlock = verilogFile.VerilogParsedDocument.Root.BuildingBlocks.Values.FirstOrDefault(
                    block => !(block is Class) && !(block is InterfaceClass) && !(block is Package));
            if (buildingBlock == null) return null;

            return Create(verilogFile, buildingBlock.Name);
        }

        /// <summary>
        /// Collect the simulation target files of one building block.
        ///
        /// <paramref name="file"/> may be a plain <see cref="VerilogFile"/> (base
        /// parse of the whole file) or a <see cref="VerilogModuleInstance"/> /
        /// <see cref="InterfaceInstance"/> which holds the INSTANCE parse (only the
        /// instantiated building block, with its parameter overrides applied).
        /// The instance parse is what ParseHierarchy re-parses and keeps
        /// registered, so callers must pass the instance when the target module
        /// was reached through an instantiation.
        ///
        /// <paramref name="buildingBlockName"/> selects the building block to
        /// walk; it is required because a file may declare several of them (the
        /// first non-class block of the base parse is not necessarily the
        /// instantiated one).
        /// </summary>
        public static SimulationSetup? Create(IVerilogRelatedFile file, string buildingBlockName)
        {
            SimulationSetup setup = new SimulationSetup();
            setup.TopFile = file;

            List<string> ids = new List<string>();

            Verilog.ParsedDocument? topParsedDocument = file.VerilogParsedDocument;
            if (topParsedDocument == null) return null;
            if (topParsedDocument.Root == null) return null;

            setup.Project = file.Project;

            // The root setup owns the per-project table. A sub-setup created by
            // Create() for an external project (the fallback path used when the
            // instance parse is not available yet) registers itself as its own
            // root so that getExternalProjectSetup() still yields one setup per
            // project inside that traversal.
            if (!setup.ProjectSetups.ContainsKey(file.Project))
            {
                setup.ProjectSetups.Add(file.Project, setup);
            }

            if (!topParsedDocument.Root.BuildingBlocks.ContainsKey(buildingBlockName)) return null;

            // The entry file of this setup must be in Files: the simulator
            // compiles SOURCE files, and nothing else adds the top file (the
            // recursion below only adds the files it reaches through
            // instantiations / references).
            IVerilogRelatedFile topSourceFile = getSourceFile(file);
            if (!setup.Files.Contains(topSourceFile)) setup.Files.Add(topSourceFile);

            setup.TopName = buildingBlockName;
            searchHier(file, setup.TopName, ids, setup, setup.TopName);

            List<IVerilogRelatedFile> targetClassFiles = setup.ClassFiles.ToList();

            // defensive iteration cap: normally the loop terminates when no new
            // class file is found; the cap guards against unexpected cycles
            int classSearchLoopCount = 0;
            while (true)
            {
                classSearchLoopCount++;
                if (classSearchLoopCount > 1000) break;

                List<IVerilogRelatedFile> newClassFiles = new List<IVerilogRelatedFile>();

                foreach (IVerilogRelatedFile classFile in targetClassFiles)
                {
                    // NOTE: keep this local name distinct from the "file"
                    // parameter of Create(): a lambda/loop local cannot shadow
                    // the enclosing method parameter (CS0136).
                    Data.VerilogFile? classVerilogFile = classFile as Data.VerilogFile;
                    if (classVerilogFile == null) continue;
                    Verilog.ParsedDocument? parsedDocument = classVerilogFile.VerilogParsedDocument;
                    if (parsedDocument == null) continue;
                    ProjectProperty? projectProperty = parsedDocument.ProjectProperty;
                    if (projectProperty == null) continue;
                    foreach (string className in parsedDocument.ReferencedUnitNameSpace)
                    {
                        IVerilogRelatedFile? newfile = projectProperty.UnitNameSpace.GetFile(className);
                        // exclude files already collected (in this batch or in
                        // setup.ClassFiles). Without the setup.ClassFiles check,
                        // circular class references (A -> B -> A) alternate the
                        // batches forever and this while(true) loop never ends
                        // (UI freeze / deadlock when invoked from the menu thread).
                        if (newfile != null
                            && !newClassFiles.Contains(newfile)
                            && !setup.ClassFiles.Contains(newfile))
                        {
                            newClassFiles.Add(newfile);
                        }
                    }
                }
                if (newClassFiles.Count == 0) break;
                foreach (IVerilogRelatedFile newClassFile in newClassFiles)
                {
                    if (!setup.ClassFiles.Contains(newClassFile))
                    {
                        setup.ClassFiles.Add(newClassFile);
                    }
                }

                targetClassFiles = newClassFiles;
            }

            // Reorder setup.Files based on class dependency graph
            // (extends / implements / references) so that referenced classes
            // are compiled before the referencing files.
            ReorderFilesByClassDependencies(setup);

            if (setup.Failed) return null;

            // Surface the unresolved references of this setup AND of every
            // external project sub-setup (a missing definition inside a
            // referenced project used to be ignored). The setup itself is still
            // returned: the caller decides (the log / UnfoundModules list the
            // missing names), and a stale entry recorded by an earlier parse
            // pass must not make the whole hierarchy unavailable.
            List<string> unfoundModules = new List<string>();
            collectUnfoundModules(setup, unfoundModules, new List<SimulationSetup>());
            foreach (string module in unfoundModules)
            {
                CodeEditor2.Controller.AppendLog(module + " unfound", Avalonia.Media.Colors.Red);
            }
            return setup;
        }

        /// <summary>
        /// Gather the unfound references of this setup and of every external
        /// project sub-setup (recursively). Each entry is prefixed with the name
        /// of the project that owns the sub-setup so the log shows where the
        /// missing definition belongs.
        /// </summary>
        private static void collectUnfoundModules(SimulationSetup setup, List<string> results, List<SimulationSetup> visited)
        {
            if (visited.Contains(setup)) return;
            visited.Add(setup);

            foreach (string module in setup.UnfoundModules)
            {
                string entry = setup.Project != null ? setup.Project.Name + ":" + module : module;
                if (!results.Contains(entry)) results.Add(entry);
            }
            foreach (ExternalProjectReference reference in setup.ExternalProjectReferences)
            {
                if (reference.Setup == null) continue;
                collectUnfoundModules(reference.Setup, results, visited);
            }
        }


        /// <summary>
        /// setup.Files のうち VerilogFile 同士の依存関係 (extends / implements / 参照) に基づいて
        /// コンパイル順を整列する。循環参照は typedef class 前方宣言で吸収する。
        /// </summary>
        private static void ReorderFilesByClassDependencies(SimulationSetup setup)
        {
            ReorderFilesByClassDependencies(setup, new List<SimulationSetup>(), "");
        }

        private static void ReorderFilesByClassDependencies(SimulationSetup setup, List<SimulationSetup> visited, string projectPath)
        {
            if (visited.Contains(setup)) return;
            visited.Add(setup);

            // align the external project sub-setups first so that the top-level
            // ordering below already sees their final file lists
            foreach (ExternalProjectReference reference in setup.ExternalProjectReferences)
            {
                if (reference.Setup == null) continue;
                string subPath = reference.Project != null ? reference.Project.Name : "";
                if (projectPath != "") subPath = projectPath + "/" + subPath;
                ReorderFilesByClassDependencies(reference.Setup, visited, subPath);
            }

            try
            {
                ClassFileOrderResolver.Result result = ClassFileOrderResolver.ResolveOrder(setup.Files, setup);
                if (result.OrderedFiles != null && result.OrderedFiles.Count > 0)
                {
                    setup.Files.Clear();
                    setup.Files.AddRange(result.OrderedFiles);
                }
                if (result.CyclicGroups != null)
                {
                    foreach (var group in result.CyclicGroups)
                    {
                        var names = new List<string>();
                        foreach (var f in group)
                        {
                            try { names.Add(f.RelativePath); } catch { names.Add(f.ID); }
                        }
                        CodeEditor2.Controller.AppendLog(
                            "class circular reference detected (typedef class will be inserted): " + string.Join(", ", names),
                            Avalonia.Media.Colors.Orange);
                    }
                }
                if (result.ForwardDeclarations != null && result.ForwardDeclarations.Count > 0)
                {
                    setup.RequiredForwardDeclarations = result.ForwardDeclarations;
                }
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog("class file order resolver failed: " + ex.Message, Avalonia.Media.Colors.Orange);
            }
        }

        private static void searchHier(IVerilogRelatedFile file, string buildingBlockName, List<string> ids, SimulationSetup setup, string path)
        {
            string visitedKey = file.Project.Name + ":" + file.ID + ":" + buildingBlockName;
            if (ids.Contains(visitedKey)) return;
            ids.Add(visitedKey);

            ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;
            if (parsedDocument.Project == null) return;

//            appendFile(file, ref setup, ids, path, buildingBlockName);

            // Report the unresolved instantiations of this file, but do NOT
            // stop the traversal here.
            //
            // ParsedDocument.UnfoundModules is de-duplicated on Add but never
            // cleared when the module later resolves (e.g. a forward reference
            // resolved by the second parse pass, or an instance inside a
            // non-generated "generate if" branch). Returning early therefore
            // dropped the WHOLE subtree of any file that ever recorded an
            // unfound module, even though the NavigatePanel (Updater) expands
            // the resolved instances of that same file. That is why modules
            // visible in the NavigatePanel were missing from the setup.
            //
            // The unresolved names are still surfaced (log + UnfoundModules)
            // so a genuinely missing definition is not silently ignored.
            foreach (string unfound in parsedDocument.UnfoundModules)
            {
                // A cross-project ("// @project <name>") reference can still be
                // listed here by a parse pass that ran before the target project
                // was parsed. The reference is valid as long as the definition
                // file resolves in the project that owns it, so it must not be
                // reported as a missing module file.
                if (isResolvableCrossProjectReference(parsedDocument, unfound)) continue;

                CodeEditor2.Controller.AppendLog("unfound " + unfound, Avalonia.Media.Colors.Red);
                addUnfoundModule(setup, file, unfound);
            }

            foreach (string external in parsedDocument.ExternalRefrenceModules)
            {
                if (file.ProjectProperty.ExtenralModuleLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralModuleLibraryPath[external];
                    if (!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
                if (file.ProjectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(external))
                {
                    string libPath = file.ProjectProperty.ExtenralPrimitiveLibraryPath[external];
                    if (!setup.ExternalLibraryPathList.Contains(libPath)) setup.ExternalLibraryPathList.Add(libPath);
                }
            }

            foreach (string className in parsedDocument.ReferencedUnitNameSpace)
            {
                // UnitNameSpace.GetFile() returns the FILE that declares the class
                // (not the Class building block itself), so resolve the Class from
                // the file's parsed document (same rule as RegisterNameSpace.Get).
                // Casting the file to Class always yielded null and every class
                // reference (e.g. "Packet") was wrongly reported as unfound.
                IVerilogRelatedFile? classFile = parsedDocument.ProjectProperty?.UnitNameSpace.GetFile(className);
                Class? class_ = null;
                if (classFile != null
                    && classFile.VerilogParsedDocument != null
                    && classFile.VerilogParsedDocument.Root != null
                    && classFile.VerilogParsedDocument.Root.NamedElements.TryGetValue(className, out INamedElement? element))
                {
                    class_ = element as Class;
                }
                if (class_ != null)
                {
                    appendClass(file, className, parsedDocument.Project, setup, ids);
                }
                else
                {
                    // A class name can be recorded by a parse pass that ran before
                    // the declaring file was parsed, and the entry is never
                    // cleared. Aborting the traversal here dropped the WHOLE
                    // subtree of the file (the same failure mode as the stale
                    // UnfoundModules early return). Report it and keep walking;
                    // Create() refuses the setup when UnfoundModules is not empty.
                    CodeEditor2.Controller.AppendLog("unfound " + className, Avalonia.Media.Colors.Red);
                    addUnfoundModule(setup, file, className);
                }
            }

            foreach (var ifile in parsedDocument.IncludeFiles.Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }

            // module / interface / program / udp references are registered to
            // ReferencedDefinitionNameSpaceProjects during parse. Collect their definition
            // files here; a missing definition is reported as file shortage.

            //foreach(var reference in parsedDocument.DefinitionNameSpaceRefs.Values)
            //{
                

            //}

            foreach (VerilogRelatedFileReference verilogRelatedFileReference in parsedDocument.DefinitionNameSpaceRefs.Values)
            {
                string projectName = verilogRelatedFileReference.ProjectName;

                if (!CodeEditor2.Global.Projects.TryGetValue(projectName, out CodeEditor2.Data.Project? ownerProject))
                {
                    CodeEditor2.Controller.AppendLog("referenced project not found: " + projectName, Avalonia.Media.Colors.Red);
                    setup.Failed = true;
                    continue;
                }

                // The definition file is compiled as part of the project that
                // declares it. A cross-project (// @project <name>) reference
                // therefore belongs to that project's sub-setup; putting it in
                // this project's Files would compile it into the wrong library.
                SimulationSetup ownerSetup = ownerProject == setup.Project
                    ? setup
                    : getExternalProjectSetup(setup, ownerProject);

                IVerilogRelatedFile? defFile = verilogRelatedFileReference.GetTargetFile();
                if (defFile == null)
                {
                    // recorded reference that no longer resolves (file removed
                    // from the project): surface it instead of silently skipping
                    addUnfoundModule(ownerSetup, file, verilogRelatedFileReference.RelativeFilePath);
                    continue;
                }

                IVerilogRelatedFile defSourceFile = getSourceFile(defFile);
                if (!ownerSetup.Files.Contains(defSourceFile)) ownerSetup.Files.Add(defSourceFile);
            }

            // Process imported packages
            foreach (string packageName in parsedDocument.ImportedPackages)
            {
                appendImportedPackage(packageName, file.Project, setup, ids);
            }

            if (!parsedDocument.Root.BuildingBlocks.TryGetValue(buildingBlockName, out BuildingBlock? buildingBlock))
            {
                return;
            }

            searchNameSpace(file, ids, buildingBlock, setup, path);
        }

        private static void searchNameSpace(IVerilogRelatedFile file, List<string> ids, NameSpace nameSpace, SimulationSetup setup, string path)
        {
            foreach (INamedElement element in nameSpace.NamedElements.Values)
            {
                if (element is NameSpace ns && ns.IsVirtualScope)
                {
                    // @scope comment references are NOT real instances and must not be collected for simulation.
                    // The target file is already collected via its own ModuleInstantiation chain.
                    continue;
                }
                if (element is NameSpace)
                {
                    NameSpace subNameSpace = (NameSpace)element;
                    string newPath = path + "." + subNameSpace.Name;
                    searchNameSpace(file, ids, subNameSpace, setup, newPath);
                }
                else if (element is ModuleInstantiation)
                {
                    ModuleInstantiation moduleInstantiation = (ModuleInstantiation)element;
                    if (nameSpace.BuildingBlock.Project.Name != moduleInstantiation.SourceProjectName)
                    {
                        string sourceProjectName = moduleInstantiation.SourceProjectName;
                        string newPath = path + "." + moduleInstantiation.Name;

                        if (!CodeEditor2.Global.Projects.ContainsKey(sourceProjectName))
                        {
                            CodeEditor2.Controller.AppendLog("external project not found: " + sourceProjectName, Avalonia.Media.Colors.Red);
                            setup.Failed = true;
                            return;
                        }
                        CodeEditor2.Data.Project newProject = CodeEditor2.Global.Projects[sourceProjectName];
                        ProjectProperty? newProperty = newProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;

                        // Resolve the INSTANCE first (it holds the parameter-overridden
                        // parse produced by ParseHierarchy), then the definition file.
                        //
                        // Create(VerilogFile) used to be called with the definition
                        // file, so the external sub-setup walked the base parse: it
                        // picked the first building block of the file (not
                        // necessarily the instantiated one) and used the DEFAULT
                        // parameter values, so modules selected by an overridden
                        // "generate if" were missing from the sub-setup.
                        VerilogModuleInstance? externalInstance = findModuleInstance(file, moduleInstantiation);
                        IVerilogRelatedFile? externalTarget = null;
                        if (externalInstance != null && externalInstance.VerilogParsedDocument != null)
                        {
                            externalTarget = externalInstance;
                        }
                        else
                        {
                            externalTarget = newProperty?.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName) as VerilogFile;
                        }

                        if (externalTarget != null)
                        {
                            // Collect into the SHARED sub-setup of that project
                            // (one setup per project) instead of building a whole
                            // new setup per instance. Two instance paths that
                            // reference the same project then contribute to the
                            // same compile unit, and the reference edge records
                            // which instance path introduced it.
                            SimulationSetup subSetup = getExternalProjectSetup(setup, newProject);
                            if (subSetup.TopFile == null)
                            {
                                subSetup.TopFile = externalTarget;
                                subSetup.TopName = moduleInstantiation.SourceName;
                            }

                            // The external project's source file is a compile unit
                            // of that project, so it always goes into the
                            // sub-setup's Files (searchHier only records the files
                            // it reaches through references).
                            IVerilogRelatedFile externalSourceFile = getSourceFile(externalTarget);
                            if (!subSetup.Files.Contains(externalSourceFile)) subSetup.Files.Add(externalSourceFile);

                            // The sub-setup walks the INSTANCE parse (parameter
                            // overrides applied) so that the modules selected by
                            // an overridden "generate if" are the ones collected.
                            searchHier(externalTarget, moduleInstantiation.SourceName, ids, subSetup, newPath);

                            // record the instance path on the edge from this setup
                            ExternalProjectReference? edge = setup.ExternalProjectReferences.Find(r => r.Setup == subSetup);
                            if (edge != null) edge.InstancePath = newPath;
                        }
                    }
                    else
                    {
                        string newPath = moduleInstantiation.Name;
                        if (path != "") newPath = path + "." + newPath;

                        // Resolve the instantiated module's source file.
                        //
                        // file.Items only holds the internal instantiations when the
                        // source file declares exactly ONE building block
                        // (Updater.addSubItemsSingleBuldingBlock). For a file with
                        // several building blocks (e.g. a module plus a package, or a
                        // module plus a testbench module) file.Items holds one instance
                        // per building block and the internal instantiations live in
                        // those instances' Items. Looking the instance up in
                        // file.Items therefore failed and the module (and its whole
                        // subtree) was missing from the setup even though the
                        // NavigatePanel could expand it.
                        //
                        // Prefer the instance item when it is present (it carries the
                        // parameter-overridden parse), then the instance-array
                        // elements ("name[i]"), and finally resolve the definition
                        // file directly from the DefinitionNameSpace.
                        VerilogModuleInstance? moduleInstance = findModuleInstance(file, moduleInstantiation);

                        VerilogFile? sourceFile = moduleInstance?.SourceVerilogFile;
                        if (sourceFile == null)
                        {
                            sourceFile = file.ProjectProperty?.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName) as VerilogFile;
                        }

                        if (sourceFile != null)
                        {
                            // The definition FILE is what the simulator has to
                            // compile, so it is always collected.
                            if (!setup.Files.Contains(sourceFile)) setup.Files.Add(sourceFile);

                            // The dependencies must be walked on the INSTANCE
                            // parse, not on the definition file's base parse.
                            //
                            // A definition file may declare several building
                            // blocks (e.g. a module plus a package). ParseHierarchy
                            // re-parses (and keeps registered) the instance parse,
                            // whose Root only holds the instantiated building block
                            // and whose parameter overrides are applied. The base
                            // parse of sourceFile, in contrast, may have never been
                            // produced in this hierarchy pass (it is skipped when
                            // the file declares several building blocks, see
                            // VerilogModuleInstance.AcceptParsedDocumentAsync) or
                            // still carries the DEFAULT parameter values, so
                            // searchHier() found no parsed document / the wrong
                            // generate branches and the whole subtree was missing
                            // from the setup.
                            //
                            // VerilogModuleInstance.VerilogParsedDocument resolves
                            // the instance parsed document by key (module name +
                            // parameter overrides), exactly like the editor's
                            // NavigatePanel and the LSP RTL hierarchy do.
                            IVerilogRelatedFile target = sourceFile;
                            if (moduleInstance != null && moduleInstance.VerilogParsedDocument != null)
                            {
                                target = moduleInstance;
                            }
                            searchHier(target, moduleInstantiation.SourceName, ids, setup, newPath);
                        }
                    }
                }
                else if ((element is Verilog.Items.InterfaceInstance))
                {
                    Verilog.Items.InterfaceInstance moduleInstantiation = (Verilog.Items.InterfaceInstance)element;
                    string newPath = moduleInstantiation.Name;
                    if (path != "") newPath = path + "." + newPath;

                    // Same reasoning as the module instantiation branch above:
                    // file.Items only holds the internal instantiations when the
                    // source file declares exactly one building block, so fall back
                    // to resolving the interface definition file directly.
                    InterfaceInstance? interfaceInstance = null;
                    if (file.Items.TryGetValue(moduleInstantiation.Name, out CodeEditor2.Data.Item? item))
                    {
                        interfaceInstance = item as InterfaceInstance;
                    }

                    // The simulator compiles the definition FILE, never the
                    // instance wrapper, so setup.Files always receives the
                    // source file (adding the instance itself used to put an
                    // InstanceTextFile into the compile list).
                    VerilogFile? interfaceSourceFile = interfaceInstance?.SourceVerilogFile;
                    if (interfaceSourceFile == null)
                    {
                        interfaceSourceFile = file.ProjectProperty?.DefinitionNameSpace.GetFile(moduleInstantiation.SourceName) as VerilogFile;
                    }
                    if (interfaceSourceFile != null)
                    {
                        if (!setup.Files.Contains(interfaceSourceFile)) setup.Files.Add(interfaceSourceFile);

                        // Walk the INSTANCE parse (parameter overrides applied,
                        // only the instantiated interface parsed) instead of the
                        // definition file's base parse; see the module
                        // instantiation branch above for the full reasoning.
                        IVerilogRelatedFile target = interfaceSourceFile;
                        if (interfaceInstance != null && interfaceInstance.VerilogParsedDocument != null)
                        {
                            target = interfaceInstance;
                        }
                        searchHier(target, moduleInstantiation.SourceName, ids, setup, newPath);
                    }
                }
                else if (element is Verilog.Items.ProgramInstantiation)
                {
                    // program instantiation: collect the program definition file
                    // (definition lookup failure is reported by searchHier via
                    //  ReferencedDefinitionNameSpace, so nothing to do here)
                }
                else if (element is Verilog.Items.UdpInstantiation)
                {
                    // udp instantiation: collect the udp (primitive) definition file
                    // (definition lookup failure is reported by searchHier via
                    //  ReferencedDefinitionNameSpace, so nothing to do here)
                }
                else if (element is DataObject)
                {
                    // A variable declaration is the only place where a class /
                    // interface class / virtual interface dependency shows up
                    // without an instantiation. Its source file has to be
                    // compiled as well, otherwise the simulator sees an unknown
                    // type.
                    DataObject dataObject = (DataObject)element;
                    // Variables.Object holds BuildingBlocks.Class itself as DataType (not ClassType).
                    // UserDefinedVariable holds UserDefinedType (typedef of class / interface class).
                    if (dataObject.DataType is ClassType || dataObject.DataType is Class || dataObject.DataType is UserDefinedType)
                    {
                        appendClassInstance(file, dataObject, setup);
                    }
                    else if (dataObject.DataType is InterfaceClass)
                    {
                        appendInterfaceClassInstance(file, dataObject, setup);
                    }
                    else if (dataObject is Verilog.DataObjects.Variables.VirtualInterface)
                    {
                        // virtual interface: collect the interface definition file
                        appendVirtualInterfaceInstance(file, (Verilog.DataObjects.Variables.VirtualInterface)dataObject, setup);
                    }
                }
            }
        }


        /// <summary>
        /// append the given file to the appropriate list of the setup (or the
        /// external project sub-setup), then recurse into external project files
        /// so that their own dependencies are collected into the sub-setup.
        /// </summary>
        /// 
        /*
        private static void appendFile(IVerilogRelatedFile file, ref SimulationSetup setup, List<string> ids, string path, string buildingBlockName)
        {
            if (file is pluginVerilog.Data.VerilogFile || file is SystemVerilogFile)
            {
                if (setup.Files.Contains(file)) return;
                setup.Files.Add(file);
                return;
            }
            if (file is VerilogModuleInstance)
            {
                VerilogModuleInstance? instance = file as VerilogModuleInstance;
                if (instance == null) return;
                IVerilogRelatedFile? sourceFile = instance.SourceTextFile as IVerilogRelatedFile;
                if (sourceFile == null) return;

                if (sourceFile.Project == setup.Project)
                {
                    if (setup.Files.Contains(sourceFile)) return;
                    setup.Files.Add(sourceFile);
                }
                else
                {
                    CodeEditor2.Data.Project project = sourceFile.Project;
                    SimulationSetup pSetup;
                    if (!setup.ExternalProjectReferences.ContainsKey(project))
                    {
                        pSetup = new SimulationSetup() { Project = project };
                        pSetup.TopFile = instance.SourceVerilogFile as VerilogFile;
                        setup.ExternalProjectReferences.Add(project, pSetup);
                        pSetup.TopFile = instance.SourceVerilogFile;
                        pSetup.TopName = instance.ModuleName;
                    }
                    else
                    {
                        pSetup = setup.ExternalProjectReferences[project];
                    }
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    // recurse into the external project instance so that its own
                    // module / class / package dependencies are collected into the
                    // external project sub-setup as well.
                    // Note: do NOT return when the source file was already added.
                    // The same external file may be instantiated several times
                    // with different parameter overrides, and each instance has
                    // its own parsed document (hence its own dependencies).
                    appendExternalProjectDependencies(instance, sourceFile, pSetup, ids, path);

                    setup = pSetup;
                }
                return;
            }
            else if (file is InterfaceInstance)
            {
                InterfaceInstance? instance = file as InterfaceInstance;
                if (instance == null) return;
                IVerilogRelatedFile? sourceFile = instance.SourceTextFile as IVerilogRelatedFile;
                if (sourceFile == null) return;

                if (sourceFile.Project == setup.Project)
                {
                    if (setup.Files.Contains(sourceFile)) return;
                    setup.Files.Add(sourceFile);
                }
                else
                {
                    CodeEditor2.Data.Project project = sourceFile.Project;
                    SimulationSetup pSetup;
                    if (!setup.ExternalProjectReferences.ContainsKey(project))
                    {
                        pSetup = new SimulationSetup() { Project = project };
                        setup.ExternalProjectReferences.Add(project, pSetup);
                        pSetup.TopFile = instance.SourceVerilogFile;
                        pSetup.TopName = instance.ModuleName;
                    }
                    else
                    {
                        pSetup = setup.ExternalProjectReferences[project];
                    }
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    // recurse into the external project instance (same reasoning
                    // as the module instance above: several parameterized
                    // instances of one file have different dependencies).
                    appendExternalProjectDependencies(instance, sourceFile, pSetup, ids, path);
                }
                return;
            }
        }

        /// <summary>
        /// Walk the dependencies of an instance that belongs to another project
        /// and collect them into that project's sub-setup.
        ///
        /// The INSTANCE (not its source file) is traversed: an instance holds
        /// the parsed document produced with the parameter overrides of this
        /// particular instantiation (VerilogModuleInstance.ParsedDocument looks
        /// the instanced parsed document up by key, and its Items were built
        /// from that same document). Walking the source file instead used the
        /// base parse (default parameter values), so a parameterized external
        /// instance such as "@project X" + "#(.MODE(1))" collected the modules
        /// selected by the *default* value of MODE and missed the ones actually
        /// instantiated (e.g. inside "generate if (MODE == 1)").
        ///
        /// The source file is used only as a fallback when the instanced parsed
        /// document is not available yet (the instance has not been parsed),
        /// because then only the base parse exists.
        /// </summary>
        private static void appendExternalProjectDependencies(
            CodeEditor2.Data.Item instance,
            IVerilogRelatedFile sourceFile,
            SimulationSetup subSetup,
            List<string> ids,
            string path)
        {
            string buildingBlockName = getInstantiatedBuildingBlockName(instance, sourceFile);

            IVerilogRelatedFile? instanceFile = instance as IVerilogRelatedFile;
            if (instanceFile != null && instanceFile.VerilogParsedDocument != null)
            {
                string instancePath = instance.Name;
                if (path != "") instancePath = path + "." + instancePath;
                searchHier(instanceFile, buildingBlockName, ids, subSetup, instancePath);
                return;
            }

            // the instance has not been parsed yet: fall back to the base parse
            // of the source file so that at least its own dependencies are found
            searchHier(sourceFile, buildingBlockName, ids, subSetup, path);
        }

        /// <summary>
        /// Name of the building block (module / interface) an instance refers to.
        /// </summary>
        private static string getInstantiatedBuildingBlockName(CodeEditor2.Data.Item instance, IVerilogRelatedFile sourceFile)
        {
            if (instance is VerilogModuleInstance moduleInstance) return moduleInstance.ModuleName;
            if (instance is InterfaceInstance interfaceInstance) return interfaceInstance.ModuleName;
            return instance.Name ?? sourceFile.Name;
        }
        */
        private static void appendClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle Object (class instance) - has Class property
            if (dataObject is Verilog.DataObjects.Variables.Object objectInstance)
            {
                Class? class_ = objectInstance.GetSourceClass();
                if (class_ == null) return;

                appendClassFile(file, projectProperty, class_.Name, setup);
                return;
            }

            // Handle UserDefinedVariable with UserDefinedType
            if (dataObject is UserDefinedVariable userDefinedVariable)
            {
                if (userDefinedVariable.DataType is UserDefinedType userDefinedType)
                {
                    // typedef is registered to UnitNameSpace (compilation-unit scope), not DefinitionNameSpace
                    appendClassFile(file, projectProperty, userDefinedType.Typedef.Name, setup);
                }
            }
        }

        private static void appendInterfaceClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            // Handle InterfaceClass similarly - use UserDefinedType approach
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle UserDefinedVariable with UserDefinedType that references InterfaceClass
            if (dataObject is UserDefinedVariable userDefinedVariable)
            {
                if (userDefinedVariable.DataType is UserDefinedType userDefinedType)
                {
                    appendClassFile(file, projectProperty, userDefinedType.Typedef.Name, setup);
                }
            }
        }

        /// <summary>
        /// Collect the file that declares the given compilation-unit-scope type
        /// (class / interface class / typedef) referenced by a variable
        /// declaration. The declaring project owns the file, so a type from
        /// another project goes into that project's sub-setup.
        /// </summary>
        private static void appendClassFile(
            IVerilogRelatedFile referrer,
            ProjectProperty projectProperty,
            string typeName,
            SimulationSetup setup)
        {
            IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(typeName);
            if (sourceFile == null)
            {
                // the type is declared in a file that has not been parsed yet, or
                // the reference is genuinely missing: surface it (Create() then
                // refuses the setup) instead of silently dropping the file.
                addUnfoundModule(setup, referrer, typeName);
                return;
            }

            IVerilogRelatedFile source = getSourceFile(sourceFile);
            SimulationSetup ownerSetup = source.Project == setup.Project
                ? setup
                : getExternalProjectSetup(setup, source.Project);

            if (!ownerSetup.Files.Contains(source)) ownerSetup.Files.Add(source);
            if (!ownerSetup.ClassFiles.Contains(source)) ownerSetup.ClassFiles.Add(source);
        }

        private static void appendVirtualInterfaceInstance(IVerilogRelatedFile file, Verilog.DataObjects.Variables.VirtualInterface virtualInterface, SimulationSetup setup)
        {
            // collect the interface definition file referenced by a virtual interface variable.
            // missing interface definition is reported as file shortage so that simulation
            // startup is refused later in Create()
            Verilog.BuildingBlocks.Interface? sourceInterface = virtualInterface.GetSourceInterface();
            if (sourceInterface == null)
            {
                Verilog.DataObjects.DataTypes.VirtualInterfaceType? dataType = virtualInterface.DataType as Verilog.DataObjects.DataTypes.VirtualInterfaceType;
                string? interfaceName = dataType?.InterfaceIdentifier;
                if (!string.IsNullOrEmpty(interfaceName))
                {
                    addUnfoundModule(setup, file, interfaceName);
                }
                return;
            }

            ProjectProperty? declaringProjectProperty = file.ProjectProperty;
            if (declaringProjectProperty == null) return;

            IVerilogRelatedFile? interfaceFile = declaringProjectProperty.DefinitionNameSpace.GetFile(sourceInterface.Name);
            if (interfaceFile == null)
            {
                addUnfoundModule(setup, file, sourceInterface.Name);
                return;
            }

            IVerilogRelatedFile interfaceSource = getSourceFile(interfaceFile);
            SimulationSetup ownerSetup = interfaceSource.Project == setup.Project
                ? setup
                : getExternalProjectSetup(setup, interfaceSource.Project);
            if (!ownerSetup.Files.Contains(interfaceSource)) ownerSetup.Files.Add(interfaceSource);
        }
        /// <summary>
        /// The source file that owns <paramref name="file"/>.
        ///
        /// An instance (VerilogModuleInstance / InterfaceInstance /
        /// ImportedPackage / VerilogHeaderInstance) is a view over its source
        /// file: it shares the source's CodeDocument and parsed documents. The
        /// simulator must compile the SOURCE file, never the instance wrapper
        /// (an InstanceTextFile has no path of its own), so every place that
        /// puts something into setup.Files / IncludeFiles goes through here.
        /// </summary>
        private static IVerilogRelatedFile getSourceFile(IVerilogRelatedFile file)
        {
            if (file is InstanceTextFile instance)
            {
                IVerilogRelatedFile? source = instance.SourceTextFile as IVerilogRelatedFile;
                if (source != null) return source;
            }
            return file;
        }

        /// <summary>
        /// Find the instance item that a module instantiation produced.
        ///
        /// The instance carries the parameter-overridden parse, so it — not the
        /// definition file's base parse — must be used to walk the dependencies.
        /// Lookup order:
        ///   1. the plain instantiation name (a resolved instance registered by
        ///      the Updater),
        ///   2. the instance-array elements ("name[i]"), because
        ///      VerilogModuleInstance.CreateArray registers the elements while the
        ///      ModuleInstantiation itself is keyed by the plain name.
        /// Every array element resolves the same module with the same parameter
        /// overrides, so one element is representative.
        /// Returns null when the instantiation has not been expanded (e.g. the
        /// module was not resolved), in which case the caller falls back to the
        /// definition file.
        /// </summary>
        private static VerilogModuleInstance? findModuleInstance(IVerilogRelatedFile file, ModuleInstantiation moduleInstantiation)
        {
            VerilogModuleInstance? moduleInstance = null;
            try
            {
                if (file.Items.TryGetValue(moduleInstantiation.Name, out CodeEditor2.Data.Item? item))
                {
                    moduleInstance = item as VerilogModuleInstance;
                }
                if (moduleInstance == null && moduleInstantiation.InstanceRange != null)
                {
                    int count = moduleInstantiation.InstanceCount;
                    for (int i = 0; i < count; i++)
                    {
                        string elementName = moduleInstantiation.Name + "[" + i.ToString() + "]";
                        if (!file.Items.TryGetValue(elementName, out CodeEditor2.Data.Item? arrayItem)) continue;
                        moduleInstance = arrayItem as VerilogModuleInstance;
                        if (moduleInstance != null) break;
                    }
                }
            }
            catch
            {
                return null;
            }
            return moduleInstance;
        }

        private static void appendVerilogHeaderInstance(VerilogHeaderInstance file, SimulationSetup setup)
        {
            // An include header may live in another project (a shared header
            // directory referenced from an external project's sources). It then
            // belongs to that project's sub-setup, otherwise the backend never
            // receives its include path.
            SimulationSetup ownerSetup = setup;
            if (file.Project != setup.Project)
            {
                ownerSetup = getExternalProjectSetup(setup, file.Project);
            }

            if (ownerSetup.IncludeFiles.Contains(file)) return;
            ownerSetup.IncludeFiles.Add(file);
            string? path = System.IO.Path.GetDirectoryName(file.Project.GetAbsolutePath(file.RelativePath));
            if (path == null) return;
            if (!ownerSetup.IncludePaths.Contains(path)) ownerSetup.IncludePaths.Add(path);
        }
        
        private static void appendImportedPackage(string packageName, CodeEditor2.Data.Project project, SimulationSetup setup, List<string> ids)
        {
            // Search for the package file in the project
            IVerilogRelatedFile? packageFile = findPackageFile(packageName, project, setup);
            if (packageFile == null) return;

            // The package file belongs to the project that declares it. When it
            // is imported from another project it has to go into that project's
            // sub-setup, otherwise the backend compiles the import without the
            // package source (and its own dependencies).
            SimulationSetup ownerSetup = packageFile.Project == setup.Project
                ? setup
                : getExternalProjectSetup(setup, packageFile.Project);

            if (ownerSetup.ImportFiles.Contains(packageFile)) return;
            ownerSetup.ImportFiles.Add(packageFile);
            // A package is a source file of the compile list as well: the
            // simulator has to compile it, ImportFiles only records the role.
            IVerilogRelatedFile packageSourceFile = getSourceFile(packageFile);
            if (!ownerSetup.Files.Contains(packageSourceFile)) ownerSetup.Files.Add(packageSourceFile);

            // package file may itself reference other packages / classes: trace them
            // (also reports missing references in the package as file shortage)
            searchHier(packageFile, packageName, ids, ownerSetup, "");
        }
        
        private static void appendClass(IVerilogRelatedFile referrer, string className, CodeEditor2.Data.Project project, SimulationSetup setup, List<string> ids)
        {
            IVerilogRelatedFile? classFile = findClassFile(className, project, setup);
            if (classFile == null)
            {
                // referenced class definition file is missing: report as file shortage
                setup.Failed = true;
                return;
            }

            // Same rule as appendImportedPackage: the class file is compiled as
            // part of the project that declares it, so a class declared in
            // another project goes into that project's sub-setup.
            SimulationSetup ownerSetup = classFile.Project == setup.Project
                ? setup
                : getExternalProjectSetup(setup, classFile.Project);

            if (ownerSetup.ClassFiles.Contains(classFile)) return;
            ownerSetup.ClassFiles.Add(classFile);
            IVerilogRelatedFile classSourceFile = getSourceFile(classFile);
            if (!ownerSetup.Files.Contains(classSourceFile)) ownerSetup.Files.Add(classSourceFile);

            // class file may itself reference other classes / packages: trace them
            // (also reports missing references in the class file as file shortage)
            searchHier(classFile, className, ids, ownerSetup, "");
        }

        /// <summary>
        /// Get (or create) the sub-setup that collects the files of the given
        /// external project, and record the reference edge from
        /// <paramref name="setup"/> to it.
        ///
        /// The sub-setup is cached on the ROOT setup (one sub-setup per
        /// project), so two different instance paths that reference the same
        /// external project share one compile unit instead of splitting its
        /// file list. The reference edge is recorded on the setup that actually
        /// contains the reference, because the backends use it to decide the
        /// compile / library boundary.
        /// </summary>
        private static SimulationSetup getExternalProjectSetup(SimulationSetup setup, CodeEditor2.Data.Project project)
        {
            SimulationSetup root = setup.RootSetup ?? setup;

            SimulationSetup subSetup;
            if (!root.ProjectSetups.TryGetValue(project, out SimulationSetup? existing))
            {
                subSetup = new SimulationSetup()
                {
                    Project = project,
                    RootSetup = root,
                    ProjectSetups = root.ProjectSetups,
                };
                root.ProjectSetups.Add(project, subSetup);
            }
            else
            {
                subSetup = existing;
            }

            if (setup != subSetup && !setup.ExternalProjectReferences.Exists(r => r.Setup == subSetup))
            {
                setup.ExternalProjectReferences.Add(new ExternalProjectReference()
                {
                    Project = project,
                    Setup = subSetup,
                    InstancePath = "",
                });
            }
            return subSetup;
        }

        /// <summary>
        /// Register a reference that could not be resolved as
        /// "RelativePath:Name" so that the log shows where it is referenced from.
        /// </summary>
        private static void addUnfoundModule(SimulationSetup setup, IVerilogRelatedFile file, string name)
        {
            string entry;
            try { entry = file.RelativePath + ":" + name; }
            catch { entry = file.ID + ":" + name; }
            if (!setup.UnfoundModules.Contains(entry)) setup.UnfoundModules.Add(entry);
        }

        /// <summary>
        /// True when the given name (recorded as unfound by some parse pass) is a
        /// cross-project ("// @project &lt;name&gt;") reference whose definition file
        /// resolves in the project that owns it.
        ///
        /// Such a name is not a missing module file: the entry is written by a
        /// parse pass that ran before the target project was parsed, and
        /// ParsedDocument.UnfoundModules is de-duplicated on Add but never
        /// cleared on a later successful pass.
        /// </summary>
        private static bool isResolvableCrossProjectReference(ParsedDocument parsedDocument, string definitionName)
        {
            CodeEditor2.Data.Project? ownProject = parsedDocument.Project;
            if (ownProject == null) return false;

            // The name is not missing as long as some registered project declares
            // it. The stale entry comes from a parse pass that ran before the
            // owning project was parsed; a definition that exists anywhere in the
            // workspace means the reference itself is valid.
            foreach (CodeEditor2.Data.Project project in CodeEditor2.Global.Projects.Values)
            {
                if (project == null || project == ownProject) continue;
                ProjectProperty? property = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (property == null) continue;
                if (property.DefinitionNameSpace.GetFile(definitionName) != null) return true;
            }
            return false;
        }

        private static IVerilogRelatedFile? findPackageFile(string packageName, CodeEditor2.Data.Project project, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return null;

            //            Package? package = projectProperty.PackageNameSpace.Get(packageName) as Package;
            IVerilogRelatedFile? verilogRelatedFile = projectProperty.PackageNameSpace.GetFile(packageName);
            return verilogRelatedFile;
        }
        
        private static IVerilogRelatedFile? findClassFile(string className, CodeEditor2.Data.Project project, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
            if (projectProperty == null) return null;

            //            Class? class_ = projectProperty.UnitNameSpace.Get(className) as Class;
            IVerilogRelatedFile? verilogRelatedFile = projectProperty.UnitNameSpace.GetFile(className);
            return verilogRelatedFile;
        }

    }
}
