using AjkAvaloniaLibs.Libs.Json;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using pluginVerilog.Data;
using SystemVerilogCore.Verilog.BuildingBlocks;
using SystemVerilogCore.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using SystemVerilogCore.Verilog;

// The rule model and the annotation command table moved to the UI-free parser
// assembly. These aliases keep the existing references in this assembly
// compiling unchanged; the parser itself now uses the SystemVerilogCore.Verilog
// types directly.
using Rule = SystemVerilogCore.Verilog.Rule;
using RuleSet = SystemVerilogCore.Verilog.RuleSet;
using AnnotationCommands = SystemVerilogCore.Verilog.AnnotationCommands;
using Macro = SystemVerilogCore.Verilog.Macro;

namespace pluginVerilog
{
    public class ProjectProperty : CodeEditor2.Data.ProjectProperty,
        SystemVerilogCore.Projects.IVerilogProjectSettings,
        Verilog.IVerilogParserSettings
    {
        // ---- IVerilogParserSettings: the parts typed with parser types -----
        // These cannot live on the UI-free settings contract (that assembly
        // cannot name WordScanner / NameSpace / IBuildingBlockInstantiation),
        // so they are declared next to the parser and the parser reads them
        // through this interface instead of this class. The member shapes are
        // identical to the members below, so no parse site changes.
        //
        // Only the members whose concrete type differs from the interface are
        // forwarded explicitly; the rest match by signature and satisfy the
        // interface directly.

        IReadOnlyDictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>?>
            Verilog.IVerilogParserSettings.SystemTaskParsers => SystemTaskParsers;

        IReadOnlyDictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>?>
            Verilog.IVerilogParserSettings.SystemFunctions => SystemFunctions;

        // These three are fields on this class, and a field cannot implement an
        // interface member, so the contract needs an explicit implementation.
        // The IVerilogProjectSettings members above forward for the same reason.
        IReadOnlyDictionary<string, Macro>
            Verilog.IVerilogParserSettings.Macros => Macros;

        RuleSet Verilog.IVerilogParserSettings.RuleSet => RuleSet;

        IReadOnlyDictionary<string, string>
            Verilog.IVerilogParserSettings.ExtenralModuleLibraryPath =>
            ExtenralModuleLibraryPath;

        IReadOnlyDictionary<string, string>
            Verilog.IVerilogParserSettings.ExtenralPrimitiveLibraryPath =>
            ExtenralPrimitiveLibraryPath;

        // A field on this class, like the three above.
        string Verilog.IVerilogParserSettings.AnnotationKeyValueDelimiter =>
            AnnotationKeyValueDelimiter;

        /// <summary>
        /// Builds the item-tree instance the parser records after it parses a
        /// parameter-overridden module on demand.
        ///
        /// <para>
        /// The concrete instance (<see cref="Data.VerilogModuleInstance"/>) is an
        /// editor item, so the parser cannot name it. It asks here instead and
        /// gets the instance back as the UI-free
        /// <see cref="SystemVerilogCore.Projects.IVerilogInstanceSource"/>
        /// contract.
        /// </para>
        /// </summary>
        SystemVerilogCore.Projects.IVerilogInstanceSource?
            Verilog.IVerilogParserSettings.CreateInstanceItem(
                Verilog.Items.IBuildingBlockInstantiation instantiation)
        {
            if (instantiation is not Verilog.Items.ModuleInstantiation moduleInstantiation) return null;
            return Data.VerilogModuleInstance.Create(moduleInstantiation);
        }

        /// <summary>
        /// The completion candidate that expands a module name to a whole
        /// instantiation. The expansion text is an editor concern, so the parser
        /// asks for the candidate rather than building one.
        /// </summary>
        SystemVerilogCore.Documents.IVerilogAutocompleteItem?
            Verilog.IVerilogParserSettings.CreateModuleInstanceCandidate(string moduleName) =>
            new Data.VerilogCommon.EditorSnippets.ModuleInstanceSnippet(moduleName);

        /// <summary>
        /// The completion candidate for a block-opening keyword, whose insertion
        /// text is a skeleton rather than the keyword itself.
        ///
        /// <para>
        /// Only the block keywords have a dedicated editor candidate; every other
        /// keyword is a plain candidate the parser builds. Returning null for
        /// those lets the parser fall back to the plain candidate.
        /// </para>
        /// </summary>
        SystemVerilogCore.Documents.IVerilogAutocompleteItem?
            Verilog.IVerilogParserSettings.CreateKeywordCandidate(string keyword) =>
            keyword switch
            {
                "begin" => new Data.VerilogCommon.EditorAutoComplete.BeginAutoCompleteItem(),
                "case" => new Data.VerilogCommon.EditorAutoComplete.CaseAutocompleteItem(),
                "function" => new Data.VerilogCommon.EditorAutoComplete.FunctionAutocompleteItem(),
                "generate" => new Data.VerilogCommon.EditorAutoComplete.GenerateAutoCompleteItem(),
                "module" => new Data.VerilogCommon.EditorAutoComplete.ModuleAutocompleteItem(),
                "task" => new Data.VerilogCommon.EditorAutoComplete.TaskAutocompleteItem(),
                "interface" => new Data.VerilogCommon.EditorAutoComplete.InterfaceAutocompleteItem(),
                "<=" => new Data.VerilogCommon.EditorAutoComplete.NonBlockingAssignmentAutoCompleteItem(),
                _ => null,
            };

        /// <summary>
        /// Forwards keyword enumeration to
        /// <see cref="Data.VerilogCommon.AutoCompleteKeyword"/>, passing these
        /// settings along so the block keywords come back host-expanded.
        /// </summary>
        void Verilog.IVerilogParserSettings.AppendKeywordCandidates(
            List<SystemVerilogCore.Documents.IVerilogAutocompleteItem> items,
            string candidate,
            int candidateStartIndex,
            int lineStartIndex,
            bool systemVerilog,
            Func<SystemVerilogCore.Documents.IVerilogAutocompleteItem, bool> itemFilter) =>
            Data.VerilogCommon.AutoCompleteKeyword.AppendKeywordAutoCompleteItems(
                items, candidate, candidateStartIndex, lineStartIndex, systemVerilog,
                itemFilter, this);

        /// <summary>
        /// Forwards the completion-target walk to
        /// <see cref="Data.VerilogCommon.AutoComplete"/>.
        /// </summary>
        bool Verilog.IVerilogParserSettings.GetAutoCompleteTarget(
            SystemVerilogCore.Documents.IVerilogSourceFile item,
            Verilog.ParsedDocument parsedDocument,
            int index,
            out Verilog.NameSpace? nameSpace,
            out Verilog.INamedElement? element,
            out string candidate,
            out int candidateStartIndex) =>
            Data.VerilogCommon.AutoComplete.GetAutoCompleteTarget(
                item, parsedDocument, index,
                out nameSpace, out element, out candidate, out candidateStartIndex);

        // The ID is required by the parser-facing settings contract so a parse
        // site can identify these settings without knowing the editor type.
        string SystemVerilogCore.Projects.IVerilogProjectSettings.ID => Plugin.StaticID;

        // ---- IVerilogProjectSettings: what the parser reads ----------------
        // Same values as the members below; the contract just types them so a
        // parse site does not have to name this class.

        SystemVerilogCore.Verilog.IVerilogNameSpace
            SystemVerilogCore.Projects.IVerilogProjectSettings.DefinitionNameSpace =>
            DefinitionNameSpace;

        SystemVerilogCore.Verilog.IVerilogNameSpace
            SystemVerilogCore.Projects.IVerilogProjectSettings.PackageNameSpace =>
            PackageNameSpace;

        SystemVerilogCore.Verilog.IVerilogNameSpace
            SystemVerilogCore.Projects.IVerilogProjectSettings.UnitNameSpace =>
            UnitNameSpace;

        IReadOnlyDictionary<string, Macro>
            SystemVerilogCore.Projects.IVerilogProjectSettings.Macros => Macros;

        AnnotationCommands
            SystemVerilogCore.Projects.IVerilogProjectSettings.AnnotationCommands =>
            AnnotationCommands;

        RuleSet SystemVerilogCore.Projects.IVerilogProjectSettings.RuleSet => RuleSet;

        IReadOnlyDictionary<string, string>
            SystemVerilogCore.Projects.IVerilogProjectSettings.ExtenralModuleLibraryPath =>
            ExtenralModuleLibraryPath;

        IReadOnlyDictionary<string, string>
            SystemVerilogCore.Projects.IVerilogProjectSettings.ExtenralPrimitiveLibraryPath =>
            ExtenralPrimitiveLibraryPath;

        //public ProjectProperty(CodeEditor2.Data.Project project)
        //{
        //    this.project = project;
        //}
        public ProjectProperty(Project project, ProjectProperty.Setup setup) : base(project, setup)
        {
            this.project = project;
            CompileOption = setup.CompileOption;
        }

        /// <summary>
        /// The project the rule sets belong to (used to resolve the absolute
        /// paths of the <c>.rule.yaml</c> files while scanning the workspace).
        /// </summary>
        private Project? project;

        /// <summary>
        /// Default rule set: used for every file that is not covered by a
        /// <c>.rule.yaml</c> file.
        /// </summary>
        public RuleSet RuleSet = new RuleSet();

        public string CompileOption { get; set; } = "";

        // rule sets -----------------------------------------------------------

        /// <summary>
        /// Guards <see cref="ruleSets"/> / <see cref="scannedRootPath"/>.
        /// Parsing runs on background threads, so the lookup from
        /// <see cref="GetRuleSet"/> may happen while the workspace scan or a
        /// <c>.rule.yaml</c> file accept replaces the table.
        /// </summary>
        private readonly object ruleSetLock = new object();

        /// <summary>
        /// Rule sets keyed by the directory they apply to (project relative,
        /// '/' separated, "" = project root). A <c>.rule.yaml</c> file applies
        /// to every source file below the directory that contains it.
        /// </summary>
        private Dictionary<string, RuleSet> ruleSets = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Root path the workspace scan already ran for (null = not scanned).</summary>
        private string? scannedRootPath = null;

        /// <summary>
        /// Serializes the workspace scan. Parsing runs on background threads
        /// and the first rule lookup can happen on several of them at once:
        /// without this, one thread would see <see cref="scannedRootPath"/>
        /// already set while the scan is still running and parse with the
        /// built-in defaults.
        /// </summary>
        private readonly object ruleScanLock = new object();

        /// <summary>
        /// Snapshot of the rule sets currently registered per directory.
        /// </summary>
        public IReadOnlyDictionary<string, RuleSet> RuleSets
        {
            get
            {
                lock (ruleSetLock)
                {
                    return new Dictionary<string, RuleSet>(ruleSets, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// Normalizes a project relative path for rule set keys: both
        /// separators are accepted and the result never starts / ends with
        /// a separator ("" is the project root).
        /// </summary>
        public static string NormalizeRuleSetPath(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return "";
            string path = relativePath.Replace('\\', '/').Trim();
            while (path.StartsWith("/", StringComparison.Ordinal)) path = path.Substring(1);
            while (path.EndsWith("/", StringComparison.Ordinal)) path = path.Substring(0, path.Length - 1);
            return path;
        }

        /// <summary>
        /// Directory (project relative, '/' separated) a file belongs to.
        /// </summary>
        private static string GetDirectoryKey(string? relativePath)
        {
            string path = NormalizeRuleSetPath(relativePath);
            int slash = path.LastIndexOf('/');
            if (slash < 0) return "";
            return path.Substring(0, slash);
        }

        /// <summary>
        /// Registers the rule set of a <c>.rule.yaml</c> file. The rule set
        /// applies to every source file below the directory that contains the
        /// <c>.rule.yaml</c> file.
        /// </summary>
        /// <param name="ruleFilePath">
        /// project relative path of the .rule.yaml file
        /// </param>
        /// <param name="ruleSet">the loaded rule set (null removes the entry)</param>
        public void SetRuleSet(string ruleFilePath, RuleSet? ruleSet)
        {
            string key = GetDirectoryKey(ruleFilePath);
            lock (ruleSetLock)
            {
                if (ruleSet == null)
                {
                    ruleSets.Remove(key);
                }
                else
                {
                    ruleSets[key] = ruleSet;
                }
            }
        }

        /// <summary>
        /// Returns the rule set that applies to <paramref name="relativePath"/>.
        ///
        /// <para>
        /// The deepest directory with a <c>.rule.yaml</c> file wins, so a
        /// <c>.rule.yaml</c> in <c>src/rtl</c> overrides one in <c>src</c>. When
        /// no <c>.rule.yaml</c> file covers the file, <see cref="RuleSet"/> (the
        /// built-in defaults) is returned.
        /// </para>
        /// </summary>
        public RuleSet GetRuleSet(string? relativePath)
        {
            EnsureRuleSetsLoaded();

            string path = NormalizeRuleSetPath(relativePath);
            lock (ruleSetLock)
            {
                while (true)
                {
                    if (ruleSets.TryGetValue(path, out RuleSet? found)) return found;
                    if (path.Length == 0) break;

                    int slash = path.LastIndexOf('/');
                    path = slash < 0 ? "" : path.Substring(0, slash);
                }
            }
            return RuleSet;
        }

        /// <summary>
        /// Scans the workspace for <c>.rule.yaml</c> files the first time a rule
        /// set is requested (and again when the project root changes), so the
        /// rules are available during parsing no matter in which order the
        /// files are parsed.
        /// </summary>
        public void EnsureRuleSetsLoaded()
        {
            if (project == null) return;

            string rootPath = project.RootPath ?? "";
            lock (ruleScanLock)
            {
                lock (ruleSetLock)
                {
                    if (string.Equals(scannedRootPath, rootPath, StringComparison.OrdinalIgnoreCase)) return;
                }

                RefreshRuleSets();

                lock (ruleSetLock)
                {
                    scannedRootPath = rootPath;
                }
            }
        }

        /// <summary>
        /// Re-reads every <c>.rule.yaml</c> file of the workspace and replaces the
        /// registered rule sets.
        /// </summary>
        public void RefreshRuleSets()
        {
            if (project == null) return;

            Dictionary<string, RuleSet> loaded = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string rootPath = project.RootPath ?? "";
                if (rootPath.Length != 0 && System.IO.Directory.Exists(rootPath))
                {
                    // Enumerate once and filter on the extension: two passes would
                    // make a directory that holds both spellings depend on which
                    // pass ran last.
                    foreach (string filePath in System.IO.Directory.EnumerateFiles(
                        rootPath, "*", System.IO.SearchOption.AllDirectories))
                    {
                        if (!IsRuleFilePath(filePath)) continue;

                        string relativePath = project.GetRelativePath(filePath);
                        if (IsInSkippedDirectory(relativePath)) continue;

                        SystemVerilogCore.Verilog.RuleSet? ruleSet =
                            SystemVerilogCore.Verilog.RuleSet.LoadFile(filePath);
                        if (ruleSet == null) continue;
                        loaded[GetDirectoryKey(relativePath)] = ruleSet;
                    }
                }
            }
            catch (Exception)
            {
                // an unreadable directory must not break parsing
            }

            lock (ruleSetLock)
            {
                ruleSets = loaded;
            }
        }

        /// <summary>
        /// Extension of the rule files (<c>.rule.yaml</c>).
        /// </summary>
        /// <remarks>
        /// The double extension says what the file is (a rule set) and what it
        /// contains (YAML), so editors highlight it as YAML out of the box. The
        /// older <c>.rule</c> spelling is still recognised by
        /// <see cref="RuleFileExtensions"/> so an existing workspace keeps
        /// working; only new files are created with the long name.
        /// </remarks>
        public const string RuleFileExtension = ".rule.yaml";

        /// <summary>
        /// Every extension a rule file may use, newest first. The first entry is
        /// what <see cref="FileTypes.RuleFile.CreateNewFile"/> writes.
        /// </summary>
        /// <remarks>
        /// <c>.rule</c> is the original spelling and stays readable: a workspace
        /// that already has one must not silently stop applying its severities
        /// because the extension changed. It is not offered for new files.
        /// </remarks>
        public static readonly string[] RuleFileExtensions = { ".rule.yaml", ".rule" };

        /// <summary>
        /// True when <paramref name="path"/> names a rule file, whichever
        /// spelling it uses.
        /// </summary>
        public static bool IsRuleFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string lower = path.ToLowerInvariant();
            foreach (string extension in RuleFileExtensions)
            {
                if (lower.EndsWith(extension, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Build directories / VCS metadata never contribute rule files.
        /// </summary>
        private static bool IsInSkippedDirectory(string relativePath)
        {
            string path = NormalizeRuleSetPath(relativePath);
            foreach (string segment in path.Split('/'))
            {
                switch (segment.ToLowerInvariant())
                {
                    case "obj":
                    case "bin":
                    case "publish":
                    case "node_modules":
                    case ".git":
                    case ".svn":
                    case ".hg":
                        return true;
                }
            }
            return false;
        }

        public Data.VerilogCommon.EditorAutoComplete.Setup SnippetSetup = new Data.VerilogCommon.EditorAutoComplete.Setup();

        public override void InitializePropertyForm(ItemPropertyForm form, CodeEditor2.NavigatePanel.NavigatePanelNode node, Project project)
        {
            base.InitializePropertyForm(form, node, project);
            if (node is CodeEditor2.NavigatePanel.ProjectNode)
            {
                Views.ProjectPropertyTab tab = new Views.ProjectPropertyTab(this, form, node, project);
            }
        }

        public override Setup CreateSetup()
        {
            return new Setup(this);
        }

        public override CodeEditor2.Data.ProjectProperty.Setup? CreateSetup(JsonElement jsonElement, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(jsonElement, typeof(Setup), options) as CodeEditor2.Data.ProjectProperty.Setup;
        }
        public static ProjectProperty.Setup? DeserializeSetup(JsonElement jsonElement, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(jsonElement, typeof(ProjectProperty.Setup), options) as ProjectProperty.Setup;
        }
        public new class Setup : CodeEditor2.Data.ProjectProperty.Setup
        {
            public Setup() { }
            public Setup(ProjectProperty projectProperty)
            {
                this.CompileOption = projectProperty.CompileOption;
            }
            public override string ID { get; set; } = Plugin.StaticID;
            public string test { get; set; } = "verilogTest";

            public string CompileOption { get; set; } = "";
            public override void Write(
                Utf8JsonWriter writer,
                JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, this, typeof(Setup), options);
            }


        }

        public Dictionary<string, string> ExtenralModuleLibraryPath = new Dictionary<string, string>();
        public Dictionary<string, string> ExtenralPrimitiveLibraryPath = new Dictionary<string, string>();

        public AnnotationCommands AnnotationCommands { get; set; } = new AnnotationCommands();
        public string AnnotationKeyValueDelimiter = ":";
        public string AnnotationSeparator = ",";

        /// <summary>
        /// Compatibility alias for the type that used to be nested here.
        ///
        /// <para>
        /// The table moved to the UI-free parser assembly
        /// (<see cref="SystemVerilogCore.Verilog.AnnotationCommands"/>), because
        /// the parser is its only consumer. The old name is kept as an alias so
        /// existing references keep compiling; new code should use the
        /// contract type.
        /// </para>
        /// </summary>
        public class AnnotationCommandsClass : AnnotationCommands
        {
        }

        public BuildingBlock? GetInstancedBuildingBlock(IBuildingBlockInstantiation instantiation)
        {
            if (instantiation.ParameterOverrides.Count == 0)
            {
                return DefinitionNameSpace.Get(instantiation.SourceName);
            }
            else
            {
                Data.VerilogFile? file = DefinitionNameSpace.GetFile(instantiation.SourceName) as Data.VerilogFile;
                if (file == null) return null;
                string InstanceKey = Verilog.ParsedDocument.KeyGenerator(file, instantiation.SourceName, instantiation.ParameterOverrides);

                Verilog.ParsedDocument? parsedDocument = file.GetInstancedParsedDocument(InstanceKey) as Verilog.ParsedDocument;
                if (parsedDocument == null) return null;
                if (parsedDocument.Root == null) return null;
                if(parsedDocument.Root.BuildingBlocks.TryGetValue(instantiation.SourceName, out BuildingBlock? buildingBlock))
                {
                    return buildingBlock;
                }
                return null;
            }
        }


        // Definition NameSpace ----------------------------------------------------------------------------------------------------------------------------------
        // top level module, primitive, program, interface register to this namespace
        public Verilog.RegisterNameSpace<BuildingBlock> DefinitionNameSpace { get; } = new Verilog.RegisterNameSpace<BuildingBlock>();
        /*
        定義ネームスペース (Definitions name space): 他の宣言の外側で定義された、ネストされていないモジュール、プリミティブ、プログラム、インターフェースの識別子を統合します。
        あるコンパイル単位でこれらの名前が使用されると、他のコンパイル単位も含め、デザイン全体で同じ名前を再利用することはできません。
        */


        // Package NameSpace ----------------------------------------------------------------------------------------------------------------------------------
        // packages
        public Verilog.RegisterNameSpace<Package> PackageNameSpace { get; } = new Verilog.RegisterNameSpace<Package>();
        /*
        パッケージ・ネームスペース (Package name space): すべてのコンパイル単位で定義されたすべてのパッケージ識別子を統合します。
        パッケージ名もデザイン全体で一意である必要があります。
        */

        // Compile Unit NameSpace ----------------------------------------------------------------------------------------------------------------------------------
        // top level class 
        public Verilog.RegisterNameSpace<INamedElement> UnitNameSpace { get; } = new Verilog.RegisterNameSpace<INamedElement>();
        /*
        コンパイル単位でグローバルなネームスペース
        コンパイル単位スコープ・ネームスペース (Compilation-unit scope name space): 
        モジュール、インターフェース、パッケージなどの外側に存在し、そのコンパイル単位内での関数、タスク、チェッカー、
        パラメータ、名前付きイベント、ネット、変数、ユーザー定義型を統合します
        */


        // -----------------------------------------------------------------------------------------------------------------------------------------


        // inline comment
        //        public Dictionary<string, Action<Verilog.ParsedDocument>> InLineCommentCommands { get; } = new Dictionary<string, Action<Verilog.ParsedDocument>>();

        // macros
        public Dictionary<string, Macro> Macros { get; } = new Dictionary<string, Macro>();

        // system tasks
        public Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>?> SystemTaskParsers { get; }
            = new Dictionary<string, Func<Verilog.WordScanner, Verilog.NameSpace, Verilog.Statements.SystemTask.SystemTask>?>
        {
            // Display task
            {"$display",null },
            {"$strobe",null },
            {"$displayb",null },
            {"$strobeb",null },
            {"$displayh",null },
            {"$strobeh",null },
            {"$displayo",null },
            {"$strobeo", null },
            {"$monitor", null },
            {"$write", null },
            {"$monitorb", null },
            {"$writeb", null },
            {"$monitorh", null },
            {"$writeh", null },
            {"$monitoro", null },
            {"$writeo", null },
            {"$monitoroff", null },
            {"$monitoron", null },
            // File I/O tasks
            {"$fclose", null },
            {"$fdisplay", null },
            {"$fstrobe", null } ,
            {"$fdisplayb", null },
            {"$fstrobeb", null },
            {"$fdisplayh", null },
            {"$fstrobeh", null },
            {"$fdisplayo", null },
            {"$fstrobeo", null },
            {"$ungetc", null },
            {"$fflush", null },
            {"$fmonitor", null },
            {"$fwrite", null },
            {"$fmonitorb", null },
            {"$fwriteb", null },
            {"$fmonitorh", null },
            {"$fwriteh", null },
            {"$fmonitoro", null },
            {"$fwriteo", null },
            {"$readmemb", null },
            {"$readmemh", null },
            {"$swrite", null },
            {"$swriteb", null },
            {"$swriteo", null },
            {"$swriteh", null } ,
            {"$sdf_annotate", null } ,
            // Timescale tasks
            {"$printtimescale", null },
            {"$timeformat", null },
            // Simulation control tasks
            {"$finish", null },
            {"$stop", null },
            // PLA modeling tasks
            {"$async$and$array", null },
            {"$async$and$plane", null },
            {"$async$nand$array", null },
            {"$async$nand$plane", null },
            {"$async$or$array", null },
            {"$async$or$plane", null },
            {"$async$nor$array", null },
            {"$async$nor$plane", null },
            {"$sync$and$array", null },
            {"$sync$and$plane", null },
            {"$sync$nand$array", null },
            {"$sync$nand$plane", null },
            {"$sync$or$array", null },
            {"$sync$or$plane", null },
            {"$sync$nor$array", null },
            {"$sync$nor$plane", null },
            // Stochastic analysis tasks
            {"$q_initialize", null },
            {"$q_add", null },
            {"$q_remove", null },
            {"$q_full", null },
            {"$q_exam", null },

            // Dump
            {"$dumpfile", (word,nameSpace) =>{ return Verilog.Statements.SystemTask.SystemTask.ParseCreate(word,nameSpace); } },
            {"$dumpall",null },
            {"$dumpoff",null },
            {"$dumpon",null },
            {"$dumpvars", (word,nameSpace) =>{ return Verilog.Statements.SystemTask.SkipArguments.ParseCreate(word,nameSpace); }  },
            {"$dumpflush",null },
            {"$dumplimit",null },

            // Elaboration system tasks
            {"$fatal",null },
            {"$error",null },
            {"$warning",null },
            {"$info",null },

            // SystemVerilog
            //Simulation control tasks (20.2)
            //{"$finish",null },
            //{"$stop",null },
            {"$exit",null },
            //Timescale tasks (20.4)
            //{"$printtimescale",null },
            //{"$timeformat",null },
            //Severity tasks (20.10)
            //{"$fatal",null },
            //{"$error",null },
            //{"$warning",null },
            //{"$info",null },
            //Elaboration tasks (20.11)
            //{"$fatal",null },
            //{"$error",null },
            //{"$warning",null },
            //{"$info",null },
            //Assertion control tasks (20.12)
            {"$asserton",null },
            {"$assertoff",null },
            {"$assertkill",null },
            {"$assertcontrol",null },
            {"$assertpasson",null },
            {"$assertpassoff",null },
            {"$assertfailon",null },
            {"$assertfailoff",null },
            {"$assertnonvacuouson",null },
            {"$assertvacuousoff",null },
            //Stochastic analysis tasks (20.16)
            //{"$q_add",null },
            //{"$q_remove",null },
            //{"$q_initialize",null },
            //{"$q_exam",null },
            //PLA modeling tasks (20.17)
            //{"$async$and$array",null },
            //{"$async$and$plane",null },
            //{"$async$nand$array",null },
            //{"$async$nand$plane",null },
            //{"$async$or$array",null },
            //{"$async$or$plane",null },
            //{"$async$nor$array",null },
            //{"$async$nor$plane",null },
            //{"$sync$and$array",null },
            //{"$sync$and$plane",null },
            //{"$sync$nand$array",null },
            //{"$sync$nand$plane",null },
            //{"$sync$or$array",null },
            //{"$sync$or$plane",null },
            //{"$sync$nor$array",null },
            //{"$sync$nor$plane",null },

            // Command line input
            {"$test$plusargs",null },
            {"$value$plusargs",null },

            //Miscellaneous tasks and functions (20.18)
            {"$system",null },

            //Formatting data to a string (21.3.3)
            {"$sformat",null },

            // File I/O tasks / functions usable as statements (21.3)
            // ($fread / $fgets / $fscanf / $sscanf / $fpos / $ftell / etc.
            //  are also registered in SystemFunctions for expression use; they
            //  need a task entry as well when called as a statement.)
            {"$fread",null },
            {"$fgets",null },
            {"$fscanf",null },
            {"$sscanf",null },
            {"$fgetc",null },
            {"$fpos",null },
            {"$ftell",null },
            {"$ferror",null },
            {"$rewind",null },
            {"$fseek",null },
            {"$fopen",null },

            // VCD dump ports tasks (21.7)
            {"$dumpports",null },
            {"$dumpportslimit",null },
            {"$dumpportsoff",null },
            {"$dumpportson",null },
            {"$dumpportsflush",null },
            {"$dumpportsall",null },

            // Cast task (6.24.2)
            {"$cast",null },

            // Coverage control tasks (20.14)
            // ($coverage_control / $coverage_get / $coverage_save etc. are
            //  registered in SystemFunctions for expression use; a statement
            //  position call such as `$coverage_control(...)` as a task needs a
            //  task entry as well.)
            {"$coverage_control",null },
            {"$coverage_get_max",null },
            {"$coverage_get",null },
            {"$coverage_merge",null },
            {"$coverage_save",null },
            {"$get_coverage",null },
            {"$set_coverage_db_name",null },
            {"$load_coverage_db",null },

            // undefined in systemverilog standard
        };
        /*

         
         */

        public Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>?> SystemFunctions { get; }
            = new Dictionary<string, Func<Verilog.DataObjects.Variables.Variable, Verilog.WordScanner>?>
        {
            {"$sformat", null },
            {"$ferror", null },
            {"$rewind", null },
            {"$fseek", null },
            {"$fread", null },
            {"$ftell", null } ,
            {"$sscanf", null } ,
            {"$fscanf", null },
            {"$fgetc", null },
            {"$fgets", null },
            {"$fopen", null },

            // Simulation time functions
            {"$realtime",null },
            {"$stime",null },
            {"$time",null },

            // Conversion functions
            {"$bitstoreal",null },
            {"$realtobits",null },
            {"$itor",null },
            {"$rtoi",null },
            {"$signed",null },
            {"$unsigned",null },

            // Probabilistic distribution functions
            {"$dist_chi_square",null },
            {"$dist_erlang",null },
            {"$dist_exponential",null },
            {"$dist_normal",null },
            {"$dist_poisson",null },
            {"$dist_t",null },
            {"$dist_uniform",null },
            {"$random",null },


            //SystemVerilog
            // 18.13 Random number system functions and methods
            {"$urandom",null },
            {"$urandom_range",null },

            //Simulation time functions (20.3)
            //{"$realtime",null },
            //{"$stime",null },
            //{"$time",null },
            //Conversion functions (20.5)
            //{"$bitstoreal",null },
            //{"$realtobits",null },
            {"$bitstoshortreal",null },
            {"$shortrealtobits",null },
            //{"$itor",null },
            //{"$rtoi",null },
            //{"$signed",null },
            //{"$unsigned",null },
            {"$cast",null },
            //Data query functions (20.6)
            {"$bits",null },
            {"$isunbounded",null },
            {"$typename",null },
            //Array query functions (20.7)
            {"$unpacked_dimensions",null },
            {"$dimensions",null },
            {"$left",null },
            {"$right",null },
            {"$low",null },
            {"$high",null },
            {"$increment",null },
            {"$size",null },
            //Math functions (20.8)
            {"$clog2",null },
            {"$asin",null },
            {"$ln",null },
            {"$acos",null },
            {"$log10",null },
            {"$atan",null },
            {"$exp",null },
            {"$atan2",null },
            {"$sqrt",null },
            {"$hypot",null },
            {"$pow",null },
            {"$sinh",null },
            {"$floor",null },
            {"$cosh",null },
            {"$ceil",null },
            {"$tanh",null },
            {"$sin",null },
            {"$asinh",null },
            {"$cos",null },
            {"$acosh",null },
            {"$tan",null },
            {"$atanh",null },
            //Bit vector system functions (20.9)
            {"$countbits",null },
            {"$countones",null },
            {"$onehot",null },
            {"$onehot0",null },
            {"$isunknown",null },

            //Sampled value system functions (20.13)
            {"$sampled",null },
            {"$rose",null },
            {"$fell",null },
            {"$stable",null },
            {"$changed",null },
            {"$past",null },
            {"$past_gclk",null },
            {"$rose_gclk",null },
            {"$fell_gclk",null },
            {"$stable_gclk",null },
            {"$changed_gclk",null },
            {"$future_gclk",null },
            {"$rising_gclk",null },
            {"$falling_gclk",null },
            {"$steady_gclk",null },
            {"$changing_gclk",null },
            //Coverage control functions (20.14)
            {"$coverage_control",null },
            {"$coverage_get_max",null },
            {"$coverage_get",null },
            {"$coverage_merge",null },
            {"$coverage_save",null },
            {"$get_coverage",null },
            {"$set_coverage_db_name",null },
            {"$load_coverage_db",null },
            //Probabilistic distribution functions (20.15)
            //{"$random",null },
            //{"$dist_chi_square",null },
            //{"$dist_erlang",null },
            //{"$dist_exponential",null },
            //{"$dist_normal",null },
            //{"$dist_poisson",null },
            //{"$dist_t",null },
            //{"$dist_uniform",null },
            //Stochastic analysis functions (20.16)
            //{"$q_full",null },
            //Formatting data to a string (21.3.3)
            {"$sformatf",null },
            // I/O error status(21.3.7)
            {"$feof",null },

            // Command line input (21.6): $test$plusargs / $value$plusargs are
            // also usable in expressions, e.g. if ($test$plusargs("TEST")) ...
            {"$test$plusargs",null },
            {"$value$plusargs",null },

        };


    }
}
