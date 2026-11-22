using System;
using System.Threading.Tasks;

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "autoFormat" tool: rewrite the module instantiation under
    /// the caret with <c>ModuleInstantiation.CreateString</c> (one port
    /// connection per line, aligned).
    ///
    /// <para>
    /// This is the same scope as the editor's tool, i.e. <b>only the
    /// instantiation at the caret</b> - not the whole document. The LSP
    /// standard <c>textDocument/formatting</c> is a different feature (it
    /// returns edits for every instantiation in the file), so the tool menu
    /// uses this snippet instead of dispatching to the editor's format
    /// command.
    /// </para>
    ///
    /// <para>
    /// Moved out of <c>pluginVerilog.Verilog.Snippets</c>: the snippet classes
    /// that derive from the editor's <c>InteractiveSnippet</c> are editor-side
    /// consumers, not part of the parser. Only <c>SnippetBuilder</c> (UI-free
    /// text generation, used by <c>CoreBridge.SnippetAdapter</c>) stays under
    /// <c>Verilog/</c>.
    /// </para>
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

        /// <summary>Builds the snippet for the caret at <paramref name="index"/>.</summary>
        public static void Build(
            Verilog.ParsedDocument parsedDocument,
            int index,
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            Verilog.Items.ModuleInstantiation? instantiation =
                Verilog.Snippets.SnippetBuilder.FindInstantiationAt(parsedDocument, index);
            if (instantiation == null)
            {
                result.Message = "the caret is not inside a module instantiation";
                return;
            }
            if (instantiation.LastIndexReference == null)
            {
                result.Message = "the instantiation has no known extent";
                return;
            }

            int start = Verilog.Snippets.SnippetBuilder.LastIndex(instantiation.BeginIndexReference);
            int end = Verilog.Snippets.SnippetBuilder.LastIndex(instantiation.LastIndexReference) + 1;
            string indent = Verilog.Snippets.SnippetBuilder.GetIndent(parsedDocument, start);

            // CreateString returns the statement without the trailing
            // semicolon; the replaced range covers it, so keep it.
            string? text = instantiation.CreateString(indent);
            if (string.IsNullOrEmpty(text))
            {
                result.Message = "the instantiation could not be reformatted";
                return;
            }
            if (!text.TrimEnd().EndsWith(";", StringComparison.Ordinal))
            {
                text = text.TrimEnd() + ";";
            }

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = "autoFormat",
                Detail = "reformat " + instantiation.Name,
                Kind = CoreBridge.SnippetAdapter.SnippetKind.Replace,
                Start = start,
                End = end,
                SnippetText = text,
                // No tab stops: the reformatted text is literal (any '$' or
                // '}' in a port expression must not be read as snippet syntax),
                // so the plain text is what both hosts insert.
                PlainText = text,
            });
        }
    }
}
