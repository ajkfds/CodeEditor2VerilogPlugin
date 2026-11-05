
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free formatting adapter for LSP hosts.
    ///
    /// <para>
    /// The editor's "autoFormat" tool rewrites the module instantiation under
    /// the caret (port connections aligned, one per line). That logic lives in
    /// <c>ModuleInstantiation.CreateString(indent)</c> and is UI-free; this
    /// adapter drives it and returns plain text edits so a host without an
    /// Avalonia dispatcher (LSP server, unit tests) can serve
    /// <c>textDocument/formatting</c>.
    /// </para>
    ///
    /// <para>
    /// Only module instantiations are reformatted (the same scope as the
    /// editor's tool). Everything else is left untouched, so the returned edit
    /// list is empty when the document has no instantiation to format.
    /// </para>
    /// </summary>
    public static class FormattingAdapter
    {
        /// <summary>One replacement: [Start, End) in the document → NewText.</summary>
        public sealed class TextEditEntry
        {
            public int Start { get; init; }
            public int End { get; init; }
            public string NewText { get; init; } = "";
        }

        /// <summary>
        /// Returns the edits that reformat every module instantiation in the
        /// document, or an empty list when there is nothing to format.
        /// </summary>
        public static IReadOnlyList<TextEditEntry> GetFormattingEdits(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument
            )
        {
            List<TextEditEntry> edits = new List<TextEditEntry>();
            if (file == null || parsedDocument == null) return edits;
            if (parsedDocument.Root == null) return edits;

            // the plugin's CodeDocument adds GetIndentString on top of the
            // editor's; the parse result carries that concrete type
            pluginVerilog.CodeEditor.CodeDocument? codeDocument = parsedDocument.CodeDocument;
            if (codeDocument == null) return edits;

            foreach (Verilog.BuildingBlocks.BuildingBlock block
                     in parsedDocument.Root.BuildingBlocks.Values)
            {
                foreach (Verilog.Items.IBuildingBlockInstantiation inst
                         in block.GetBuildingBlockInstantiations())
                {
                    if (inst is not Verilog.Items.ModuleInstantiation moduleInstantiation) continue;
                    if (moduleInstantiation.BeginIndexReference == null) continue;
                    if (moduleInstantiation.LastIndexReference == null) continue;

                    int start = LastIndex(moduleInstantiation.BeginIndexReference);
                    int end = LastIndex(moduleInstantiation.LastIndexReference);
                    if (start < 0 || end < start || end >= codeDocument.Length) continue;

                    string indent = codeDocument.GetIndentString(start);
                    string? formatted = moduleInstantiation.CreateString(indent);
                    if (string.IsNullOrEmpty(formatted)) continue;

                    // CreateString returns the statement without the trailing
                    // semicolon; the source range already covers it, so append
                    // it when the original text ended with one.
                    string original = codeDocument.CreateString(start, end - start + 1);
                    if (original.TrimEnd().EndsWith(";", StringComparison.Ordinal)
                        && !formatted.TrimEnd().EndsWith(";", StringComparison.Ordinal))
                    {
                        formatted = formatted.TrimEnd() + ";";
                    }

                    if (string.Equals(original, formatted, StringComparison.Ordinal)) continue;

                    edits.Add(new TextEditEntry { Start = start, End = end + 1, NewText = formatted });
                }
            }
            return edits;
        }

        /// <summary>Last index of an IndexReference (the deepest scope offset).</summary>
        private static int LastIndex(Verilog.IndexReference iref)
        {
            IReadOnlyList<int> indexes = iref.Indexes;
            return indexes.Count == 0 ? 0 : indexes[indexes.Count - 1];
        }
    }
}
