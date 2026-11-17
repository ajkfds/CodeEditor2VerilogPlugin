using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "PortInvert" tool: flips input/output and the <c>_I</c> /
    /// <c>_O</c> suffix of the selected port declarations. The text comes from
    /// <see cref="CoreBridge.SnippetAdapter"/> (shared with the VS Code
    /// extension), so both hosts invert identically.
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
    }
}
