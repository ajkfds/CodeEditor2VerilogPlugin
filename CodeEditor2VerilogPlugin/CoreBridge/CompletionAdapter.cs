
using System;
using System.Collections.Generic;
using pluginVerilog.Data.VerilogCommon;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free completion adapter for LSP hosts.
    /// Drives the plugin's Verilog.CompletionContext (partial parse based
    /// autocomplete) on an already-parsed document and converts the result
    /// into plain (text, kind) entries so hosts without an Avalonia
    /// dispatcher (LSP server, unit tests) can serve textDocument/completion.
    /// </summary>
    public static class CompletionAdapter
    {
        public sealed class CompletionEntry
        {
            public string Text { get; init; } = "";
            /// <summary>LSP CompletionItemKind value.</summary>
            public int Kind { get; init; }
            public string Detail { get; init; } = "";

            /// <summary>
            /// Text actually inserted when the entry is accepted. Null means
            /// "insert <see cref="Text"/> as-is" (plain text).
            /// </summary>
            public string? InsertText { get; init; }

            /// <summary>
            /// LSP InsertTextFormat: 1 = PlainText, 2 = Snippet.
            /// Only meaningful when <see cref="InsertText"/> is set.
            /// </summary>
            public int InsertTextFormat { get; init; } = 1;
        }

        /// <summary>
        /// Returns completion items at the given index (caret position),
        /// or null when completion is unavailable (no parse result / out of
        /// range index).
        /// </summary>
        public static IReadOnlyList<CompletionEntry>? GetCompletionItems(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index
            )
        {
            if (file == null || parsedDocument == null) return null;
            if (index < 0) return null;

            // The CompletionContext partial parse reads the source text from
            // file.CodeDocument, which a UI-less host has not loaded yet.
            CodeEditor2.CodeEditor.CodeDocument? codeDocument =
                parsedDocument.PrepareCompletionBuffer(file);
            if (codeDocument == null) return null;
            if (index > codeDocument.Length) return null;

            // runs the real CompletionContext (partial parse of the region
            // around the caret, UI-free; logs are posted to a dispatcher that
            // may not run in headless hosts, which is harmless)
            Verilog.CompletionContext? context =
                Data.VerilogCommon.AutoComplete.GetAutoCompleteItems(file, parsedDocument, index);
            if (context == null) return null;

            List<CompletionEntry> entries = new List<CompletionEntry>();
            HashSet<string> seen = new HashSet<string>();
            // The list holds the UI-free contract, so this adapter never needs
            // the editor's candidate type (it used to iterate ToolItem and cast
            // each entry back to the contract).
            foreach (SystemVerilogCore.Documents.IVerilogAutocompleteItem contractItem in context.AutoCompleteItems)
            {
                if (contractItem == null) continue;
                string text = contractItem.Label;
                if (string.IsNullOrEmpty(text)) continue;
                if (!seen.Add(text)) continue; // reject duplicated entries

                // LSP CompletionItemKind: 3=Function, 6=Variable, 9=Module,
                // 14=Keyword.
                int kind = 14;
                string detail = "";
                {
                    kind = contractItem.Kind switch
                    {
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Function => 3,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Task => 3,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Variable => 6,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Net => 6,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Parameter => 6,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.LocalParameter => 6,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Port => 6,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Module => 9,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Interface => 9,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Package => 9,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Class => 7,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Typedef => 7,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Macro => 14,
                        SystemVerilogCore.Documents.SystemVerilogAutocompleteItemKind.Snippet => 15,
                        _ => 14,
                    };
                    detail = contractItem.Kind.ToString();
                }

                // A module-name candidate is a ModuleInstanceSnippet, i.e. a
                // ToolItem rather than an AutoCompleteItem: in the editor,
                // accepting it runs ApplyAsync(), which inserts a FULL
                // instantiation (parameter overrides + one named connection per
                // port) and not just the module name. Report it as a Module
                // candidate with that text as the snippet body, otherwise the
                // VS Code host inserts the bare name - the "module name
                // completion is only a keyword completion" symptom.
                if (contractItem is Data.VerilogCommon.EditorSnippets.ModuleInstanceSnippet)
                {
                    kind = 9; // LSP CompletionItemKind.Module
                    entries.Add(new CompletionEntry
                    {
                        Text = text,
                        Kind = kind,
                        Detail = "module",
                        InsertText = BuildModuleInstanceSnippet(
                            file, parsedDocument, index, text),
                        InsertTextFormat = 2, // Snippet
                    });
                    continue;
                }

                CompletionEntry entry = new CompletionEntry { Text = text, Kind = kind, Detail = detail };

                // Block keywords expand to a full block (begin ... end etc.) as a
                // VS Code snippet, mirroring the editor's BeginAutoCompleteItem /
                // CaseAutocompleteItem / ... ApplyAsync behaviour. Without this the
                // LSP host only inserts the bare keyword.
                string? snippet = BuildBlockSnippet(text, codeDocument, index);
                if (snippet != null)
                {
                    entry = new CompletionEntry
                    {
                        Text = text,
                        Kind = kind,
                        Detail = detail,
                        InsertText = snippet,
                        InsertTextFormat = 2, // Snippet
                    };
                }

                entries.Add(entry);
            }
            return entries;
        }

        /// <summary>
        /// Builds the snippet body for a module-name completion candidate: the
        /// full instantiation the editor inserts when the candidate is accepted
        /// (parameter overrides + one named port connection per port, with
        /// <c>${n:name}</c> tab stops).
        ///
        /// <para>
        /// The editor gets this by running
        /// <c>ModuleInstanceSnippet.ApplyAsync</c>, which is interactive (it
        /// drives the popup menu / caret), so it cannot run in a headless host.
        /// <see cref="SnippetAdapter"/> holds the same text generation without
        /// the UI, so it is reused here. Falls back to the bare module name when
        /// the module cannot be resolved (e.g. it is not in the project's
        /// definition namespace yet).
        /// </para>
        /// </summary>
        private static string BuildModuleInstanceSnippet(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index,
            string moduleName)
        {
            try
            {
                SnippetAdapter.SnippetResult result = SnippetAdapter.GetSnippets(
                    file,
                    parsedDocument,
                    index,
                    SnippetAdapter.SnippetName.ModuleInstance,
                    moduleName,
                    0,
                    0);
                foreach (SnippetAdapter.SnippetEntry entry in result.Snippets)
                {
                    if (!string.IsNullOrEmpty(entry.SnippetText)) return entry.SnippetText;
                }
            }
            catch (Exception)
            {
                // never fail completion over the snippet body
            }
            return moduleName;
        }

        /// <summary>
        /// Builds a VS Code snippet body for block-opening keywords, or null when
        /// the keyword has no block expansion. The indentation of the line the
        /// caret sits on is reused for the closing keyword.
        /// </summary>
        private static string? BuildBlockSnippet(
            string keyword,
            CodeEditor2.CodeEditor.CodeDocument codeDocument,
            int index)
        {
            string indent = GetLineIndent(codeDocument, index);
            string inner = indent + "\t";

            switch (keyword)
            {
                case "begin":
                    return "begin\n" + inner + "$0\n" + indent + "end";
                case "fork":
                    return "fork\n" + inner + "$0\n" + indent + "join";
                case "case":
                    return "case (${1})\n" + inner + "$0\n" + indent + "endcase";
                case "casex":
                    return "casex (${1})\n" + inner + "$0\n" + indent + "endcase";
                case "casez":
                    return "casez (${1})\n" + inner + "$0\n" + indent + "endcase";
                case "generate":
                    return "generate\n" + inner + "$0\n" + indent + "endgenerate";
                case "module":
                    return "module ${1:name};\n" + inner + "$0\n" + indent + "endmodule";
                case "interface":
                    return "interface ${1:name};\n" + inner + "$0\n" + indent + "endinterface";
                case "function":
                    return "function ${1:type} ${2:name};\n" + inner + "$0\n" + indent + "endfunction";
                case "task":
                    return "task ${1:name};\n" + inner + "$0\n" + indent + "endtask";
                default:
                    return null;
            }
        }

        /// <summary>Leading whitespace of the line containing <paramref name="index"/>.</summary>
        private static string GetLineIndent(CodeEditor2.CodeEditor.CodeDocument codeDocument, int index)
        {
            if (index < 0) return "";
            if (index > codeDocument.Length) index = codeDocument.Length;

            int line = codeDocument.GetLineAt(index);
            int lineStart = codeDocument.GetLineStartIndex(line);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = lineStart; i < codeDocument.Length; i++)
            {
                char c = codeDocument.GetCharAt(i);
                if (c != ' ' && c != '\t') break;
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
