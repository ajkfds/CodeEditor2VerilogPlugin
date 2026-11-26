using System.Collections.Generic;
using System.Threading.Tasks;
// Moved out of SystemVerilogCore.Verilog (editor-side consumer), so the parser
// namespace it used to inherit is imported explicitly.
using SystemVerilogCore.Verilog;
// SnippetBuilder now lives in the parent namespace
// (pluginVerilog.Data.VerilogCommon), which is already in scope here.

namespace pluginVerilog.Data.VerilogCommon.EditorSnippets
{
    /// <summary>
    /// The editor's "PortInvert" tool: flips <c>input</c> / <c>output</c> and
    /// the <c>_I</c> / <c>_O</c> name suffix of every selected port
    /// declaration, preserving the original whitespace and comments.
    ///
    /// <para>
    /// The text is built by <see cref="Build"/> here, so the desktop editor and
    /// the VS Code extension invert identically.
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
            CoreBridge.SnippetAdapter.SnippetResult result)
        {
            pluginVerilog.CodeEditor.CodeDocument? doc = parsedDocument.EditorCodeDocument();
            if (doc == null) return;
            if (selectionStart < 0) selectionStart = 0;
            if (selectionEnd > doc.Length) selectionEnd = doc.Length;
            if (selectionEnd <= selectionStart)
            {
                result.Message = "select the port declarations to invert first";
                return;
            }

            string selected = doc.CreateString(selectionStart, selectionEnd - selectionStart);
            string[] lines = selected.Split('\n');
            List<string> processed = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                processed.Add(SnippetBuilder.InvertPortLine(line));
            }
            string text = string.Join("\n", processed);

            result.Snippets.Add(new CoreBridge.SnippetAdapter.SnippetEntry
            {
                Label = "PortInvert",
                Detail = "invert the selected port declarations",
                Kind = CoreBridge.SnippetAdapter.SnippetKind.Replace,
                Start = selectionStart,
                End = selectionEnd,
                SnippetText = text,
                // Literal replacement: the inverted declarations contain no tab
                // stops, so both hosts insert the same text.
                PlainText = text,
            });
        }
    }
}
