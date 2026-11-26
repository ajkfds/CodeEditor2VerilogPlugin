using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "moduleInstance" snippet for one named module: the full
    /// instantiation (parameter overrides + one named port connection per
    /// port).
    ///
    /// <para>
    /// The text is built by
    /// <see cref="ModuleInstanceSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c>, which the VS Code extension also uses (through
    /// <c>rtl/snippet</c> and the completion adapter), so the editor and the
    /// extension insert the same instantiation. This class is only the editor's
    /// entry point.
    /// </para>
    /// </summary>
    public class ModuleInstanceSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        private readonly string moduleName;

        /// <remarks>
        /// The base text is the MODULE NAME, not the tool id: the base
        /// <c>ToolItem.Text</c> is what the autocomplete contract reports as
        /// <c>Label</c>, so a candidate listed in the editor's popup reads
        /// "leaf" (the module), not "moduleInstance".
        /// </remarks>
        public ModuleInstanceSnippet(string moduleName) : base(moduleName)
        {
            this.moduleName = moduleName;
            // TryGetSvgBitmap: the snippet is also constructed while PARSING
            // (CompletionContext.AppendModuleInstanceSnippets builds one per
            // module name at the start of a module-body line), and the parser
            // runs in the headless language server too, where Avalonia's asset
            // loader is absent. A throwing icon lookup aborted the whole
            // textDocument/completion request there, so the module-name
            // candidates never reached the client.
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2VerilogPlugin/Assets/Icons/module.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.ModuleInstance, moduleName).ApplyAsync();
        }

        /// <summary>
        /// Builds the snippet for the caret at <paramref name="index"/>. Without
        /// <paramref name="moduleName"/> the candidate module names are returned
        /// so the host can show a picker.
        /// </summary>
        public static void Build(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string? moduleName,
            SnippetProvider.SnippetResult result)
        {
            ModuleInstanceSnippetBuilder.Build(file, parsedDocument, index, moduleName, result);
        }
    }
}
