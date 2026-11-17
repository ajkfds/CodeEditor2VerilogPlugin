using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "moduleInstance" snippet for one named module: the full
    /// instantiation (parameter overrides + one named port connection per
    /// port).
    ///
    /// <para>
    /// The text comes from <see cref="CoreBridge.SnippetAdapter"/>, which the
    /// VS Code extension also uses (through <c>rtl/snippet</c> and the
    /// completion adapter), so the editor and the extension insert the same
    /// instantiation. This class only carries the module name.
    /// </para>
    /// </summary>
    public class ModuleInstanceSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        private readonly string moduleName;

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
    }
}
