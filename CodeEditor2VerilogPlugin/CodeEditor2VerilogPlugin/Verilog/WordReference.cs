using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class WordReference : IDisposable
    {
        public WordReference(int index, int length, CodeEditor2.CodeEditor.ParsedDocument parsedDocument, pluginVerilog.CodeEditor.CodeDocument document)
        {
            Index = index;
            Length = length;
            ParsedDocument = parsedDocument;
            Document = document;
        }
        public void Dispose()
        {
            ParsedDocument = null;
        }

        public WordReference CreateReferenceFrom(WordReference fromReference)
        {
            if (fromReference == null) return this;
            if (fromReference.ParsedDocument != ParsedDocument) return this;
            if (Document != fromReference.Document) return this;
            if (ParsedDocument == null || Document == null) return this;
            return new WordReference(fromReference.Index, Index + Length - fromReference.Index, ParsedDocument,Document);
        }
        public int Index { get; protected set; }
        public int Length { get; protected set; }

        private System.WeakReference<CodeEditor2.CodeEditor.ParsedDocument>? parsedDocumentRef;
        public CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument {
            get
            {
                if (parsedDocumentRef == null) return null;
                CodeEditor2.CodeEditor.ParsedDocument? ret;
                if (!parsedDocumentRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                if(value == null)
                {
                    parsedDocumentRef = null;
                    return;
                }
                parsedDocumentRef = new WeakReference<CodeEditor2.CodeEditor.ParsedDocument>(value);
            }
        }
        private System.WeakReference<pluginVerilog.CodeEditor.CodeDocument>? documentRef;
        public pluginVerilog.CodeEditor.CodeDocument? Document
        {
            get
            {
                if (documentRef == null) return null;
                pluginVerilog.CodeEditor.CodeDocument? ret;
                if (!documentRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                if (value == null)
                {
                    documentRef = null;
                    return;
                }
                documentRef = new WeakReference<pluginVerilog.CodeEditor.CodeDocument>(value);
            }
        }
        public void AddError(string message)
        {
            // null check
            if (ParsedDocument == null || Document == null) return;

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument == null) return;

            Data.IVerilogRelatedFile? vFile = Document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.ErrorCount < 100)
            {
                int lineNo = Document.GetLineAt(Index);
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Error, Index, lineNo, Length, ParsedDocument.Project));
            }
            else if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.ErrorCount == 100)
            {
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 errors", Verilog.ParsedDocument.Message.MessageType.Error, 0, 0, 0, ParsedDocument.Project)); ;
            }

            // increment message count
            if (ParsedDocument is Verilog.ParsedDocument) vParsedDocument.ErrorCount++;

            // add mark
            Document.Marks.SetMarkAt(Index,Length, 0);
        }
        public void AddWarning(string message)
        {
            // null check
            if (ParsedDocument == null || Document == null) return;

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument == null) return;

            Data.IVerilogRelatedFile? vFile = Document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (vParsedDocument.WarningCount < 100)
            {
                int lineNo = Document.GetLineAt(Index);
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Warning, Index, lineNo, Length, ParsedDocument.Project));
            }
            else if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.WarningCount == 100)
            {
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 warnings", Verilog.ParsedDocument.Message.MessageType.Warning, 0, 0, 0, ParsedDocument.Project));
            }

            // increment message count
            if (ParsedDocument is Verilog.ParsedDocument) vParsedDocument.WarningCount++;

            // add mark
            Document.Marks.SetMarkAt(Index, Length,1);
        }
        public void AddNotice(string message)
        {
            // null check
            if (ParsedDocument == null || Document == null) return;

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument == null) return;

            Data.IVerilogRelatedFile? vFile = Document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.NoticeCount < 100)
            {
                int lineNo = Document.GetLineAt(Index);
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Notice, Index, lineNo, Length, ParsedDocument.Project));
            }
            else if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.NoticeCount == 100)
            {
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Notice, 0, 0, 0, ParsedDocument.Project));
            }

            // increment message count
            if (ParsedDocument is Verilog.ParsedDocument) vParsedDocument.NoticeCount++;

            // add mark
            Document.Marks.SetMarkAt(Index, Length, 2);
        }
        public void AddHint(string message)
        {
            // null check
            if (ParsedDocument == null || Document == null) return;

            Verilog.ParsedDocument? vParsedDocument = ParsedDocument as Verilog.ParsedDocument;
            if (vParsedDocument == null) return;

            Data.IVerilogRelatedFile? vFile = Document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.HintCount < 100)
            {
                int lineNo = Document.GetLineAt(Index);
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Hint , Index, lineNo, Length, ParsedDocument.Project));
            }
            else if (ParsedDocument is Verilog.ParsedDocument && vParsedDocument.HintCount == 100)
            {
                ParsedDocument.Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Hint, 0, 0, 0, ParsedDocument.Project));
            }

            // increment message count
            if (ParsedDocument is Verilog.ParsedDocument) vParsedDocument.HintCount++;

            // add mark
            Document.Marks.SetMarkAt(Index, Length, 3);
        }

    }
}
