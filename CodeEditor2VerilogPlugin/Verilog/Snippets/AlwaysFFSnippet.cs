using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Snippets
{
    /// <summary>
    /// The editor's "alwaysFF" tool.
    ///
    /// <para>
    /// The text is produced by <see cref="CoreBridge.SnippetAdapter"/> and
    /// driven by <see cref="SnippetRunner"/>, so the desktop editor and the
    /// VS Code extension insert exactly the same block from one
    /// implementation. This class only exists so the editor's tool list has a
    /// named entry (and an icon).
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
    }
}
