using CodeEditor2.CodeEditor;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that exposes a plugin-side <see cref="CodeEditor2.CodeEditor.CodeDocument"/>
    /// (which may be the plugin-specific subclass
    /// <c>pluginVerilog.CodeEditor.CodeDocument</c>) through the UI-agnostic
    /// <see cref="ISystemVerilogCodeDocument"/> surface. Only the read-only
    /// surface needed by the language tooling is exposed; mutation helpers
    /// intentionally stay on the host side.
    /// </summary>
    public sealed class SystemVerilogCodeDocumentAdapter : ISystemVerilogCodeDocument
    {
        private readonly CodeDocument _document;

        public SystemVerilogCodeDocumentAdapter(CodeDocument document)
        {
            _document = document;
        }

        public int Length => _document.Length;
        public int LineCount => _document.Lines;
        public ulong Version => _document.Version;

        public char GetCharAt(int index) => _document.GetCharAt(index);

        public string GetText() => _document.CreateString();

        public string GetText(int startIndex, int length) => _document.CreateString(startIndex, length);

        public string GetLineText(int line) => _document.CreateLineString(line);

        public int GetLineAt(int index) => _document.GetLineAt(index);

        public int GetLineStartIndex(int line) => _document.GetLineStartIndex(line);

        public int GetLineLength(int line) => _document.GetLineLength(line);

        public bool TryGetWord(int index, out int wordStart, out int wordLength)
        {
            _document.GetWord(index, out wordStart, out wordLength);
            return wordLength > 0;
        }
    }
}
