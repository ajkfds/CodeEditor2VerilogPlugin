using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
using SystemVerilogCore.Snippets;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "PortInvert" tool: flips <c>input</c> / <c>output</c> and
    /// the <c>_I</c> / <c>_O</c> name suffix of every selected port
    /// declaration, preserving the original whitespace and comments.
    ///
    /// <para>
    /// The text is built by <see cref="PortInvertSnippetBuilder.Build"/> in
    /// <c>SystemVerilogCore</c>, so the desktop editor and the VS Code extension
    /// invert identically. This class is only the editor's entry point.
    /// </para>
    /// </summary>
    public class PortInvertSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public PortInvertSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.PortInvert)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.PortInvert).ApplyAsync();
        }

        /// <summary>Builds the snippet for the selected range.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int selectionStart,
            int selectionEnd,
            SnippetProvider.SnippetResult result)
        {
            PortInvertSnippetBuilder.Build(parsedDocument, selectionStart, selectionEnd, result);
        }
    }
}
