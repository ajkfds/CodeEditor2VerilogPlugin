
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free entry point for snippet requests from LSP hosts.
    ///
    /// <para>
    /// The snippet implementations live with the snippets themselves
    /// (<c>Verilog/Snippets/*.cs</c>: <c>AlwaysFFSnippet.Build</c>,
    /// <c>ModuleInstanceSnippet.Build</c>, ...), so the desktop editor and the
    /// VS Code extension insert exactly the same text from one implementation.
    /// This class only <b>dispatches</b> a request by name and keeps the wire
    /// shape (<see cref="SnippetResult"/> / <see cref="SnippetEntry"/>) that the
    /// LSP layer serialises.
    /// </para>
    ///
    /// <para>
    /// Why the editor's snippets cannot be called directly by a host: they are
    /// interactive (<c>CodeEditor2.Snippets.InteractiveSnippet</c>) and drive
    /// <c>Controller.CodeEditor</c>, the popup menu and key events, which does
    /// not exist in a headless LSP server. Their <c>Build</c> methods are static
    /// and Avalonia-free, so both hosts can call them.
    /// </para>
    ///
    /// <para>
    /// The host is responsible only for the mechanical part: calling
    /// <c>TextEditor.insertSnippet</c> (or replacing a range) with the returned
    /// text. Interactive key handling is not reproduced; the tab stops give the
    /// same "jump between the fields" behaviour.
    /// </para>
    /// </summary>
    public static class SnippetAdapter
    {
        /// <summary>One snippet the host can insert or apply as a replacement.</summary>
        public sealed class SnippetEntry
        {
            /// <summary>Human readable name (shown in the command palette / picker).</summary>
            public string Label { get; init; } = "";

            /// <summary>Short description of what the snippet does.</summary>
            public string? Detail { get; init; }

            /// <summary><c>insert</c> (at the caret) or <c>replace</c> (over a range).</summary>
            public string Kind { get; init; } = SnippetKind.Insert;

            /// <summary>Replacement range start (only meaningful for <c>replace</c>).</summary>
            public int Start { get; init; }

            /// <summary>Replacement range end, exclusive (only for <c>replace</c>).</summary>
            public int End { get; init; }

            /// <summary>VS Code snippet text (<c>${1:name}</c> tab stops).</summary>
            public string SnippetText { get; init; } = "";

            /// <summary>
            /// The same snippet as plain text with every field's default inlined.
            /// The desktop editor inserts this and highlights
            /// <see cref="Fields"/>; the VS Code extension inserts
            /// <see cref="SnippetText"/>. Both come from one
            /// <see cref="SnippetTemplate"/>, so they cannot drift.
            /// </summary>
            public string PlainText { get; init; } = "";

            /// <summary>
            /// Where the editable fields sit inside <see cref="PlainText"/>
            /// (empty for snippets without fields).
            /// </summary>
            public IReadOnlyList<SnippetField> Fields { get; init; } = Array.Empty<SnippetField>();

            /// <summary>
            /// <c>true</c> when the snippet has tab stops, i.e. the user is
            /// expected to fill something in.
            /// </summary>
            public bool HasFields => Fields.Count > 0;
        }

        /// <summary>Result of a snippet request.</summary>
        public sealed class SnippetResult
        {
            /// <summary>Snippets to apply (usually one).</summary>
            public List<SnippetEntry> Snippets { get; } = new();

            /// <summary>
            /// When the request needs a further choice (e.g. which module to
            /// instantiate), the candidate names. The host shows a picker and
            /// re-requests with the chosen name.
            /// </summary>
            public List<string> Candidates { get; } = new();

            /// <summary>Why nothing was produced (shown to the user).</summary>
            public string? Message { get; set; }
        }

        /// <summary>Snippet kinds understood by <see cref="GetSnippets"/>.</summary>
        public static class SnippetKind
        {
            public const string Insert = "insert";
            public const string Replace = "replace";
        }

        /// <summary>
        /// Requested snippet names (the <c>kind</c> parameter). The values match
        /// <see cref="ToolMenuCatalog.ToolId"/> so the tool menu can probe the
        /// same snippets it offers.
        /// </summary>
        public static class SnippetName
        {
            public const string AlwaysFF = "alwaysFF";
            public const string AutoFormat = "autoFormat";
            public const string ModuleInstance = "moduleInstance";
            public const string PortConnectionCreate = "portConnectionCreate";
            public const string PortInvert = "portInvert";
            public const string ModPort = "modport";
        }

        /// <summary>
        /// Dispatches the request to the snippet implementation.
        /// </summary>
        /// <param name="file">The file being edited (its parse result is used).</param>
        /// <param name="parsedDocument">Parse result of <paramref name="file"/>.</param>
        /// <param name="index">Caret offset in the document.</param>
        /// <param name="name">One of <see cref="SnippetName"/>.</param>
        /// <param name="argument">
        /// Extra argument: the module name for <c>moduleInstance</c> /
        /// <c>portConnectionCreate</c>. Ignored by the other snippets.
        /// </param>
        /// <param name="selectionStart">Selection start (for <c>portInvert</c>).</param>
        /// <param name="selectionEnd">Selection end, exclusive (for <c>portInvert</c>).</param>
        public static SnippetResult GetSnippets(
            Data.IVerilogRelatedFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string name,
            string? argument,
            int selectionStart,
            int selectionEnd)
        {
            SnippetResult result = new SnippetResult();
            if (file == null || parsedDocument == null)
            {
                result.Message = "the file has not been parsed yet";
                return result;
            }
            if (parsedDocument.Root == null)
            {
                result.Message = "the file has not been parsed yet";
                return result;
            }

            switch (name)
            {
                case SnippetName.AlwaysFF:
                    Data.VerilogCommon.EditorSnippets.AlwaysFFSnippet.Build(parsedDocument, index, result);
                    break;
                case SnippetName.AutoFormat:
                    Data.VerilogCommon.EditorSnippets.AutoFormatSnippet.Build(parsedDocument, index, result);
                    break;
                case SnippetName.ModuleInstance:
                    Data.VerilogCommon.EditorSnippets.ModuleInstanceSnippet.Build(file, parsedDocument, index, argument, result);
                    break;
                case SnippetName.PortConnectionCreate:
                    Data.VerilogCommon.EditorSnippets.PortConnectionCreateSnippet.Build(parsedDocument, index, result);
                    break;
                case SnippetName.PortInvert:
                    Data.VerilogCommon.EditorSnippets.PortInvertSnippet.Build(parsedDocument, selectionStart, selectionEnd, result);
                    break;
                case SnippetName.ModPort:
                    Data.VerilogCommon.EditorSnippets.ModPortSnippet.Build(parsedDocument, index, result);
                    break;
                default:
                    result.Message = "unknown snippet: " + name;
                    break;
            }
            return result;
        }

        /// <summary>
        /// True when the caret sits on a module instantiation, i.e. the tools
        /// that rewrite an instantiation (autoFormat / portConnectionCreate /
        /// autoConnect) can run here. Used by <see cref="ToolMenuAdapter"/> to
        /// decide whether a menu row is enabled; the lookup itself is the same
        /// one the snippets use, so the menu and the tools never disagree.
        /// </summary>
        public static bool HasModuleInstantiationAt(
            Verilog.ParsedDocument parsedDocument,
            int index)
        {
            return Data.VerilogCommon.SnippetBuilder.HasModuleInstantiationAt(parsedDocument, index);
        }
    }
}
