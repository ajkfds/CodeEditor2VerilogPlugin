using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "moduleInstance" tool: lists every module of the project so
    /// the user can pick one to instantiate.
    ///
    /// <para>
    /// The candidate list and the instantiation text both come from
    /// <see cref="ModuleInstanceSnippet.Build"/>, and <see cref="SnippetRunner"/>
    /// shows the picker, so this tool is the same two step flow the VS Code
    /// extension uses (ask without an argument, get candidates, ask again with
    /// the chosen name). Keeping the list here would duplicate that logic.
    /// </para>
    /// </summary>
    public class ModuleInstanceMenuSnippet : CodeEditor2.Snippets.InteractiveSnippet
    {
        public ModuleInstanceMenuSnippet() : base(CoreBridge.ToolMenuCatalog.ToolId.ModuleInstance)
        {
            IconImage = Global.Icons.TryGetSvgBitmap(
                    "CodeEditor2/Assets/Icons/wrench.svg",
                    Plugin.ThemeColor
                    );
        }

        public override Task ApplyAsync()
        {
            return new SnippetRunner(CoreBridge.ToolMenuCatalog.ToolId.ModuleInstance).ApplyAsync();
        }
    }
}
