using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "autoFormat" tool: reformats the module instantiation under
    /// the caret. The text comes from
    /// <see cref="CoreBridge.SnippetAdapter"/> (shared with the VS Code
    /// extension).
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
    }
}
