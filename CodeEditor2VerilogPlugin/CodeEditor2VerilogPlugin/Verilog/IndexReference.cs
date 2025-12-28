using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class IndexReference
    {
        protected IndexReference() { }

        public static IndexReference Create(WordPointer wordPointer,List<WordPointer> stocks)
        {
            IndexReference ret = new IndexReference();

            if (stocks.Count >= 1)
            {
                ret.rootParsedDocumentRef = new WeakReference<ParsedDocument>(stocks[0].ParsedDocument);
            }
            else
            {
                ret.rootParsedDocumentRef = new WeakReference<ParsedDocument>(wordPointer.ParsedDocument);
            }

            ret.parsedDocumentRef = new WeakReference<ParsedDocument>(wordPointer.ParsedDocument);
            if (stocks.Count != 0)
            {
                foreach (WordPointer wp in stocks)
                {
                    ret.indexes.Add(wp.Index);
                }
            }
            ret.indexes.Add(wordPointer.Index);

            return ret;
        }
        public static IndexReference Create(ParsedDocument parsedDocument, CodeEditor.CodeDocument document,int index)
        {
            IndexReference ret = new IndexReference();

            ret.rootParsedDocumentRef = new WeakReference<ParsedDocument>(parsedDocument);
            ret.parsedDocumentRef = new WeakReference<ParsedDocument>(parsedDocument);
            ret.indexes.Add(index);

            return ret;
        }

        public static IndexReference Create(IndexReference indexReference, int index)
        {
            IndexReference ret = indexReference.Clone();
            ret.rootParsedDocumentRef = indexReference.rootParsedDocumentRef;
            ret.parsedDocumentRef = indexReference.parsedDocumentRef;
            if (ret.indexes.Count > 0) ret.indexes.RemoveAt(ret.indexes.Count - 1);
            ret.indexes.Add(index);
            return ret;
        }

        public IndexReference Clone()
        {
            IndexReference ret = new IndexReference();
            ret.rootParsedDocumentRef = rootParsedDocumentRef;
            ret.parsedDocumentRef = parsedDocumentRef;
            foreach (int index in indexes)
            {
                ret.indexes.Add(index);
            }
            return ret;
        }

        public string GetIndexID()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (int index in indexes)
            {
                stringBuilder.Append(index.ToString());
                stringBuilder.Append('_');
            }
            return stringBuilder.ToString();
        }

        protected WeakReference<ParsedDocument> parsedDocumentRef;
        [JsonIgnore]
        public ParsedDocument? ParsedDocument
        {
            get
            {
                ParsedDocument? ret;
                if (!parsedDocumentRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        [JsonIgnore]
        public pluginVerilog.CodeEditor.CodeDocument? Document
        {
            get
            {
                ParsedDocument? parsedDocument = ParsedDocument;
                if (parsedDocument == null) return null;
                return parsedDocument.CodeDocument;
            }
        }


        private System.WeakReference<ParsedDocument> rootParsedDocumentRef;
        [JsonIgnore]
        public ParsedDocument RootParsedDocument
        {
            get
            {
                ParsedDocument ret;
                if (!rootParsedDocumentRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public int Index
        {
            get
            {
                return indexes.Last();
            }
        }

        public IReadOnlyList<int> Indexes
        {
            get
            {
                return indexes;   
            }
        }

        private List<int> indexes = new List<int>();

        public bool IsGreaterThan(IndexReference indexReference)
        {
            if (indexReference == null) return false;

            int i = indexes.Count;
            if (i > indexReference.indexes.Count)
            {
                i = indexReference.indexes.Count;
            }

            for (int j = 0; j < i; j++)
            {
                if (indexes[j] > indexReference.indexes[j]) return true;
            }
            return false;
        }
        public bool IsSameAs(IndexReference indexReference)
        {
            if (indexReference == null) return false;

            int i = indexes.Count;
            if (i > indexReference.indexes.Count)
            {
                i = indexReference.indexes.Count;
            }

            for (int j = 0; j < i; j++)
            {
                if (indexes[j] != indexReference.indexes[j]) return false;
            }
            return true;
        }

        public bool IsSmallerThan(IndexReference indexReference)
        {
            if (indexReference == null) return false;

            int i = indexes.Count;
            if (i > indexReference.indexes.Count)
            {
                i = indexReference.indexes.Count;
            }

            for (int j = 0; j < i; j++)
            {
                if (indexes[j] < indexReference.indexes[j]) return true;
            }
            return false;
        }

        public bool IsPointSameHier(IndexReference indexReference)
        {
            if (indexReference == null) return false;

            int i = indexes.Count;
            if (i > indexReference.indexes.Count)
            {
                i = indexReference.indexes.Count;
            }

            for (int j = 0; j < i-1; j++)
            {
                if (indexes[j] != indexReference.indexes[j]) return false;
            }
            return true;

        }
    }
}
