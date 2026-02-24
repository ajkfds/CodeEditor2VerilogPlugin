using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Parser
{
    public class TestResultParser : DocumentParser
    {
        [SetsRequiredMembers]
        public TestResultParser(Data.TestResultFile file, DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token) : base(file, parseMode, token)
        {
            this.Document = new CodeEditor2.CodeEditor.CodeDocument(file); // use verilog codeDocument
            CodeEditor2.CodeEditor.CodeDocument? document = file.CodeDocument;
            this.Document.CopyTextOnlyFrom(document);
            this.ParseMode = parseMode;
            this.TextFile = file as CodeEditor2.Data.TextFile;

            ParsedDocument = new CodeEditor2.Tests.TestResultParsedDocument(file, file.RelativePath, file.CodeDocument.Version, parseMode);
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

        public TestResultParsedDocument? TestResultParsedDocument
        {
            get
            {
                if (ParsedDocument == null) return null;
                return (TestResultParsedDocument)ParsedDocument;
            }
        }
        public override async Task ParseAsync()
        {
            if(TestResultParsedDocument == null) return;

            for (int line = 1; line < Document.Lines; line++)
            {
                if (tryParseLine(line, "result", out string result))
                {
                    if(result =="passed") TestResultParsedDocument.Passed = true;
                    if(result=="failed") TestResultParsedDocument.Failed = true;
                }
                if (tryParseLine(line, "hash", out string hash))
                {
                    TestResultParsedDocument.Hash = hash;
                }
                if (tryParseLine(line, "testName", out string textName))
                {
                    TestResultParsedDocument.TestName= textName;
                }
            }
        }
        public CodeEditor2.Tests.TestResult TestResult;


        private bool tryParseLine(int line, string header, out string text)
        {
            text = "";
            string lineText = Document.CreateString(Document.GetLineStartIndex(line), Document.GetLineLength(line));
            if (!lineText.StartsWith(header + ":")) return false;

            int start = Document.GetLineStartIndex(line);
            int end = start + header.Length;
            for (int i = start; i < end; i++)
            {
                Document.TextColors.SetColorAt(i, (byte)Style.Color.Header);
            }

            if (text.Length > end + 1)
            {
                text = lineText.Substring(end + 1).Trim();
            }
            return true;
        }
    }
}
