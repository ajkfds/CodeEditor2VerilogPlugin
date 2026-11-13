using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free tool menu builder (the VS Code counterpart of the desktop
    /// editor's Shift+Space tool list).
    ///
    /// <para>
    /// The menu contents come from <see cref="ToolMenuCatalog"/>, so the editor
    /// and the extension share one list. This adapter adds the part the editor
    /// cannot show: <b>whether a tool can run at the caret</b>. The desktop
    /// editor lists every tool unconditionally and the tool itself reports
    /// "the caret is not inside a module instantiation" when it cannot run; in
    /// VS Code the user picks from a QuickPick, so it is friendlier to grey the
    /// rows out up front and show the reason as the row's description.
    /// </para>
    ///
    /// <para>
    /// The enablement check reuses <see cref="SnippetAdapter"/> (the same code
    /// that produces the text), so the menu and the tool can never disagree
    /// about whether a tool applies. Nothing here needs an Avalonia dispatcher.
    /// </para>
    /// </summary>
    public static class ToolMenuAdapter
    {
        /// <summary>One row of the menu: the catalog entry plus its availability.</summary>
        public sealed class ToolMenuEntry
        {
            /// <summary>Stable id (one of <see cref="ToolMenuCatalog.ToolId"/>).</summary>
            public string Id { get; init; } = string.Empty;

            /// <summary>Menu label.</summary>
            public string Label { get; init; } = string.Empty;

            /// <summary>One line explaining what the tool does.</summary>
            public string Detail { get; init; } = string.Empty;

            /// <summary><c>false</c> when the tool cannot run at the caret.</summary>
            public bool Enabled { get; init; }

            /// <summary>Why the tool cannot run (only set when disabled).</summary>
            public string? Reason { get; init; }

            /// <summary>
            /// The tool needs a window only the desktop editor owns, so a host
            /// such as VS Code can never run it. The client decides whether to
            /// hide or grey it out.
            /// </summary>
            public bool DesktopOnly { get; init; }
        }

        /// <summary>Result of a tool menu request.</summary>
        public sealed class ToolMenuResult
        {
            /// <summary>Rows in menu order.</summary>
            public List<ToolMenuEntry> Entries { get; } = new();

            /// <summary>Why the menu is empty (only set when it is).</summary>
            public string? Message { get; set; }
        }

        /// <summary>
        /// Builds the menu for the caret at <paramref name="index"/>.
        /// <paramref name="selectionStart"/> / <paramref name="selectionEnd"/>
        /// decide whether the selection-based tools are enabled.
        /// </summary>
        public static ToolMenuResult Build(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            int selectionStart,
            int selectionEnd)
        {
            ToolMenuResult result = new ToolMenuResult();
            if (file == null || parsedDocument?.Root == null)
            {
                result.Message = "the file has not been parsed yet";
                return result;
            }

            bool hasSelection = selectionEnd > selectionStart;

            foreach (ToolMenuCatalog.Entry entry in ToolMenuCatalog.All)
            {
                string? reason = GetDisabledReason(
                    entry, file, parsedDocument, index, hasSelection, selectionStart, selectionEnd);

                result.Entries.Add(new ToolMenuEntry
                {
                    Id = entry.Id,
                    Label = entry.Label,
                    Detail = entry.Detail,
                    Enabled = reason == null,
                    Reason = reason,
                    DesktopOnly = entry.DesktopOnly,
                });
            }
            return result;
        }

        /// <summary>
        /// Returns why <paramref name="entry"/> cannot run at the caret, or null
        /// when it can.
        /// </summary>
        private static string? GetDisabledReason(
            ToolMenuCatalog.Entry entry,
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            bool hasSelection,
            int selectionStart,
            int selectionEnd)
        {
            switch (entry.Id)
            {
                case ToolMenuCatalog.ToolId.AutoConnect:
                    // opens the port list of the instantiation under the caret
                    return SnippetAdapter.HasModuleInstantiationAt(parsedDocument, index)
                        ? null
                        : "the caret is not inside a module instantiation";

                case ToolMenuCatalog.ToolId.ConnectionCheck:
                    // the result is shown in the desktop editor's tree view
                    return "needs the desktop editor's connection window";

                case ToolMenuCatalog.ToolId.ToUpper:
                case ToolMenuCatalog.ToolId.ToLower:
                    return hasSelection ? null : "select the text to transform first";

                case ToolMenuCatalog.ToolId.AlwaysFF:
                case ToolMenuCatalog.ToolId.AutoFormat:
                case ToolMenuCatalog.ToolId.ModuleInstance:
                case ToolMenuCatalog.ToolId.PortConnectionCreate:
                case ToolMenuCatalog.ToolId.PortInvert:
                case ToolMenuCatalog.ToolId.ModPort:
                    // ask the snippet code itself: the menu and the tool can
                    // never disagree about whether a row applies
                    return ProbeSnippetReason(
                        entry.Id, file, parsedDocument, index, selectionStart, selectionEnd);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Asks <see cref="SnippetAdapter"/> what it would produce for the tool.
        /// A message means the tool cannot run (the adapter's messages are
        /// already user facing), an empty result without a message is treated as
        /// "nothing to do here" so the row is not offered either.
        /// </summary>
        private static string? ProbeSnippetReason(
            string toolId,
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            int selectionStart,
            int selectionEnd)
        {
            SnippetAdapter.SnippetResult probe = SnippetAdapter.GetSnippets(
                file, parsedDocument, index, toolId, null, selectionStart, selectionEnd);

            if (!string.IsNullOrEmpty(probe.Message)) return probe.Message;
            if (probe.Snippets.Count == 0 && probe.Candidates.Count == 0)
            {
                return "nothing to do at the caret";
            }
            return null;
        }
    }
}
