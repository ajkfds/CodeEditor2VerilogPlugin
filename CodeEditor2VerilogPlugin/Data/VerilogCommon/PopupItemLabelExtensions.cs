using CodeEditor2.CodeEditor.PopupHint;

namespace pluginVerilog
{
    /// <summary>
    /// Bridges the parser's UI-free label contract
    /// (<see cref="SystemVerilogCore.Documents.VerilogLabel"/>) to the editor's
    /// hint item.
    ///
    /// <para>
    /// This is the one place that owns both the contract and the editor's
    /// <c>PopupItem</c>. It lives here (the editor-side <c>Data/VerilogCommon</c>
    /// layer), not next to the parser: the parser only produces palette indices,
    /// so turning them into concrete colours is this host's job. Keeping it out
    /// of <c>Verilog/</c> is what lets the parser namespace stay free of the
    /// editor assembly.
    /// </para>
    /// </summary>
    internal static class PopupItemLabelExtensions
    {
        /// <summary>
        /// Builds the editor's caret-hint item from the parser's UI-free label,
        /// resolving palette indices through the plugin's own palette.
        /// </summary>
        public static PopupItem ToPopupItem(
            this SystemVerilogCore.Documents.VerilogLabel label)
        {
            return new PopupItem(
                label,
                colorType => Global.CodeDrawStyle.Color((CodeDrawStyle.ColorType)(byte)colorType));
        }

        /// <summary>
        /// Appends a parser label to an existing editor hint item (the
        /// mouse-over hint is built incrementally and also mixes in message
        /// colours that are not palette entries).
        /// </summary>
        public static void AppendLabel(
            this PopupItem target,
            SystemVerilogCore.Documents.VerilogLabel label)
        {
            if (label == null) return;
            foreach (SystemVerilogCore.Documents.VerilogLabelRun run in label.Runs)
            {
                if (run.ColorType is SystemVerilogCore.Documents.VerilogColorType colorType)
                {
                    target.AppendText(
                        run.Text,
                        Global.CodeDrawStyle.Color((CodeDrawStyle.ColorType)(byte)colorType));
                }
                else
                {
                    target.AppendText(run.Text);
                }
            }
        }

        /// <summary>
        /// Appends a macro's label to an existing editor hint item.
        /// </summary>
        public static void AppendLabel(
            this PopupItem target,
            SystemVerilogCore.Documents.VerilogLabel label,
            object _macroTable)
        {
            target.AppendLabel(label);
        }
    }
}
