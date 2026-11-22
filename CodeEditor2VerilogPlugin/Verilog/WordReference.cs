using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog
{
    public class WordReference : IDisposable
    {
        protected WordReference() { }

        public static WordReference Create(WordPointer wordPointer, List<WordPointer> stocks)
        {
            WordReference wRef = new WordReference();
            wRef.indexReference = IndexReference.Create(wordPointer, stocks);
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

        [JsonIgnore]
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
            if (fromReference.IsPintSameHierarchy(toReference))
            {
                ret.indexReference = fromReference.indexReference;
                ret.length = toReference.Index - fromReference.Index + toReference.length;
            }
            return ret;
        }

        [JsonIgnore]
        public CodeEditor2.CodeEditor.IParsedDocument? ParsedDocument
        {
            get
            {
                return indexReference.ParsedDocument;
            }
        }
        [JsonIgnore]
        public pluginVerilog.CodeEditor.CodeDocument? Document
        {
            get
            {
                return indexReference.Document;
            }
        }

        /// <summary>
        /// The document this reference points into, as the UI-free contract, so
        /// colouring a reference does not need the editor's concrete document.
        /// </summary>
        [JsonIgnore]
        public SystemVerilogCore.Documents.IVerilogTextDocument? ContractDocument
        {
            get
            {
                return indexReference.Document;
            }
        }
        /// <summary>
        /// The rule set that applies to the file this reference belongs to
        /// (<c>.rule.yaml</c> file of the containing directory, falling back to the
        /// project wide default). Returns the built-in default rule set when
        /// the project property cannot be resolved.
        /// </summary>
        [JsonIgnore]
        public RuleSet RuleSet
        {
            get
            {
                // The parser's own document type carries the cached project
                // property and the source file, so no editor project is needed.
                if (indexReference?.ParsedDocument is not ParsedDocument parsedDocument)
                {
                    return new pluginVerilog.RuleSet();
                }
                ProjectProperty? projectProperty = parsedDocument.ProjectProperty;
                if (projectProperty == null) return new pluginVerilog.RuleSet();

                return projectProperty.GetRuleSet(parsedDocument.TextFile?.RelativePath);
            }
        }

        public void ApplyRule(Rule rule)
        {
            ApplyRule(rule, "");
        }
        public void ApplyRule(Rule rule, string message)
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
            SystemVerilogCore.Documents.IVerilogTextDocument? document = ContractDocument;
            if (document == null) return;
            document.SetColorAt(Index, CodeDrawStyle.ColorIndex(colorType), length);
        }
    }
}
