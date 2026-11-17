using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "portConnectionCreate" tool: rewrites the instantiation
    /// under the caret with one named connection per port. The text comes from
    /// <see cref="CoreBridge.SnippetAdapter"/> (shared with the VS Code
    /// extension); <see cref="SnippetRunner"/> inserts it and walks the fields.
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
    }
}
