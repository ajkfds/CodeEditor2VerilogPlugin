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
using System. Collections. Generic;
using System.Globalization;
using System. Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog. Data
{
    /// <summary>
    /// シミュレーション (コンパイル) に渡すソース一式を、プロジェクト単位に集めたもの。
    ///
    /// 2 つの役割を持つ。
    /// 1. モジュール名が衝突する複数の module instance を区別して扱うための情報を保持する
    ///    (同名 module でも instance ごとに parameter override が異なるため、
    ///     instance 単位で依存を辿る必要がある。→ ExternalProjectEntryInstance /
    ///     UnfoundModules などは instance path 付きで記録する)。
    /// 2. プロジェクトごとの分割コンパイルを行い、外部プロジェクトをライブラリとして
    ///    参照するための情報を保持する (→ ExternalProjectReferences / ProjectSetups /
    ///     ExternalLibraryPathList)。
    ///
    /// 1 プロジェクトにつき 1 インスタンス。共有レジストリ ProjectSetups が
    /// プロジェクト → セットアップの対応を保持し、外部プロジェクト参照は
    /// ExternalProjectReferences が「参照している」方向だけを保持する。
    /// </summary>
    public class SimulationSetup
    {
        protected SimulationSetup() { }

        /// <summary>
        /// このセットアップのシミュレーション対象 (トップ) building block 名。
        /// ルートではトップファイル内で最初に宣言された module / interface など、
        /// 外部プロジェクトのセットアップではそのプロジェクトで最初に instance
        /// された building block 名が入る。
        /// </summary>
        public string TopName;

        /// <summary>
        /// トップ (シミュレーションの起点) となるソースファイル。
        /// ルートではシミュレーション実行対象のファイル 1 つ。
        /// 外部プロジェクトのセットアップでは、そのプロジェクトが最初に
        /// instance されたときのソースファイル。
        /// </summary>
        public List<VerilogFile> TopFiles = new List<VerilogFile>();

        /// <summary>
        /// このプロジェクトのコンパイル対象ソースファイル (依存順)。
        ///
        /// instance 先のファイルは、そのプロジェクトのセットアップ側の Files に
        /// 入る (他プロジェクトのファイルはここには入らない)。VerilogModuleInstance
        /// のような instance ファイルではなく、その実体のソースファイルが入る。
        /// 同じファイルを parameter 違いで複数回 instance しても 1 回だけ登録される。
        /// </summary>
        public List<IVerilogRelatedFile> Files = new List<IVerilogRelatedFile>();

        /// <summary>
        /// `include で取り込まれるヘッダファイル (このプロジェクト所有分)。
        /// コンパイル対象のファイル一覧とは別管理で、backend は
        /// IncludePaths と併せて include 探索に使う。
        /// </summary>
        public List<IVerilogRelatedFile> IncludeFiles = new List<IVerilogRelatedFile>();

        /// <summary>
        /// include 探索ディレクトリ (+incdir)。IncludeFiles の存在する
        /// ディレクトリが登録される。外部プロジェクト分はそのプロジェクトの
        /// セットアップ側に登録される。
        /// </summary>
        public List<string> IncludePaths = new List<string>();

        /// <summary>
        /// import される package の定義ファイル (このプロジェクト所有分)。
        /// package はコンパイル順序に制約があるため Files とは別に保持し、
        /// backend は Files より先に渡す。
        /// </summary>
        public List<IVerilogRelatedFile> ImportFiles = new List<IVerilogRelatedFile>();

        /// <summary>
        /// 参照される class の定義ファイル (このプロジェクト所有分)。
        /// class は宣言順・継承関係に依存するため Files とは別に保持し、
        /// 依存関係に基づいて整列したうえで backend に渡す。
        /// </summary>
        public List<IVerilogRelatedFile> ClassFiles = new List<IVerilogRelatedFile>();

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


        /// <summary>
        /// 外部ライブラリとして参照するモジュール / primitive のソースパス。
        ///
        /// プロジェクト外のライブラリ (ExtenralModuleLibraryPath /
        /// ExtenralPrimitiveLibraryPath) で提供されるモジュールは、プロジェクトの
        /// ソースファイルとしては収集されない。代わりにこのリストにライブラリの
        /// ファイルパスが入り、backend は分割コンパイル時にライブラリとして
        /// 渡す。外部プロジェクトのセットアップにも同じ情報が入りうるため、
        /// backend は GetAllProjectSetups() 経由で全プロジェクト分を集約して使う。
        /// </summary>
        public List<string> ExternalLibraryPathList = new List<string>();

        /// <summary>
        /// 参照先の定義ファイルが見つからなかったもの (ファイル欠落)。
        /// エントリは "RelativePath:Name" 形式で、どのファイルのどの参照が
        /// 解決できなかったのかが分かる形で記録する。
        ///
        /// モジュール名が衝突する複数 instance を扱うため、同名 module でも
        /// instance ごとに解決結果が異なりうる。そのためセットアップ単位で
        /// 記録し、collectUnfoundModules() がプロジェクト名を前置して集約する。
        /// 1 件でもあれば Create() は null を返し simulation 起動は拒否される。
        /// </summary>
        public List<string> UnfoundModules = new List<string>();

        /// <summary>
        /// このセットアップが担当するプロジェクト。
        /// ルートではシミュレーション実行対象のプロジェクト、サブセットアップでは
        /// その外部プロジェクト。Files / IncludePaths などはすべてこのプロジェクトを
        /// 基準に収集される (プロジェクトを跨ぐファイルは各プロジェクトの
        /// セットアップ側に入る)。
        /// </summary>
        public CodeEditor2. Data. Project Project;

        /// <summary>
        /// このセットアップ自身のルート (トップ) セットアップ。
        /// ルートは自分自身を指す。ProjectSetups はルートだけが実体を持ち、
        /// サブセットアップは同じ Dictionary を共有する。
        /// </summary>
        public SimulationSetup RootSetup = null!;

        /// <summary>
        /// 「1 プロジェクトにつき 1 つの SimulationSetup」を保証する共有レジストリ。
        ///
        /// ルートセットアップ (Create) が自分自身を登録して生成し、外部プロジェクトの
        /// サブセットアップはすべて同じインスタンスを共有する。以前はサブセットアップを
        /// 参照元 setup ごとに生成していたため、同じプロジェクトを複数の箇所から
        /// 参照すると同一プロジェクトのファイル一覧が複数に分裂し、
        /// UnfoundModules / RequiredForwardDeclarations / ClassFiles などが
        /// セットアップごとにずれていた。
        /// </summary>
        public Dictionary<CodeEditor2. Data. Project, SimulationSetup> ProjectSetups = new Dictionary<Project, SimulationSetup>();

        /// <summary>
        /// 指定プロジェクトのセットアップを取得する。
        ///
        /// 未登録 (このセットアップ木がまだそのプロジェクトを参照していない) なら
        /// null。ProjectSetups はルートで共有されているため、どのセットアップから
        /// 呼んでも同じ結果になる。
        /// </summary>
        public SimulationSetup? GetProjectSetup(CodeEditor2.Data.Project project)
        {
            if (ProjectSetups.TryGetValue(project, out SimulationSetup? setup)) return setup;
            return null;
        }

        /// <summary>
        /// セットアップ木に含まれる全プロジェクトのセットアップ (ルート自身を含む)。
        /// プロジェクトごとに 1 インスタンスだけが返る。
        ///
        /// 分割コンパイルを行う backend は、この一覧をプロジェクト単位の
        /// コンパイル単位として使う (Files / IncludePaths / UnfoundModules などは
        /// それぞれのセットアップが持つ)。依存順に並べたい場合は
        /// GetAllFiles() / GetAllIncludePaths() を使うこと。
        /// </summary>
        public List<SimulationSetup> GetAllProjectSetups()
        {
            return new List<SimulationSetup>(ProjectSetups.Values);
        }

        /// <summary>
        /// セットアップ木に含まれる全プロジェクトの外部プロジェクト instance 情報を
        /// まとめて返す (プロジェクトごとの ExternalProjectEntryInstance を集約)。
        ///
        /// どのプロジェクトのセットアップからでも同じ一覧が返る
        /// (ProjectSetups はルートで共有されている)。instance path をキーに
        /// しているため、同名 module の複数 instance もすべて含まれる。
        /// </summary>
        public List<ExternalProjectEntryInstanceInfo> GetAllExternalProjectEntryInstances()
        {
            List<ExternalProjectEntryInstanceInfo> results = new List<ExternalProjectEntryInstanceInfo>();
            foreach (SimulationSetup projectSetup in ProjectSetups.Values)
            {
                foreach (ExternalProjectEntryInstanceInfo instance in projectSetup.ExternalProjectEntryInstance.Values)
                {
                    if (results.Contains(instance)) continue;
                    results.Add(instance);
                }
            }
            return results;
        }

        /// <summary>
        /// このプロジェクトが参照している外部プロジェクトのセットアップ。
        /// key: 参照先プロジェクト、value: そのプロジェクトのセットアップ。
        ///
        /// プロジェクトごとの分割コンパイルを行い、外部プロジェクトをライブラリとして
        /// 参照するための情報。参照の向き (どのプロジェクトがどのプロジェクトを
        /// 参照しているか) だけを表し、セットアップの実体は ProjectSetups が保持する
        /// 唯一のインスタンスを指す (同じ外部プロジェクトを複数箇所から参照しても
        /// セットアップは 1 つ)。
        ///
        /// backend は GetAllFiles() / GetAllIncludePaths() を使えば、この参照を
        /// 再帰的に辿った全プロジェクト分のファイル / include パスを取得できる
        /// (参照先が先に並ぶ = 依存順)。
        /// </summary>
        public Dictionary<CodeEditor2. Data. Project, SimulationSetup> ExternalProjectReferences = new Dictionary<Project, SimulationSetup>();

        /// <summary>
        /// 外部プロジェクトの building block を instance している箇所。
        /// key: instance path ("top.u_sub.u_leaf")、value: その instance の情報。
        ///
        /// モジュール名が衝突する複数の module instance を区別するための情報。
        /// instance path をキーにすることで、同じ module 名を複数箇所で
        /// instance していても 1 件ずつ独立して保持できる。
        /// 分割コンパイルでは、この情報から外部プロジェクトのセットアップ
        /// (ExternalProjectReferences) とその entry module を辿る。
        ///
        /// この辞書はセットアップ (= 1 プロジェクト) ごとに存在するため、
        /// セットアップ木全体の情報が必要な場合は
        /// GetAllExternalProjectEntryInstances() で集約して取得する。
        /// </summary>
        public Dictionary<string, ExternalProjectEntryInstanceInfo> ExternalProjectEntryInstance = new Dictionary<string, ExternalProjectEntryInstanceInfo>();

        /// <summary>
        /// 外部プロジェクトの module / interface を instance している 1 箇所分の情報。
        ///
        /// 同一の module 名が複数の instance path で使われていても (module 名が
        /// 衝突していても)、instance path ごとに「どの module を」「どの
        /// プロジェクトから」instance しているかを特定できるようにするための情報。
        /// 分割コンパイルでは、この情報から外部プロジェクトのセットアップ
        /// (ExternalProjectReferences) とその entry module を辿る。
        /// </summary>
        public class ExternalProjectEntryInstanceInfo
        {
            /// <summary>
            /// 階層 instance path ("top.u_sub.u_leaf")。
            /// ExternalProjectEntryInstance の key と同じ値。ルートのトップ名から
            /// 辿った位置を表すため、同名 module の複数 instance を区別できる。
            /// </summary>
            public required string InstancePath { get; set; }

            /// <summary>
            /// その instance path で instance している module / interface 名
            /// (parameter override 前の定義名)。instance path だけでは分からなかった
            /// 「何を instance しているか」を保持する。
            /// </summary>
            public required string ModuleName { get; set; }

            /// <summary>
            /// ModuleName を定義している (所有している) プロジェクト。
            /// "// @project &lt;name&gt;" 指定時はその参照先プロジェクトになる。
            /// 分割コンパイル時のライブラリ境界 (どのプロジェクトのビルド成果物を
            /// 参照するか) を決めるのに使う。
            /// </summary>
            public required CodeEditor2.Data.Project Project { get; set; }
        }

        /// <summary>
        /// シミュレーションに渡すファイル一覧を依存順で返す。
        ///
        /// 外部プロジェクトのファイルはそのプロジェクト専用のセットアップ
        /// (ProjectSetups / ExternalProjectReferences) にのみ入るため、Files だけを
        /// 列挙するシミュレーション backend では外部プロジェクトを instance した
        /// 設計がコンパイルできない。backend はこのメソッド経由で全ファイルを
        /// 取得すること。
        ///
        /// 参照している外部プロジェクトは「インスタンス元より先」に並べる
        /// (依存順)。プロジェクトごとに 1 セットアップなので、同じプロジェクトを
        /// 複数箇所から参照していてもファイルは重複しない。
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

            // referenced (external) projects must be compiled first: their
            // modules are instantiated from this project's sources
            foreach (SimulationSetup referenced in setup.ExternalProjectReferences.Values)
            {
                collectAllFiles(referenced, results, visited);
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
            foreach (var kvp in setup.ExternalProjectReferences)
            {
                collectIncludePaths(kvp.Value, results, visited);
            }
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

        public static async Task<SimulationSetup?> CreateAsync(pluginVerilog.Data.VerilogFile verilogFile)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await Tool.ParseHierarchy.ParseAsync(verilogFile, Tool.ParseHierarchy.ParseMode.SearchReparseReqestedTree);
            return Create(verilogFile);
        }
        public static SimulationSetup? Create(pluginVerilog. Data. VerilogFile verilogFile)
        {
            if (verilogFile.VerilogParsedDocument == null) return null;
            if (verilogFile.VerilogParsedDocument.Root == null) return null;

            // one SimulationSetup per project: the ROOT setup registers itself
            // in the shared registry so that every external project sub-setup
            // reuses exactly one instance per project.
            SimulationSetup setup = new SimulationSetup();
            setup.Project = verilogFile.Project;
            setup.RootSetup = setup;
            setup.ProjectSetups.Add(setup.Project, setup);
            setup.TopFiles.Add(verilogFile);

            List<string> ids = new List<string>();

            // TopName selection rule: when the top file declares multiple
            // building blocks (e.g. DUT + testbench in one file), the FIRST
            // declared building block is used as the simulation top.
            // This is intentional (documented behavior); a top-module selector
            // UI is a possible future enhancement.
            BuildingBlock? buildingBlock = verilogFile. VerilogParsedDocument. Root. BuildingBlocks. Values. FirstOrDefault();
            // A file may declare classes (or other non-instantiable building blocks)
            // before the top module. Classes are registered in Root.BuildingBlocks
            // too, so skip them when selecting the simulation top; otherwise
            // "class Packet" above "module MODULE5" made Packet the top and the
            // simulation failed. Fall back to the first declared block only when
            // no module/interface/program/checker exists (preserves previous
            // behavior for single-block files).
            if (buildingBlock is Class || buildingBlock is InterfaceClass || buildingBlock is Package)
            {
                buildingBlock = verilogFile. VerilogParsedDocument. Root. BuildingBlocks. Values. FirstOrDefault(
                    bb => !(bb is Class) && !(bb is InterfaceClass) && !(bb is Package));
            }
            if (buildingBlock == null) return null;

            setup.TopName = buildingBlock. Name;
            searchHier(verilogFile, setup. TopName, ids, setup, setup.TopName);

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
                    Data.VerilogFile? file = classFile as Data.VerilogFile;
                    if (file == null) continue;
                    Verilog.ParsedDocument? parsedDocument = file.VerilogParsedDocument;
                    if (parsedDocument == null) continue;
                    ProjectProperty? projectProperty = parsedDocument.ProjectProperty;
                    if(projectProperty == null) continue;
                    foreach(string className in parsedDocument.ReferencedUnitNameSpace)
                    {
                        IVerilogRelatedFile? newfile = projectProperty.UnitNameSpace.GetFile(className);
                        // exclude files already collected (in this batch or in
                        // setup.ClassFiles). Without the setup.ClassFiles check,
                        // circular class references (A -> B -> A) alternate the
                        // batches forever and this while(true) loop never ends
                        // (UI freeze / deadlock when invoked from the menu thread).
                        if(newfile != null
                            && !newClassFiles.Contains(newfile)
                            && !setup.ClassFiles.Contains(newfile))
                        {
                            newClassFiles.Add(newfile);
                        }
                    }
                }
                if (newClassFiles.Count == 0) break;
                foreach(IVerilogRelatedFile file in newClassFiles)
                {
                    if(!setup.ClassFiles.Contains(file)) setup.ClassFiles.Add(file);
                }

                targetClassFiles = newClassFiles;
            }

            // Reorder setup.Files based on class dependency graph
            // (extends / implements / references) so that referenced classes
            // are compiled before the referencing files.
            ReorderFilesByClassDependencies(setup);


            // Unfound references are reported from the top-level setup AND from
            // every external project sub-setup (recursively). Previously only the
            // top-level setup was inspected, so a missing definition inside a
            // referenced external project was silently ignored.
            List<string> unfoundModules = new List<string>();
            collectUnfoundModules(setup, unfoundModules, new List<SimulationSetup>());
            if (unfoundModules.Count != 0)
            {
                foreach (string module in unfoundModules)
                {
                    CodeEditor2.Controller.AppendLog(verilogFile.Project.Name + ":" + module + " unfound", Avalonia.Media.Colors.Red);
                }
                return null;
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
            foreach (var kvp in setup.ExternalProjectReferences)
            {
                collectUnfoundModules(kvp.Value, results, visited);
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
            foreach (var kvp in setup.ExternalProjectReferences)
            {
                string subPath = kvp.Key.Name;
                if (projectPath != "") subPath = projectPath + "/" + subPath;
                ReorderFilesByClassDependencies(kvp.Value, visited, subPath);
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
            // visited guard: key is (project + file.ID + buildingBlockName) so
            // that the same file can still be traversed for a different building
            // block, but a cycle (module A <-> module B, circular class
            // references, bind / definition cross references, external project
            // cycles) terminates.
            // The project name is part of the key because RelativePath (and thus
            // the ID of a plain VerilogFile) is only unique inside one project:
            // two projects may contain "src/top.sv", and without the prefix the
            // second project's file was skipped as already visited, dropping the
            // external project's sources from the setup.
            // Without this registration, ids stays empty forever and the guard
            // below never triggers, causing infinite recursion (stack overflow
            // / UI freeze when invoked from the menu thread).
            string visitedKey = getProjectName(file) + ":" + file.ID + ":" + buildingBlockName;
            if (ids.Contains(visitedKey)) return;
            ids.Add(visitedKey);

            // Everything BELOW an "@project <name>" instance belongs to the
            // referenced project. When the file we are about to walk is owned by
            // a project different from the setup that reached it, switch to that
            // project's (shared) sub-setup and record the crossing instance.
            //
            // Previously the traversal kept the referencing project's setup for
            // the whole subtree: appendFile() already routed each individual file
            // to its owning project's setup (it decides by sourceFile.Project),
            // but the SETUP CONTEXT stayed the referencing one. As a result the
            // nested instances/objects under the "@project" instance recorded
            // their unfound modules, class / package files and entry instances
            // against the referencing (local) project — i.e. the referenced
            // project name was not applied to the lower-level objects. Normalizing
            // the setup here covers every recursion site (module / interface /
            // array / class / package), because they all funnel through searchHier.
            if (file.Project != setup.Project)
            {
                // record the crossing on the REFERENCING setup (setup is still the
                // referencing project's setup here), then continue as the target.
                addExternalProjectEntryInstance(setup, path, buildingBlockName, file.Project);
                setup = getExternalProjectSetup(setup, file.Project);

                // keep the sub-setup's entry information (TopName / TopFiles) as
                // documented. With the setup switched before appendFile, the
                // external-project branch of appendFile is not reached for this
                // entry any more, so set them here (first crossing wins, a shared
                // sub-setup must not be overwritten by later references).
                VerilogFile? topFile = file as VerilogFile;
                if (topFile == null && file is VerilogModuleInstance moduleInstance) topFile = moduleInstance.SourceVerilogFile;
                if (topFile == null && file is InterfaceInstance interfaceInstance) topFile = interfaceInstance.SourceVerilogFile;
                if (topFile != null && setup.TopFiles.Count == 0) setup.TopFiles.Add(topFile);
                if (string.IsNullOrEmpty(setup.TopName)) setup.TopName = buildingBlockName;
            }

            ParsedDocument? parsedDocument = file.VerilogParsedDocument;
            if (parsedDocument == null) return;

            appendFile(file, setup, ids, path, buildingBlockName);

            foreach (string unfound in parsedDocument.UnfoundModules)
            {
                // A cross-project ("// @project <name>") reference can still be
                // listed here by a parse pass that ran before the target project
                // was parsed (the entry is de-duplicated but never cleared, see
                // ModuleInstantiation.ParseAsync). The reference is valid as long
                // as the definition file resolves in the project that owns it, so
                // it must not be reported as a missing module file.
                if (isResolvableCrossProjectReference(parsedDocument, unfound)) continue;

                // register as "RelativePath:Name" (instance unfound on this file)
                addUnfoundModule(setup, file, unfound);
            }

            foreach (string external in parsedDocument. ExternalRefrenceModules)
            {
                if (file.ProjectProperty.ExtenralModuleLibraryPath.ContainsKey(external))
                {
                    string libPath = file. ProjectProperty. ExtenralModuleLibraryPath[external];
                    if (!setup. ExternalLibraryPathList. Contains(libPath)) setup. ExternalLibraryPathList. Add(libPath);
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
                if (parsedDocument.Project == null) continue;
                IVerilogRelatedFile? classFile = parsedDocument.ProjectProperty?.UnitNameSpace.GetFile(className);
                Class? class_ = null;
                if (classFile != null
                    && classFile.VerilogParsedDocument != null
                    && classFile.VerilogParsedDocument.Root != null
                    && classFile.VerilogParsedDocument.Root.NamedElements.TryGetValue(className, out INamedElement? element))
                {
                    class_ = element as Class;
                }
                if (parsedDocument.Project == null) continue;
                if (class_ != null)
                {
                    appendClass(file, className, parsedDocument.Project, setup, ids);
                }
                else
                {
                    // referenced class definition is missing: report as file shortage
                    // so that simulation startup is refused later in Create()
                    addUnfoundModule(setup, file, className);
                }
            }

            foreach (var ifile in parsedDocument. IncludeFiles. Values)
            {
                appendVerilogHeaderInstance(ifile, setup);
            }

            foreach (string cFile in parsedDocument.ReferencedUnitNameSpace)
            {
                appendClass(file, cFile, file.Project, setup, ids);
            }

            // module / interface / program / udp references are registered to
            // ReferencedDefinitionNameSpace during parse. Collect their definition
            // files here; a missing definition is reported as file shortage.
            //
            // A module instanced with "// @project <name>" is NOT registered in
            // the local project's DefinitionNameSpace, so resolving every name
            // only against the local project reported a valid cross-project
            // module as "unfound" and aborted the simulation startup. The parse
            // result therefore records the owner project of cross-project
            // references (ParsedDocument.ReferencedDefinitionNameSpaceProjects).
            //
            // The lookup MUST use the recorded owner project, NOT the local
            // project. A "// @project <name>" instance explicitly asks for the
            // building block of the referenced project, so a same-named
            // definition declared in the local project is a DIFFERENT building
            // block and must never be collected in its place. (Resolving the
            // local DefinitionNameSpace first collected the wrong file and the
            // wrong dependencies whenever both projects declared a definition
            // with the same name.)
            foreach (string definitionName in parsedDocument.ReferencedDefinitionNameSpace)
            {
                CodeEditor2.Data.Project? localProject = parsedDocument.Project;
                if (localProject == null) continue;

                // Each recorded owner project is resolved independently; the
                // local project is only one of them. A name referenced both
                // without a comment (local project) and with "// @project <name>"
                // therefore collects both definition files.
                List<CodeEditor2.Data.Project> owners = getReferencedDefinitionProjects(parsedDocument, definitionName);
                if (owners.Count == 0)
                {
                    // reference recorded without an owner project (parse result
                    // produced before owners were recorded): resolve it in this
                    // document's own project
                    owners.Add(localProject);
                }

                bool resolved = false;
                foreach (CodeEditor2.Data.Project ownerProject in owners)
                {
                    ProjectProperty? ownerProperty = ownerProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                    if (ownerProperty == null) continue;

                    // external library modules are provided by external file
                    // paths, not project files
                    if (ownerProperty.ExtenralModuleLibraryPath.ContainsKey(definitionName)) { resolved = true; continue; }
                    if (ownerProperty.ExtenralPrimitiveLibraryPath.ContainsKey(definitionName)) { resolved = true; continue; }

                    IVerilogRelatedFile? defFile = ownerProperty.DefinitionNameSpace.GetFile(definitionName);
                    if (defFile == null) continue;

                    // A "// @project <name>" reference MUST be collected from that
                    // project even when this document's own project declares a
                    // definition with the same name: the comment explicitly
                    // selects the referenced project's building block.
                    appendReferencedDefinition(defFile, ownerProject, definitionName, file, ids, setup, path);
                    resolved = true;
                }

                if (!resolved)
                {
                    // referenced module/interface/program/udp definition file is missing
                    addUnfoundModule(setup, file, definitionName);
                }
            }

            // Process imported packages
            foreach (string packageName in parsedDocument. ImportedPackages)
            {
                appendImportedPackage(packageName, file. Project, setup, ids);
            }

            if(!parsedDocument. Root. BuildingBlocks. TryGetValue(buildingBlockName,out BuildingBlock? buildingBlock))
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
                    NameSpace subNameSpace = (NameSpace) element;
                    string newPath = path + "." + subNameSpace. Name;
                    searchNameSpace(file, ids, subNameSpace, setup, newPath);
                }
                else if (element is ModuleInstantiation)
                {
                    ModuleInstantiation moduleInstantiation = (ModuleInstantiation) element;
                    if (nameSpace.BuildingBlock.Project.Name != moduleInstantiation.SourceProjectName)
                    {
                        // external project instance: record the instance path
                        // together with the instanced module name and the project
                        // that defines it. The path alone could not tell which
                        // module is instanced at that point.
                        string newPath = path + "." + moduleInstantiation.Name;
                        if (CodeEditor2.Global.Projects.TryGetValue(moduleInstantiation.SourceProjectName, out CodeEditor2.Data.Project? sourceProject))
                        {
                            addExternalProjectEntryInstance(setup, newPath, moduleInstantiation.SourceName, sourceProject);
                        }
                    }
                    if (file. Items. TryGetValue(moduleInstantiation. Name, out CodeEditor2. Data. Item? item))
                    {
                        string newPath = moduleInstantiation. Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = item as IVerilogRelatedFile;
                        if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                    }
                    else if (moduleInstantiation. InstanceRange != null)
                    {
                        // instance array: VerilogModuleInstance.CreateArray registers
                        // each element to file.Items as "name[i]" while the
                        // ModuleInstantiation itself is registered with the plain
                        // name. Look up each array element so that (especially for
                        // external project instances) the definition file and its
                        // own dependencies are collected into the sub-setup.
                        int count = moduleInstantiation. InstanceCount;
                        for (int i = 0; i < count; i++)
                        {
                            string elementName = moduleInstantiation. Name + "[" + i. ToString() + "]";
                            if (!file. Items. TryGetValue(elementName, out CodeEditor2. Data. Item? arrayItem)) continue;

                            string newPath = elementName;
                            if (path != "") newPath = path + "." + newPath;

                            var subfile = arrayItem as IVerilogRelatedFile;
                            if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
                        }
                    }
                }
                else if ((element is Verilog.Items.InterfaceInstance))
                {
                    Verilog.Items.InterfaceInstance moduleInstantiation = (Verilog.Items.InterfaceInstance)element;
                    if (file.Items.TryGetValue(moduleInstantiation.Name, out CodeEditor2.Data.Item? item))
                    {
                        string newPath = moduleInstantiation.Name;
                        if (path != "") newPath = path + "." + newPath;

                        var subfile = item as IVerilogRelatedFile;
                        if (subfile != null) searchHier(subfile, moduleInstantiation.SourceName, ids, setup, newPath);
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
                    // Handle DataObject - check if it's a Class or InterfaceClass instance
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
        private static void appendFile(IVerilogRelatedFile file, SimulationSetup setup, List<string> ids, string path, string buildingBlockName)
        {
            if (file is pluginVerilog. Data. VerilogFile || file is SystemVerilogFile)
            {
                if (setup. Files. Contains(file)) return;
                setup. Files. Add(file);
                return;
            }
            if (file is VerilogModuleInstance)
            {
                VerilogModuleInstance? instance = file as VerilogModuleInstance;
                if (instance == null) return;
                IVerilogRelatedFile? sourceFile = instance. SourceTextFile as IVerilogRelatedFile;
                if (sourceFile == null) return;

                if (sourceFile. Project == setup. Project)
                {
                    if (setup. Files. Contains(sourceFile)) return;
                    setup. Files. Add(sourceFile);
                }
                else
                {
                    CodeEditor2. Data. Project project = sourceFile.Project;
                    // one setup per project: the instance is shared through the
                    // root registry, only the reference edge is per setup.
                    SimulationSetup pSetup = getExternalProjectSetup(setup, project);
                    if (pSetup.TopFiles.Count == 0) pSetup.TopFiles.Add(instance.SourceVerilogFile);
                    if (string.IsNullOrEmpty(pSetup.TopName)) pSetup.TopName = instance.ModuleName;
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    // remember the instance path together with the instanced
                    // module name and its owning project
                    addExternalProjectEntryInstance(setup, path, instance.ModuleName, project);

                    // recurse into the external project instance so that its own
                    // module / class / package dependencies are collected into the
                    // external project sub-setup as well.
                    // Note: do NOT return when the source file was already added.
                    // The same external file may be instantiated several times
                    // with different parameter overrides, and each instance has
                    // its own parsed document (hence its own dependencies).
                    appendExternalProjectDependencies(instance, sourceFile, pSetup, ids, path);
                }
                return;
            }else if(file is InterfaceInstance)
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
                    // same reasoning as the module instance above
                    SimulationSetup pSetup = getExternalProjectSetup(setup, project);
                    if (pSetup.TopFiles.Count == 0) pSetup.TopFiles.Add(instance.SourceVerilogFile);
                    if (string.IsNullOrEmpty(pSetup.TopName)) pSetup.TopName = instance.ModuleName;
                    if (!pSetup.Files.Contains(sourceFile)) pSetup.Files.Add(sourceFile);

                    addExternalProjectEntryInstance(setup, path, instance.ModuleName, project);

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

        private static void appendClassInstance(IVerilogRelatedFile file, DataObject dataObject, SimulationSetup setup)
        {
            ProjectProperty? projectProperty = file.ProjectProperty;
            if (projectProperty == null) return;

            // Handle Object (class instance) - has Class property
            if (dataObject is Verilog.DataObjects.Variables.Object objectInstance)
            {
                Class? class_ = objectInstance.GetSourceClass();
                if (class_ == null) return;

                IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(class_.Name);
                if (sourceFile == null) return;

                if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                {
                    if (setup.Files.Contains(sourceFile)) return;
                    setup.Files.Add(sourceFile);
                    return;
                }
            }

            // Handle UserDefinedVariable with UserDefinedType
            if (dataObject is UserDefinedVariable userDefinedVariable)
            {
                if (userDefinedVariable.DataType is UserDefinedType userDefinedType)
                {
                    // typedef is registered to UnitNameSpace (compilation-unit scope), not DefinitionNameSpace
                    IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(userDefinedType.Typedef.Name);
                    if (sourceFile == null) return;

                    if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                    {
                        if (setup.Files.Contains(sourceFile)) return;
                        setup.Files.Add(sourceFile);
                        return;
                    }
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
                    IVerilogRelatedFile? sourceFile = projectProperty.UnitNameSpace.GetFile(userDefinedType.Typedef.Name);
                    if (sourceFile == null) return;

                    if (sourceFile is pluginVerilog.Data.VerilogFile || sourceFile is SystemVerilogFile)
                    {
                        if (setup.Files.Contains(sourceFile)) return;
                        setup.Files.Add(sourceFile);
                        return;
                    }
                }
            }
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

            if (setup.Files.Contains(interfaceFile)) return;
            setup.Files.Add(interfaceFile);
        }

        private static void appendVerilogHeaderInstance(VerilogHeaderInstance file, SimulationSetup setup)
        {
            if (file. Project == setup. Project)
            {
                if (setup. IncludeFiles. Contains(file)) return;
                setup. IncludeFiles. Add(file);
                string? path = System. IO. Path. GetDirectoryName(file. Project. GetAbsolutePath(file. RelativePath));
                if (path == null) return;
                if (!setup. IncludePaths. Contains(path)) setup. IncludePaths. Add(path);
                return;
            }
            else
            {
                CodeEditor2. Data. Project project = file. Project;
                SimulationSetup pSetup = getExternalProjectSetup(setup, project);
                if (pSetup. IncludeFiles. Contains(file)) return;
                pSetup. IncludeFiles. Add(file);
                string? path = System. IO. Path. GetDirectoryName(file. Project. GetAbsolutePath(file. RelativePath));
                if (path == null) return;
                if (!pSetup. IncludePaths. Contains(path)) pSetup. IncludePaths. Add(path);
                return;
            }

        }

        private static void appendImportedPackage(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup, List<string> ids)
        {
            // Search for the package file in the project
            IVerilogRelatedFile? packageFile = findPackageFile(packageName, project, setup);
            if (packageFile == null) return;

            // Add to ImportFiles
            SimulationSetup ownerSetup;
            if (packageFile.Project == setup.Project)
            {
                if (setup.ImportFiles.Contains(packageFile)) return;
                setup.ImportFiles.Add(packageFile);
                ownerSetup = setup;
            }
            else
            {
                // Handle external project reference. The package file belongs to
                // another project, so its own dependencies have to be collected
                // into that project's sub-setup as well (previously the recursion
                // used the current setup, which put the external package's
                // dependencies into the wrong project and left the sub-setup with
                // only the package file itself).
                SimulationSetup pSetup = getExternalProjectSetup(setup, packageFile.Project);
                if (pSetup.ImportFiles.Contains(packageFile)) return;
                pSetup.ImportFiles.Add(packageFile);
                ownerSetup = pSetup;
            }

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
                addUnfoundModule(setup, referrer, className);
                return;
            }

            // Add to ClassFiles
            SimulationSetup ownerSetup;
            if (classFile.Project == setup.Project)
            {
                if (setup.ClassFiles.Contains(classFile)) return;
                setup.ClassFiles.Add(classFile);
                ownerSetup = setup;
            }
            else
            {
                // Handle external project reference. The class file belongs to
                // another project, so its own dependencies must be collected into
                // that project's sub-setup (see appendImportedPackage).
                SimulationSetup pSetup = getExternalProjectSetup(setup, classFile.Project);
                if (pSetup.ClassFiles.Contains(classFile)) return;
                pSetup.ClassFiles.Add(classFile);
                ownerSetup = pSetup;
            }

            // class file may itself reference other classes / packages: trace them
            // (also reports missing references in the class file as file shortage)
            searchHier(classFile, className, ids, ownerSetup, "");
        }

        /// <summary>
        /// Collect the definition file of a referenced building block that is
        /// owned by the given project (the "// @project <name>" target or this
        /// document's own project).
        ///
        /// A definition owned by another project always goes to that project's
        /// sub-setup so that the simulation backend receives it as a source of
        /// the external project. A definition owned by the top project of the
        /// setup is traversed in place instead.
        /// </summary>
        private static void appendReferencedDefinition(
            IVerilogRelatedFile defFile,
            CodeEditor2.Data.Project ownerProject,
            string definitionName,
            IVerilogRelatedFile referrer,
            List<string> ids,
            SimulationSetup setup,
            string path)
        {
            if (ownerProject == setup.Project)
            {
                // recurse into the referenced definition file so that its own
                // module / class / package dependencies are collected as well
                searchHier(defFile, definitionName, ids, setup, path);
                return;
            }

            // cross-project definition: the definition file belongs to that
            // project's sub-setup so that the simulation backend receives it as
            // a source of the external project.
            SimulationSetup ownerSetup = getExternalProjectSetup(setup, ownerProject);
            if (!ownerSetup.Files.Contains(defFile)) ownerSetup.Files.Add(defFile);

            // Its own dependencies must be collected through the INSTANCE
            // (parameter overrides applied) by appendExternalProjectDependencies,
            // which runs later from the instance tree. Walking the base parse
            // here would collect the modules selected by the DEFAULT parameter
            // values and report modules of the non-selected "generate if" branch
            // as unfound. Only fall back to the base parse when no instance item
            // exists for it.
            if (!hasInstantiationItem(referrer, definitionName))
            {
                searchHier(defFile, definitionName, ids, ownerSetup, path);
            }
        }

        /// <summary>
        /// Get (or create) the setup that collects the files of the given
        /// external project, and register it as a reference of "setup".
        ///
        /// The setup instance itself is unique per project: it is looked up in
        /// the shared ProjectSetups registry, so every referencing project (and
        /// every reference path) uses the same instance. This keeps the
        /// "1 SimulationSetup per project" invariant the simulation backends
        /// rely on. Only the reference edge is added per referencing setup.
        /// </summary>
        private static SimulationSetup getExternalProjectSetup(SimulationSetup setup, CodeEditor2.Data.Project project)
        {
            SimulationSetup subSetup = getOrCreateProjectSetup(setup, project);
            if (!setup.ExternalProjectReferences.ContainsKey(project))
            {
                setup.ExternalProjectReferences.Add(project, subSetup);
            }
            return subSetup;
        }

        /// <summary>
        /// Get (or create) the unique setup instance of the given project.
        ///
        /// The registry always lives on the ROOT setup (the one created by
        /// Create), so that sub-setups created from nested calls share it:
        /// a sub-setup does not own a registry of its own.
        /// </summary>
        private static SimulationSetup getOrCreateProjectSetup(SimulationSetup setup, CodeEditor2.Data.Project project)
        {
            SimulationSetup root = setup.RootSetup ?? setup;
            if (root.ProjectSetups.TryGetValue(project, out SimulationSetup? existing)) return existing;

            SimulationSetup projectSetup = new SimulationSetup()
            {
                Project = project,
                RootSetup = root,
                ProjectSetups = root.ProjectSetups
            };
            root.ProjectSetups.Add(project, projectSetup);
            return projectSetup;
        }

        /// <summary>
        /// Register an external project instance (instance path + module name +
        /// owning project) so that the entry point of the external project is
        /// identifiable from the setup tree.
        /// </summary>
        private static void addExternalProjectEntryInstance(
            SimulationSetup setup,
            string instancePath,
            string moduleName,
            CodeEditor2.Data.Project project)
        {
            if (setup.ExternalProjectEntryInstance.ContainsKey(instancePath)) return;

            setup.ExternalProjectEntryInstance.Add(instancePath, new ExternalProjectEntryInstanceInfo()
            {
                InstancePath = instancePath,
                ModuleName = moduleName,
                Project = project
            });
        }

        /// <summary>
        /// True when the given file has an instance item for the given definition
        /// name (i.e. an instance of the module / interface was resolved and
        /// registered to file.Items).
        ///
        /// Such an instance is traversed by the instance tree (searchNameSpace ->
        /// appendExternalProjectDependencies), which collects the dependencies of
        /// the PARAMETER-OVERRIDDEN parse. Walking the definition's base parse as
        /// well would collect the modules of the default (non-selected) generate
        /// branch and report them as unfound.
        /// </summary>
        private static bool hasInstantiationItem(IVerilogRelatedFile file, string definitionName)
        {
            try
            {
                foreach (CodeEditor2.Data.Item item in file.Items)
                {
                    if (item is VerilogModuleInstance moduleInstance && moduleInstance.ModuleName == definitionName) return true;
                    if (item is InterfaceInstance interfaceInstance && interfaceInstance.ModuleName == definitionName) return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// True when the given name (recorded as unfound by some parse pass) is a
        /// cross-project reference whose definition file resolves in the project
        /// that owns it. Such a name is not a missing module file.
        /// </summary>
        private static bool isResolvableCrossProjectReference(ParsedDocument parsedDocument, string definitionName)
        {
            CodeEditor2.Data.Project? ownProject = parsedDocument.Project;
            foreach (CodeEditor2.Data.Project crossProject in getReferencedDefinitionProjects(parsedDocument, definitionName))
            {
                if (crossProject == ownProject) continue;
                ProjectProperty? crossProperty = crossProject.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (crossProperty == null) continue;
                if (crossProperty.DefinitionNameSpace.GetFile(definitionName) != null) return true;
            }
            return false;
        }

        /// <summary>
        /// Names of the projects that own the given referenced definition, as
        /// recorded by the parse ("// @project <name>" targets and, for plain
        /// references, this document's own project).
        /// </summary>
        private static List<string> getReferencedDefinitionProjectNames(ParsedDocument parsedDocument, string definitionName)
        {
            return parsedDocument.GetReferencedDefinitionNameSpaceProjects(definitionName);
        }

        /// <summary>
        /// Projects that own the given referenced definition name, as recorded by
        /// the parse for cross-project ("// @project <name>") references.
        /// </summary>
        private static List<CodeEditor2.Data.Project> getReferencedDefinitionProjects(ParsedDocument parsedDocument, string definitionName)
        {
            List<CodeEditor2.Data.Project> results = new List<CodeEditor2.Data.Project>();
            foreach (string projectName in getReferencedDefinitionProjectNames(parsedDocument, definitionName))
            {
                if (CodeEditor2.Global.Projects.TryGetValue(projectName, out CodeEditor2.Data.Project? project)
                    && project != null)
                {
                    if (!results.Contains(project)) results.Add(project);
                }
            }
            return results;
        }

        /// <summary>
        /// Project name used as part of the visited key of searchHier.
        /// </summary>
        private static string getProjectName(IVerilogRelatedFile file)
        {
            try
            {
                return file.Project?.Name ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// register an unfound reference as "RelativePath:Name" so that the
        /// log shows where the missing definition/instance is referenced from.
        /// </summary>
        private static void addUnfoundModule(SimulationSetup setup, IVerilogRelatedFile file, string name)
        {
            string entry;
            try { entry = file.RelativePath + ":" + name; }
            catch { entry = file.ID + ":" + name; }
            if (!setup.UnfoundModules.Contains(entry)) setup.UnfoundModules.Add(entry);
        }

        private static IVerilogRelatedFile? findPackageFile(string packageName, CodeEditor2. Data. Project project, SimulationSetup setup)
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

        private static IVerilogRelatedFile? findPackageFileInProject(string packageName, CodeEditor2. Data. Project project)
        {
            foreach (var file in project.Items)
            {
                IVerilogRelatedFile? vFile = file as IVerilogRelatedFile;
                if (vFile == null) continue;

                ParsedDocument? parsedDoc = vFile. VerilogParsedDocument;
                if (parsedDoc == null) continue;
                if (parsedDoc. Root == null) continue;

                foreach (var bb in parsedDoc. Root. BuildingBlocks. Values)
                {
                    if (bb is Package)
                    {
                        Package pkg = (Package) bb;
                        if (pkg. Name == packageName)
                        {
                            return vFile;
                        }
                    }
                }
            }
            return null;
        }

    }
}
