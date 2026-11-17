using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "modport generate" tool. The text comes from
    /// <see cref="CoreBridge.SnippetAdapter"/> (shared with the VS Code
    /// extension); <see cref="SnippetRunner"/> inserts it and walks the fields.
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
    }
}
