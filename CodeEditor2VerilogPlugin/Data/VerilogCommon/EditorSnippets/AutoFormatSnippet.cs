using System.Threading.Tasks;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "autoFormat" tool: rewrite the module instantiation under
    /// the caret with <c>ModuleInstantiation.CreateString</c> (one port
    /// connection per line, aligned).
    ///
    /// <para>
    /// This is the same scope as the editor's tool, i.e. <b>only the
    /// instantiation at the caret</b> - not the whole document. The LSP
    /// standard <c>textDocument/formatting</c> is a different feature (it
    /// returns edits for every instantiation in the file), so the tool menu
    /// uses this snippet instead of dispatching to the editor's format
    /// command.
    /// </para>
    ///
    /// <para>
    /// The text is built by <see cref="AutoFormatSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c>. This class is only the editor's entry point.
    /// </para>
    /// </summary>
    public class AutoFormatSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AutoFormatSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.AutoFormat)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.AutoFormat).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetProvider.SnippetResult result)
        {
            AutoFormatSnippetBuilder.Build(parsedDocument, index, result);
        }
    }
}
