using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "alwaysFF" tool: an <c>always @(posedge clk or negedge
    /// rst)</c> block with the clock / reset taken from the ports the parser
    /// marked as clock / reset (SyncContext).
    ///
    /// <para>
    /// The text is built by <see cref="AlwaysFFSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c> and inserted by <see cref="SnippetRunner"/>, so
    /// the desktop editor and the VS Code extension insert exactly the same
    /// block from one implementation. This class is only the editor's entry
    /// point (it owns the icon and the tool id).
    /// </para>
    /// </summary>
    public class AlwaysFFSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public AlwaysFFSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.AlwaysFF)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.AlwaysFF).ApplyAsync();
        }

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            SnippetProvider.SnippetResult result)
        {
            AlwaysFFSnippetBuilder.Build(parsedDocument, index, result);
        }
    }
}
