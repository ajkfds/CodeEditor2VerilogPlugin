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
    ///
    /// <para>
    /// The editor-side half (turning a label into a <c>PopupItem</c>) lives in
    /// <c>Data/VerilogCommon/PopupItemLabelExtensions.cs</c>: that one needs the
    /// editor assembly, and this file is used by the parser, so keeping them
    /// together would drag the editor back into <c>Verilog/</c>.
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
    }
}
