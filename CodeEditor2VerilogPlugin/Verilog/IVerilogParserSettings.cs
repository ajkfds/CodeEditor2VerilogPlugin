using System;
using System.Collections.Generic;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Items;
using pluginVerilog.Verilog.Statements.SystemTask;
using Macro = SystemVerilogCore.Verilog.Macro;
using RuleSet = SystemVerilogCore.Verilog.RuleSet;
using AnnotationCommands = SystemVerilogCore.Verilog.AnnotationCommands;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// The project settings surface that the parser itself reads, including the
    /// parts that are typed with parser types.
    ///
    /// <para>
    /// <b>Why this is separate from
    /// <see cref="SystemVerilogCore.Projects.IVerilogProjectSettings"/>.</b>
    /// That contract lives in the UI-free assembly, so it can only carry values
    /// that assembly knows about. Three of the settings the parser reads are
    /// typed with parser types:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item><c>SystemTaskParsers</c> - <c>Func&lt;WordScanner, NameSpace,
    /// SystemTask&gt;</c>, the per-task statement parsers;</item>
    /// <item><c>SystemFunctions</c> - <c>Func&lt;Variable, WordScanner&gt;</c>, the
    /// per-function expression parsers;</item>
    /// <item><c>GetInstancedBuildingBlock</c> - takes the
    /// <see cref="IBuildingBlockInstantiation"/> whose parameter overrides select
    /// the instance parse result.</item>
    /// </list>
    ///
    /// <para>
    /// Putting those on the UI-free contract would make it depend on the parser,
    /// which is the opposite of the intended direction (the parser moves into the
    /// UI-free assembly, not the other way round). So they live here instead, next
    /// to the parser, and move with it. The rest of the settings - the name
    /// spaces, macros, annotation commands, rule set and library paths - stay on
    /// the UI-free contract, where the language server can implement them without
    /// the parser.
    /// </para>
    ///
    /// <para>
    /// <b>Why the member shapes match the editor's concrete class exactly.</b>
    /// The name spaces are exposed as the concrete
    /// <see cref="RegisterNameSpace{T}"/> instantiations rather than as the
    /// non-generic <see cref="SystemVerilogCore.Verilog.IVerilogNameSpace"/> view,
    /// and the tables as <see cref="IReadOnlyDictionary{TKey,TValue}"/>. Parse
    /// sites then keep calling <c>Get</c> / <c>GetFile</c> with the element type
    /// they expect, so moving them onto this contract changes no call site. The
    /// non-generic view exists for hosts that cannot name the element type; the
    /// parser always can.
    /// </para>
    /// </summary>
    public interface IVerilogParserSettings
    {
        // ---- name spaces -------------------------------------------------
        // Same three tables as IVerilogProjectSettings, but typed so the parser
        // can narrow the result without a cast at every call site.

        /// <summary>
        /// Top-level module / interface / program / primitive declarations.
        /// </summary>
        RegisterNameSpace<BuildingBlock> DefinitionNameSpace { get; }

        /// <summary>Package declarations.</summary>
        RegisterNameSpace<Package> PackageNameSpace { get; }

        /// <summary>
        /// Compilation-unit-scope declarations (root-level classes, functions,
        /// tasks, typedefs).
        /// </summary>
        RegisterNameSpace<INamedElement> UnitNameSpace { get; }

        // ---- parser tables -----------------------------------------------

        /// <summary>The `` `define `` macros of this project, by name.</summary>
        IReadOnlyDictionary<string, Macro> Macros { get; }

        /// <summary>
        /// Statement parsers for the system tasks this project accepts, by name
        /// (including the <c>$</c>). A null value means "known, nothing extra to
        /// parse".
        /// </summary>
        IReadOnlyDictionary<string, Func<WordScanner, NameSpace, SystemTask>?> SystemTaskParsers { get; }

        /// <summary>
        /// Expression parsers for the system functions this project accepts, by
        /// name (including the <c>$</c>).
        /// </summary>
        IReadOnlyDictionary<string, Func<Variable, WordScanner>?> SystemFunctions { get; }

        // ---- annotations and rules ---------------------------------------

        /// <summary>
        /// The comment annotation command words (<c>@sync</c>, <c>@clock</c>, ...).
        /// </summary>
        AnnotationCommands AnnotationCommands { get; }

        /// <summary>
        /// Separator between an annotation key and its value
        /// (<c>@sync:CLK_I</c> -&gt; <c>":"</c>).
        /// </summary>
        string AnnotationKeyValueDelimiter { get; }

        /// <summary>
        /// The project-wide default rule set (severity settings).
        ///
        /// <para>
        /// A <c>.rule.yaml</c> file overrides this per directory; parse sites that
        /// know the file they are parsing call <see cref="GetRuleSet"/> instead.
        /// </para>
        /// </summary>
        RuleSet RuleSet { get; }

        /// <summary>
        /// The rule set that applies to <paramref name="relativePath"/>: the
        /// deepest directory with a <c>.rule.yaml</c> file wins, falling back to
        /// <see cref="RuleSet"/>.
        /// </summary>
        RuleSet GetRuleSet(string? relativePath);

        // ---- external libraries ------------------------------------------

        /// <summary>
        /// Modules provided by an external library, mapped to their library path.
        /// Such a module is not an error when it is not found in the project.
        /// </summary>
        IReadOnlyDictionary<string, string> ExtenralModuleLibraryPath { get; }

        /// <summary>
        /// Primitives (UDPs) provided by an external library, mapped to their
        /// library path.
        /// </summary>
        IReadOnlyDictionary<string, string> ExtenralPrimitiveLibraryPath { get; }

        // ---- instance resolution -----------------------------------------

        /// <summary>
        /// The building block that <paramref name="instantiation"/> refers to,
        /// with its parameter overrides applied.
        ///
        /// <para>
        /// An instantiation with overrides (<c>child #(.WIDTH(4)) u(...)</c>) was
        /// parsed again under a key derived from the module name and the
        /// overrides. Resolving it has to return <em>that</em> parse result, or
        /// the port widths and <c>generate</c> branches would be wrong.
        /// </para>
        /// </summary>
        BuildingBlock? GetInstancedBuildingBlock(IBuildingBlockInstantiation instantiation);
    }
}
