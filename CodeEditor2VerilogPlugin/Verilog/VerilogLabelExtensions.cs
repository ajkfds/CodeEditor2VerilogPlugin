namespace pluginVerilog
{
    /// <summary>
    /// Bridges the parser's UI-free label contract
    /// (<see cref="SystemVerilogCore.Documents.VerilogLabel"/>) to the plugin's
    /// own palette enum.
    ///
    /// <para>
    /// <c>VerilogLabel</c> takes <see cref="SystemVerilogCore.Documents.VerilogColorType"/>
    /// so <c>SystemVerilogCore</c> stays free of any UI toolkit. The parser,
    /// however, expresses colours as <see cref="CodeDrawStyle.ColorType"/>. The
    /// numeric values are identical (the contract's enum was defined from this
    /// one), so these extensions are a pure relabel and let every call site keep
    /// writing <c>CodeDrawStyle.ColorType.Keyword</c>.
    /// </para>
    ///
    /// <para>
    /// They are extensions rather than overloads on <c>VerilogLabel</c> because
    /// that type lives in the contract assembly, which must not reference this
    /// one.
    /// </para>
    /// </summary>
    internal static class VerilogLabelExtensions
    {
        /// <summary>Appends text in the given palette colour.</summary>
        public static void AppendText(
            this SystemVerilogCore.Documents.VerilogLabel label,
            string text,
            CodeDrawStyle.ColorType colorType)
        {
            label.AppendText(
                text, (SystemVerilogCore.Documents.VerilogColorType)(byte)colorType);
        }

        /// <summary>Appends another label's runs in order.</summary>
        public static void AppendLabel(
            this SystemVerilogCore.Documents.VerilogLabel label,
            SystemVerilogCore.Documents.VerilogLabel other)
        {
            label.AppendLabel((SystemVerilogCore.Documents.VerilogLabel?)other);
        }

        /// <summary>
        /// Builds the editor's caret-hint item from the parser's UI-free label,
        /// resolving palette indices through the plugin's own palette.
        ///
        /// <para>
        /// The parser only knows palette indices (it must stay UI-free), so the
        /// concrete colours are looked up here - the one place that owns both
        /// the contract and the editor's <c>PopupItem</c>.
        /// </para>
        /// </summary>
        public static CodeEditor2.CodeEditor.PopupHint.PopupItem ToPopupItem(
            this SystemVerilogCore.Documents.VerilogLabel label)
        {
            return new CodeEditor2.CodeEditor.PopupHint.PopupItem(
                label,
                colorType => Global.CodeDrawStyle.Color((CodeDrawStyle.ColorType)(byte)colorType));
        }

        /// <summary>
        /// Appends a parser label to an existing editor hint item
        /// (<c>ParsedDocument.GetPopupItem</c> builds one incrementally and also
        /// mixes in message colours that are not palette entries).
        /// </summary>
        public static void AppendLabel(
            this CodeEditor2.CodeEditor.PopupHint.PopupItem target,
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
            this CodeEditor2.CodeEditor.PopupHint.PopupItem target,
            SystemVerilogCore.Documents.VerilogLabel label,
            object _macroTable)
        {
            target.AppendLabel(label);
        }
    }
}
