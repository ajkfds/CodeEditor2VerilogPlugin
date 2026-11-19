using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace pluginVerilog.Parser
{
    /// <summary>
    /// Parser for <c>.rule.yaml</c> files.
    ///
    /// <para>
    /// The file is YAML (<c>rules:</c> / <c>- name:</c> / <c>severity:</c>).
    /// This parser only provides the syntax highlighting of that shape and the
    /// error marking of malformed entries; the rule set itself is built by
    /// <see cref="RuleSet.ParseYaml(string?, out string?)"/> when the parsed
    /// document is accepted
    /// (<see cref="Data.RuleFile.AcceptParsedDocumentAsync"/>).
    /// </para>
    /// </summary>
    public class RuleFileParser : DocumentParser
    {
        [SetsRequiredMembers]
        public RuleFileParser(Data.RuleFile file, DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token) : base(file, parseMode, token)
        {
            this.Document = new CodeEditor2.CodeEditor.CodeDocument(file);
            CodeDocument? document = file.CodeDocument;
            if (document != null) this.Document.CopyTextOnlyFrom(document);
            this.ParseMode = parseMode;
            this.TextFile = file;

            ParsedDocument = new CodeEditor2.CodeEditor.ParsedDocument(file, file.RelativePath, file.CodeDocument?.Version ?? 0, parseMode);
        }

        public static class Style
        {
            public enum Color : byte
            {
                Normal = 0,
                Header = 5,
                Register = 3,
                Net = 9,
                Paramater = 7,
                Keyword = 4,
                Identifier = 6,
                Number = 8
            }
        }

        public override async Task ParseAsync()
        {
            await Task.CompletedTask;

            string text = Document.CreateString();
            RuleSet? ruleSet = RuleSet.ParseYaml(text, out string? errorMessage);
            if (ruleSet == null)
            {
                // the YAML itself is broken: mark the whole file
                if (Document.Length > 0) Document.Marks.SetMarkAt(0, Document.Length, 0);
                return;
            }

            ColorLines(text);

            if (errorMessage != null && ParsedDocument != null)
            {
                ParsedDocument.Messages.Add(new CodeEditor2.CodeEditor.ParsedDocument.Message()
                {
                    Index = 0,
                    Length = System.Math.Max(1, Document.Length),
                    Text = errorMessage,
                    Project = ParsedDocument.Project
                });
            }
        }

        /// <summary>
        /// Highlights "key:" headers and the severity values.
        /// </summary>
        private void ColorLines(string text)
        {
            int index = 0;
            while (index < text.Length)
            {
                int lineEnd = text.IndexOf('\n', index);
                if (lineEnd < 0) lineEnd = text.Length;

                string line = text.Substring(index, lineEnd - index).TrimEnd('\r');
                ColorLine(index, line);

                index = lineEnd + 1;
            }
        }

        private void ColorLine(int lineStart, string line)
        {
            string trimmed = line.TrimStart();
            int indent = line.Length - trimmed.Length;
            if (trimmed.Length == 0) return;
            if (trimmed.StartsWith("#")) return; // comment

            int colon = trimmed.IndexOf(':');
            if (colon <= 0) return;

            // "key:" header
            for (int i = lineStart + indent; i < lineStart + indent + colon; i++)
            {
                Document.TextColors.SetColorAt(i, (byte)Style.Color.Header);
            }

            string value = trimmed.Substring(colon + 1).Trim();
            if (value.Length == 0) return;

            // value starts after the colon (and any whitespace)
            int valueStartInLine = indent + colon + 1;
            while (valueStartInLine < line.Length && char.IsWhiteSpace(line[valueStartInLine])) valueStartInLine++;
            if (valueStartInLine >= line.Length) return;

            // a severity value is highlighted differently from a rule name
            bool isSeverity = RuleSet.TryParseSeverity(value, out _);
            Style.Color color = isSeverity ? Style.Color.Keyword : Style.Color.Identifier;
            for (int i = lineStart + valueStartInLine; i < lineStart + line.Length; i++)
            {
                Document.TextColors.SetColorAt(i, (byte)color);
            }
        }
    }
}
