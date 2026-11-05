
using System;
using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free signature-help adapter for LSP hosts.
    ///
    /// <para>
    /// The plugin already computes the "argument hint" for the caret position
    /// (the port / parameter label of the argument being typed) and stores it in
    /// <c>CompletionContext.CarletPopupItems</c> during the partial parse. This
    /// adapter drives the same partial parse and converts those popup items into
    /// plain signature strings so a host without an Avalonia dispatcher (LSP
    /// server, unit tests) can serve <c>textDocument/signatureHelp</c>.
    /// </para>
    ///
    /// <para>
    /// The plugin does not model a full call signature (name + all parameters +
    /// active parameter index) at the caret, so the adapter reports the single
    /// hint the editor would show as one signature with no parameter list. That
    /// keeps the LSP result honest: it is exactly what the editor displays.
    /// </para>
    /// </summary>
    public static class SignatureHelpAdapter
    {
        /// <summary>One signature hint (the label the editor would pop up).</summary>
        public sealed class SignatureEntry
        {
            public string Label { get; init; } = "";
        }

        /// <summary>
        /// Returns the signature hints at the given index (caret position), or
        /// null when signature help is unavailable (no parse result / out of
        /// range index / no hint at the caret).
        /// </summary>
        public static IReadOnlyList<SignatureEntry>? GetSignatures(
            Data.VerilogFile file,
            Verilog.ParsedDocument parsedDocument,
            int index
            )
        {
            if (file == null || parsedDocument == null) return null;
            if (index < 0) return null;

            // Same document preparation as CompletionAdapter: the partial parse
            // reads the source text from file.CodeDocument, which is absent in a
            // UI-less host until we create it and load the parsed text.
            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }
            CodeEditor2.CodeEditor.CodeDocument? codeDocument = file.CodeDocument;
            if (codeDocument == null) return null;
            if (parsedDocument.CodeDocument != null && codeDocument.Length != parsedDocument.CodeDocument.Length)
            {
                codeDocument.CopyTextOnlyFrom(parsedDocument.CodeDocument);
            }
            if (index > codeDocument.Length) return null;

            Verilog.CompletionContext? context =
                Data.VerilogCommon.AutoComplete.GetAutoCompleteItems(file, parsedDocument, index);
            if (context == null) return null;

            List<SignatureEntry> entries = new List<SignatureEntry>();
            HashSet<string> seen = new HashSet<string>();
            foreach (CodeEditor2.CodeEditor.PopupHint.PopupItem item in context.CarletPopupItems)
            {
                if (item == null) continue;
                string label = item.CreateString();
                if (string.IsNullOrEmpty(label)) continue;
                if (!seen.Add(label)) continue;
                entries.Add(new SignatureEntry { Label = label });
            }
            return entries.Count == 0 ? null : entries;
        }
    }
}
