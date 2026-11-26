using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "Fill Port Connections" tool: rewrite the instantiation at
    /// the caret with one named connection per port, deriving a signal name from
    /// each unconnected port.
    ///
    /// <para>
    /// The text is built by
    /// <see cref="PortConnectionCreateSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c>. This class is only the editor's entry point.
    /// </para>
    /// </summary>
    public class PortConnectionCreateSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public PortConnectionCreateSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.PortConnectionCreate)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.PortConnectionCreate).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetProvider.SnippetResult result)
        {
            PortConnectionCreateSnippetBuilder.Build(parsedDocument, index, result);
        }
    }
}
