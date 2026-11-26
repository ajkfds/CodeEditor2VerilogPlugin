namespace pluginVerilog.Data
{
    /// <summary>
    /// Narrows a parser name-space lookup to the editor's file type.
    ///
    /// <para>
    /// The parser's <c>RegisterNameSpace&lt;T&gt;.GetFile</c> hands back the UI-free
    /// <see cref="SystemVerilogCore.Verilog.IVerilogNameSpaceFile"/> contract, but
    /// editor call sites need their own <see cref="IVerilogRelatedFile"/> (the item
    /// tree, the document, the project). This is the single place where that
    /// narrowing happens, so no call site has to spell out the cast.
    /// </para>
    /// </summary>
    internal static class VerilogNameSpaceExtensions
    {
        /// <summary>
        /// The declaring file of <paramref name="name"/> as the editor's file, or
        /// null when it is unknown or is not an editor file.
        /// </summary>
        public static IVerilogRelatedFile? GetEditorFile<T>(
            this SystemVerilogCore.Verilog.RegisterNameSpace<T> nameSpace,
            string name) where T : class
            => nameSpace.GetFile(name) as IVerilogRelatedFile;
    }
}
