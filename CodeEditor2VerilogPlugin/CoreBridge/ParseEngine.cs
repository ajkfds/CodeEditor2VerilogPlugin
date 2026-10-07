
using System;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// UI-free parse engine.
    /// Drives the real VerilogParser on in-memory text and returns the
    /// resulting Verilog.ParsedDocument without touching the editor UI.
    /// Hosts without an Avalonia dispatcher (LSP server, unit tests) use
    /// this entry point instead of TextFile.PostParse / ParseWorker.
    /// </summary>
    public static class ParseEngine
    {
        /// <summary>
        /// Parse the given SystemVerilog text and return the parsed document.
        /// The parse itself (WordScanner / Root.ParseCreateAsync) is UI-free;
        /// only the editor-side acceptance (color marks, refresh) touches the
        /// UI, and that path is not invoked here.
        /// </summary>
        public static async Task<Verilog.ParsedDocument?> ParseSystemVerilogAsync(
            string text,
            Data.VerilogFile file,
            CancellationToken? cancellationToken = null
            )
        {
            if (text == null) return null;

            // The DocumentParser base constructor copies the text from
            // file.CodeDocument. In UI-less hosts FileCheckAsync has not run,
            // so create the code document here (the CodeDocument setter is
            // protected; CreateCodeDocument is the intended factory).
            if (file.CodeDocument == null)
            {
                System.Reflection.MethodInfo? create = file.GetType().GetMethod(
                    "CreateCodeDocument",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                create?.Invoke(file, null);
            }

            pluginVerilog.Parser.VerilogParser parser = new pluginVerilog.Parser.VerilogParser(
                file,
                CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.BackgroundParse,
                cancellationToken
                );

            // load the target text into the parser's working document.
            // CodeDocument has no public text setter; CopyTextOnlyFrom copies
            // the raw text (and clears color/mark info) from another document.
            CodeEditor2.CodeEditor.CodeDocument sourceDocument = new CodeEditor2.CodeEditor.CodeDocument(file, text);
            parser.Document.CopyTextOnlyFrom(sourceDocument);

            // run the (UI-free) parse
            await parser.ParseAsync(cancellationToken ?? CancellationToken.None);

            return parser.VerilogParsedDocument;
        }
    }
}
