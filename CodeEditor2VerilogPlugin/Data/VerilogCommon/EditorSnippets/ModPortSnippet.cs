using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "modport generate" tool: a modport listing every logic
    /// signal of the enclosing interface, with the direction flipped for
    /// signals an existing modport already drives.
    ///
    /// <para>
    /// The text is built by <see cref="ModPortSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c>; <see cref="SnippetRunner"/> inserts it and walks
    /// the fields. This class is only the editor's entry point.
    /// </para>
    /// </summary>
    public class ModPortSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModPortSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.ModPort)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.ModPort).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetProvider.SnippetResult result)
        {
            ModPortSnippetBuilder.Build(parsedDocument, index, result);
        }
    }
}
