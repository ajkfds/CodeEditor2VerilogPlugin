using ExCSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class WordReference : IDisposable
    {
        protected WordReference() { }

        public static WordReference Create(WordPointer wordPointer, List<WordPointer> stocks)
        {
            WordReference wRef = new WordReference();
            wRef.indexReference = IndexReference.Create(wordPointer,stocks);
            wRef.length = wordPointer.Length;
            return wRef;
        }

        public WordReference Clone()
        {
            WordReference wRef = new WordReference();
            wRef.indexReference = indexReference.Clone();
            wRef.length = length;
            return wRef;
        }

        public int Index
        {
            get
            {
                return indexReference.Index;
            }
        }

        public int Length
        {
            get
            {
                return length;
            }
        }


        public IndexReference IndexReference
        {
            get
            {
                return indexReference;
            }
        }
        private IndexReference indexReference;
        private int length;

        public void Dispose()
        {
            indexReference = null;
        }

        public bool IsPintSameHierarchy(WordReference wordReference)
        {
            return IndexReference.IsPointSameHier(wordReference.indexReference);
        }

        public static WordReference CreateReferenceRange(WordReference fromReference, WordReference toReference)
        {
            WordReference ret = toReference.Clone();
            if (fromReference.IsPintSameHierarchy(toReference)){
                ret.indexReference = fromReference.indexReference;
                ret.length = toReference.Index - fromReference.Index + toReference.length;
            }
            return ret;
        }

        public CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument {
            get
            {
                return indexReference.ParsedDocument;
            }
        }
        public pluginVerilog.CodeEditor.CodeDocument? Document
        {
            get
            {
                return indexReference.Document;
            }
        }
        public void ApplyRule(Rule rule)
        {
            ApplyRule(rule, "");
        }
        public void ApplyRule(Rule rule,string message)
        {
            switch (rule.Severity)
            {
                case Rule.SeverityEnum.Error:
                    AddError(rule.Message + message);
                    break;
                case Rule.SeverityEnum.Warning:
                    AddWarning(rule.Message + message);
                    break;
                case Rule.SeverityEnum.Notice:
                    AddNotice(rule.Message + message);
                    break;
            }
        }

        public void AddError(string message)
        {
            ParsedDocument? parsedDocument = indexReference.ParsedDocument;
            if (parsedDocument == null) return;
            parsedDocument.AddError(Index, Length, message);
        }

        public void AddWarning(string message)
        {
            ParsedDocument? parsedDocument = indexReference.ParsedDocument;
            if (parsedDocument == null) return;
            parsedDocument.AddWarning(Index, Length, message);
        }
        public void AddNotice(string message)
        {
            ParsedDocument? parsedDocument = indexReference.ParsedDocument;
            if (parsedDocument == null) return;
            parsedDocument.AddNotice(Index, Length, message);
        }
        public void AddHint(string message)
        {
            ParsedDocument? parsedDocument = indexReference.ParsedDocument;
            if (parsedDocument == null) return;
            parsedDocument.AddHint(Index, Length, message);
        }

        public void Color(CodeDrawStyle.ColorType colorType)
        {
            if(Document == null) return;
            Document.TextColors.SetColorAt(Index, CodeDrawStyle.ColorIndex(colorType), length);
        }
    }
}
