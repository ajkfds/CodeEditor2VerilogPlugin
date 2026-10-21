using pluginVerilog.CodeEditor;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class WordScanner : IDisposable
    {
        public WordScanner(
            CodeEditor.CodeDocument document,
            Verilog.ParsedDocument parsedDocument,
            bool systemVerilog,
            int indexReferenceOffset = 0,
            CompletionContext completionContext = null
            ) : this(document, parsedDocument, systemVerilog, false,false,indexReferenceOffset)
        {
            CompletionContext = completionContext;
        }

        public WordScanner(
            CodeEditor.CodeDocument document, Verilog.ParsedDocument parsedDocument, 
            bool systemVerilog, bool supressCompilerDirectiveError,
            bool prototype, int indexReferenceOffset = 0
            )
            : this(document, parsedDocument, systemVerilog, supressCompilerDirectiveError,
                   prototype, indexReferenceOffset, false)
        {
        }

        /// <summary>
        /// Constructor used by <see cref="Clone"/>: <paramref name="suppressInitialDirective"/>
        /// skips the initial recheckWord() so that compiler directives at index 0
        /// are not processed a second time. The clone replaces wordPointer right
        /// after construction; processing the leading `define / `undef here would
        /// run them on the shared ParsedDocument.Macros table again and produce
        /// spurious "duplicate macro name" errors.
        /// </summary>
        internal WordScanner(
            CodeEditor.CodeDocument document, Verilog.ParsedDocument parsedDocument,
            bool systemVerilog, bool supressCompilerDirectiveError,
            bool prototype, int indexReferenceOffset,
            bool suppressInitialDirective
            )
        {
            RootParsedDocument = parsedDocument;
            wordPointer = new WordPointer(document, parsedDocument,indexReferenceOffset);
            SupressCompilerDerectiveError = supressCompilerDirectiveError;
            this.prototype = prototype;
            if (!suppressInitialDirective) recheckWord();
            this.systemVerilog = systemVerilog;
        }
        public bool SupressCompilerDerectiveError { get; set; } = false;

        public DefaultNetTypeEnum DefaultNetType = WordScanner.DefaultNetTypeEnum.none;
        public CancellationToken? CancellationToken { get; set; }

        public CompletionContext? CompletionContext { get; set; } = null;

        /// <summary>
        /// True while parsing a partial (autocomplete) document region: the
        /// partial parse runs with a CompletionContext attached (see
        /// Verilog.CompletionContext). Errors / warnings must not be reported
        /// during that parse, they belong to the full parse only.
        /// </summary>
        public bool IsPartialParse
        {
            get
            {
                return CompletionContext != null;
            }
        }
        public int IndexReferenceOffset
        {
            get { return wordPointer.IndexReferenceOffset; }
            set { wordPointer.IndexReferenceOffset = value; }
        }
        public void CheckCancelToken()
        {
            CancellationToken?.ThrowIfCancellationRequested();
        }
        public enum DefaultNetTypeEnum
        {
            wire,
            tri,
            tri0,
            wand,
            triand,
            wor,
            trior,
            trireg,
            none
        }


        public void GetFirst()
        {
            recheckWord();
        }

        public void Dispose()
        {
            wordPointer.Dispose();
        }

        public Verilog.ParsedDocument RootParsedDocument { get; protected set; }
        public CodeEditor.CodeDocument Document
        {
            get
            {
                return wordPointer.Document;
            }
        }

        public CodeEditor2.Data.Project Project
        {
            get
            {
                return RootParsedDocument.Project;
            }
        }

        public bool ReadOnly
        {
            get;set;
        }

        public ProjectProperty ProjectProperty
        {
            get
            {
                if (Project == null) throw new Exception();
                ProjectProperty? projectProperty = Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();
                return projectProperty;
            }
        }
        public WordPointer RootPointer
        {
            get
            {
                if (stock.Count == 0)
                {
                    return wordPointer;
                }
                else
                {
                    return stock[0];
                }
            }
        }

        private int nonGeneratedCount = 0;
        private bool prototype = false;

        public bool Prototype
        {
            set
            {
                prototype = value;
            }
            get
            {
                return prototype;
            }
        }


        public bool Moved { get; set; } = false;

        private bool cellDefine;
        public bool CellDefine
        {
            get
            {
                return cellDefine;
            }
        }

        private bool systemVerilog;
        public bool SystemVerilog
        {
            get { return systemVerilog; }
        }

        /// <summary>
        /// Nesting depth of design elements (module / interface / program /
        /// package / checker / primitive / class) currently being parsed.
        /// Some compiler directives are only legal outside of a design element,
        /// e.g. `resetall (IEEE 1800-2017 22.3).
        /// </summary>
        private int designElementDepth = 0;

        public bool InDesignElement
        {
            get { return designElementDepth > 0; }
        }

        public void EnterDesignElement()
        {
            designElementDepth++;
        }

        public void ExitDesignElement()
        {
            if (designElementDepth > 0) designElementDepth--;
        }

        public void StartNonGenerated()
        {
            nonGeneratedCount++;
        }

        public void EndNonGenerated()
        {
            if (nonGeneratedCount == 0) return;
            nonGeneratedCount--;
        }

        public string Section { get; protected set; }

        protected WordPointer wordPointer = null;
        protected List<WordPointer> stock = new List<WordPointer>();

        static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        public WordScanner Clone(bool supressError)
        {
            WordScanner ret = new WordScanner(wordPointer.Document, RootParsedDocument, systemVerilog, supressError, prototype, 0, true);
            ret.wordPointer = wordPointer.Clone();
            ret.nonGeneratedCount = nonGeneratedCount;
            ret.prototype = prototype;
            ret.designElementDepth = designElementDepth;
            foreach (var wp in stock)
            {
                ret.stock.Add(wp.Clone());
            }
            return ret;
        }

        public CommentScanner GetPreviousCommentScanner()
        {
            return wordPointer.GetPreviousCommentScanner();
        }
        public CommentScanner GetNextCommentScanner()
        {
            return wordPointer.GetNextCommentScanner();
        }

        public string GetNextComment()
        {
            return wordPointer.GetNextComment();
        }
        public string GetPreviousComment()
        {
            return wordPointer.GetPreviousComment();
        }

        public WordReference GetReference()
        {
            return CrateWordReference();
        }


        public void Color(CodeDrawStyle.ColorType colorType)
        {
            if (prototype) return;
            if (ReadOnly) return;
            wordPointer.Color(colorType);
        }

        public void AppendBlock(IndexReference startIndexReference, IndexReference lastIndexReference)
        {
            appendBlock(startIndexReference, lastIndexReference, null, null);
        }

        public void AppendBlock(IndexReference startIndexReference, IndexReference lastIndexReference, string name, bool defaultClose)
        {
            appendBlock(startIndexReference, lastIndexReference, name, defaultClose);
        }
        public void appendBlock(IndexReference startIndexReference, IndexReference lastIndexReference, string? name, bool? defaultClose)
        {
            if (ReadOnly) return;
            if (startIndexReference.Indexes.Count != lastIndexReference.Indexes.Count) return;
            for (int i = 0; i < startIndexReference.Indexes.Count - 1; i++)
            {
                if (startIndexReference.Indexes[i] != lastIndexReference.Indexes[i]) return;
            }

            if (wordPointer.Document.GetLineAt(startIndexReference.Indexes.Last()) == wordPointer.Document.GetLineAt(lastIndexReference.Indexes.Last())) return;

            if (name == null)
            {
                wordPointer.AppendBlock(startIndexReference.Indexes.Last(), lastIndexReference.Indexes.Last());
            }
            else if (defaultClose != null)
            {
                wordPointer.AppendBlock(startIndexReference.Indexes.Last(), lastIndexReference.Indexes.Last(), name, (bool)defaultClose);
            }
        }

        private bool systemVerilogError = false;
        public void AddSystemVerilogError()
        {
            if (ReadOnly) return;
            if (RootParsedDocument.SystemVerilog) return;
            if (systemVerilogError) return;
            if (IsPartialParse) return;
            AddError("SystemVerilog Description");
        }

        public void AddError(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (prototype) return;
            if (IsPartialParse) return;
            wordPointer.AddError(message);
        }

        public void AddWarning(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (prototype) return;
            if (IsPartialParse) return;
            wordPointer.AddWarning(message);
        }

        public void AddPrototypeError(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (IsPartialParse) return;
            //            if (prototype) return;
            wordPointer.AddError(message);
        }

        public void AddPrototypeWarning(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (IsPartialParse) return;
            //            if (prototype) return;
            wordPointer.AddWarning(message);
        }

        public void AddNotice(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (prototype) return;
            if (IsPartialParse) return;
            wordPointer.AddNotice(message);
        }
        public void AddHint(string message)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (prototype) return;
            if (IsPartialParse) return;
            wordPointer.AddHint(message);
        }

        public void ApplyPrototypeRule(Rule rule)
        {
            if (ReadOnly) return;
            if (SupressCompilerDerectiveError) return;
            if (IsPartialParse) return;
            ApplyPrototypeRule(rule, "");
        }
        public void ApplyPrototypeRule(Rule rule, string message)
        {
            if (SupressCompilerDerectiveError) return;
            //            if (!prototype) System.Diagnostics.Debugger.Break();
            if (IsPartialParse) return;
            applyRule(rule, message);
        }
        public void ApplyRule(Rule rule)
        {
            if (SupressCompilerDerectiveError) return;
            if (IsPartialParse) return;
            ApplyRule(rule, "");
        }
        public void ApplyRule(Rule rule, string message)
        {
            if (SupressCompilerDerectiveError) return;
            if (prototype) return;
            if (IsPartialParse) return;
            applyRule(rule, message);
        }
        private void applyRule(Rule rule, string message)
        {
            if (ReadOnly) return;
            switch (rule.Severity)
            {
                case Rule.SeverityEnum.Error:
                    wordPointer.AddError(rule.Message + message);
                    break;
                case Rule.SeverityEnum.Warning:
                    wordPointer.AddWarning(rule.Message + message);
                    break;
                case Rule.SeverityEnum.Notice:
                    wordPointer.AddNotice(rule.Message + message);
                    break;
            }
        }

        public int RootIndex
        {
            get
            {
                if (stock.Count == 0)
                {
                    return wordPointer.Index;
                }
                else
                {
                    return stock[0].Index;
                }
            }
        }

        public IndexReference CreateIndexReference()
        {
            return IndexReference.Create(wordPointer, stock);
        }

        public IndexReference CreateIndexReferenceBefore()
        {
            return IndexReference.CreateBefore(wordPointer, stock);
        }

        public WordReference CrateWordReference()
        {
            return WordReference.Create(wordPointer, stock);
        }

        public bool Active
        {
            get
            {
                if (nonGeneratedCount != 0) return false;
                return true;
            }
        }

        public bool SkipToKeyword(string stopWord)
        {
            bool skipped = false;
            while (!Eof)
            {
                if (Text == stopWord) return skipped;
                if (General.ListOfStatementStopKeywords.Contains(Text)) return skipped;
                MoveNext();
                skipped = true;
            }
            return skipped;
        }
        public bool SkipToKeywords(List<string> stopKeywords)
        {
            bool skipped = false;
            while (!Eof)
            {
                if (stopKeywords.Contains(Text)) return skipped;
                if (General.ListOfStatementStopKeywords.Contains(Text)) return skipped;
                MoveNext();
                skipped = true;
            }
            return skipped;
        }

        public string SectionName
        {
            get
            {
                return wordPointer.SectionName;
            }
        }
        public void MoveNext()
        {
            Moved = true;
            CheckCancelToken();
            moveNext();
        }

        private void moveNext()
        {
            CancellationToken?.ThrowIfCancellationRequested();

            if (wordPointer.Eof)
            {
                while (wordPointer.Eof && stock.Count != 0)
                {
                    returnHierarchy();
                }
                recheckWord();
            }
            else
            {
                wordPointer.MoveNext();
                recheckWord();
            }
        }


        private void recheckWord()
        {
            // Check cancellation at the start of recheckWord
            CancellationToken?.ThrowIfCancellationRequested();

            // skip comments on the end of file
            while (!wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.Comment)
                {
                    wordPointer.MoveNext();
                }
                else if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective)
                {
                    parseCompilerDirective();
                }
                else
                {
                    break;
                }
            }

            // return hierarchy at EOF
            if (wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.Comment || wordPointer.Text == "")
                {
                    if (nonGeneratedCount != 0)
                    {
                        Color(CodeDrawStyle.ColorType.Inactivated);
                    }
                    while (wordPointer.Eof && stock.Count != 0)
                    {
                        returnHierarchy();
                        recheckWord();
                    }
                }
            }

        }

        private void returnHierarchy()
        {
            bool error = false;
            bool warning = false;
            if (wordPointer.ParsedDocument.ErrorCount != 0)
            {
                error = true;
            }
            if (wordPointer.ParsedDocument.WarningCount != 0)
            {
                warning = true;
            }
            wordPointer.ParsedDocument.CodeDocument = wordPointer.Document;

            if (wordPointer.ParsedDocument == stock.Last().ParsedDocument)
            {
                error = false;
                warning = false;
            }

            //if(wordPointer.ParsedDocument.Item != null) wordPointer.ParsedDocument.Item.Update();

            if (!RootParsedDocument.LockedDocument.Contains(wordPointer.Document))
            {
                RootParsedDocument.LockedDocument.Add(wordPointer.Document);
            }

            //wordPointer.Dispose(); keep document & parsedData
            wordPointer = stock.Last();
            stock.Remove(stock.Last());

            if (!prototype)
            {
                if (error)
                {
                    if (!SupressCompilerDerectiveError) wordPointer.AddError("include errors");
                }
                else if (warning)
                {
                    if (!SupressCompilerDerectiveError) wordPointer.AddWarning("include warnings");
                }
            }
            wordPointer.MoveNext();
        }

        public bool Eof
        {
            get
            {
                if (stock.Count == 0) return wordPointer.Eof;
                for (int i = stock.Count - 1; i >= 0; i--)
                {
                    if (!stock[i].Eof) return false;
                }
                return true;
            }
        }

        public string Text
        {
            get
            {
                string ret = wordPointer.Text;
                return ret;
            }
        }

        public WordPointer.WordTypeEnum WordType
        {
            get
            {
                return wordPointer.WordType;
            }
        }

        public string NextText
        {
            get
            {
                // keep current status
                WordPointer _wp = wordPointer.Clone();
                int _nonGeneratedCount = nonGeneratedCount;
                bool _prototype = prototype;
                List<WordPointer> _stock = new List<WordPointer>();

                foreach (var wp in stock)
                {
                    _stock.Add(wp.Clone());
                }


                if (wordPointer.Eof)
                {
                    while (wordPointer.Eof && stock.Count != 0)
                    {
                        returnHierarchy();
                    }
                }
                else
                {
                    wordPointer.MoveNext();
                }

                recheckWord();
                string text = wordPointer.Text;

                wordPointer = _wp;
                nonGeneratedCount = _nonGeneratedCount;
                prototype = _prototype;
                if (stock.Count != _stock.Count)
                {
                    stock.Clear();
                    foreach (var wp in _stock)
                    {
                        stock.Add(wp);
                    }
                }
                return text;
            }
        }

        public int Length
        {
            get
            {
                return wordPointer.Length;
            }
        }

        public char GetCharAt(int wordIndex)
        {
            return wordPointer.GetCharAt(wordIndex);
        }

        private enum ifdefEnum
        {
            ifdefActive,
            ifdefInActive,
            ElseActive,
            ElseInActive
        }
        private List<ifdefEnum> ifDefList = new List<ifdefEnum>();

        /// <summary>
        /// Compiler directive names. These are considered predefined macro names,
        /// so it is illegal to redefine one of them as a macro name with `define
        /// (IEEE 1800-2017 22.5.1).
        /// </summary>
        private static readonly HashSet<string> compilerDirectiveNames = new HashSet<string>
        {
            "define", "undef", "undefineall",
            "ifdef", "ifndef", "elsif", "else", "endif",
            "include", "resetall", "timescale", "default_nettype",
            "unconnected_drive", "nounconnected_drive",
            "celldefine", "endcelldefine", "line", "pragma",
            "begin_keywords", "end_keywords",
            "__FILE__", "__LINE__"
        };

        private void parseCompilerDirective()
        {
            switch (wordPointer.Text)
            {
                case "`pragma":
                    PragmaDirective.Parse(wordPointer);
                    break;
                case "`include":
                    parseInclude();
                    break;
                case "`define":
                    parseDefine();
                    break;
                case "`celldefine":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    cellDefine = true;
                    wordPointer.MoveNext();
                    break;
                case "`resetall":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    // `resetall shall not be specified within a design element
                    // (IEEE 1800-2017 22.3). AddError already suppresses the
                    // report on prototype / partial (autocomplete) parses.
                    if (InDesignElement)
                    {
                        AddError("`resetall shall not be specified within a design element");
                    }
                    wordPointer.MoveNext();
                    break;
                case "`undefineall":
                    parseUndefineAll();
                    break;
                case "`endcelldefine":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    cellDefine = false;
                    wordPointer.MoveNext();
                    break;
                case "`default_nettype":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();

                    switch (wordPointer.Text)
                    {
                        case "none":
                            DefaultNetType = DefaultNetTypeEnum.none;
                            wordPointer.MoveNext();
                            break;
                        case "wire":
                            DefaultNetType = DefaultNetTypeEnum.wire;
                            wordPointer.MoveNext();
                            break;
                        case "tri":
                            DefaultNetType = DefaultNetTypeEnum.tri;
                            wordPointer.MoveNext();
                            break;
                        case "tri0":
                            DefaultNetType = DefaultNetTypeEnum.tri0;
                            wordPointer.MoveNext();
                            break;
                        case "tri1":
                            // treated as tri0-family; no dedicated enum member
                            wordPointer.MoveNext();
                            break;
                        case "wand":
                            DefaultNetType = DefaultNetTypeEnum.wand;
                            wordPointer.MoveNext();
                            break;
                        case "triand":
                            DefaultNetType = DefaultNetTypeEnum.triand;
                            wordPointer.MoveNext();
                            break;
                        case "wor":
                            DefaultNetType = DefaultNetTypeEnum.wor;
                            wordPointer.MoveNext();
                            break;
                        case "trior":
                            DefaultNetType = DefaultNetTypeEnum.trior;
                            wordPointer.MoveNext();
                            break;
                        case "trireg":
                            DefaultNetType = DefaultNetTypeEnum.trireg;
                            wordPointer.MoveNext();
                            break;
                        case "uwire":
                            wordPointer.MoveNext();
                            break;
                        default:
                            if (!SupressCompilerDerectiveError) wordPointer.AddError("illegal netType");
                            wordPointer.MoveNext();
                            break;
                    }
                    break;
                case "`endif":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    if (ifDefList.Count != 0) ifDefList.Remove(ifDefList.Last());
                    break;
                case "`ifdef":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        RootParsedDocument.Macros.ContainsKey(wordPointer.Text) ||
                        RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            Color(CodeDrawStyle.ColorType.Keyword);
                            wordPointer.MoveNext();
                        }
                    }
                    else
                    {   // false
                        wordPointer.MoveNext();
                        skip();
                    }
                    break;
                case "`ifndef":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        !RootParsedDocument.Macros.ContainsKey(wordPointer.Text) &&
                        !RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            Color(CodeDrawStyle.ColorType.Keyword);
                            wordPointer.MoveNext();
                        }
                    }
                    else
                    {   // false
                        wordPointer.MoveNext();
                        skip();
                    }
                    break;
                case "`else":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    skip();
                    break;
                case "`elsif":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        RootParsedDocument.Macros.ContainsKey(wordPointer.Text) ||
                        RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            Color(CodeDrawStyle.ColorType.Keyword);
                            wordPointer.MoveNext();
                        }
                    }
                    else
                    {   // false
                        wordPointer.MoveNext();
                        skip();
                    }
                    break;
                case "`undef":
                    parseUndef();
                    break;
                case "`line":
                    // `line number "filename" level
                    //   number : positive integer (new line number)
                    //   filename : string literal
                    //   level : 0, 1 or 2
                    // (IEEE 1800-2017 22.12). Each argument is validated so an
                    // illegal form is reported instead of silently consumed.
                    Color(CodeDrawStyle.ColorType.Keyword);
                    // MoveNextUntilEol stops on the directive's argument text
                    // (the whole rest of the line), which parseLineDirective
                    // then tokenizes and validates.
                    wordPointer.MoveNextUntilEol();
                    parseLineDirective();
                    // advance to the next token so the caller does not treat the
                    // argument line as a module item (same pattern as
                    // parseUndef / parseDefine).
                    wordPointer.MoveNext();
                    break;
                case "`nounconnected_drive":
                    // `nounconnected_drive takes no argument (IEEE 1800-2017 22.9);
                    // "`nounconnected_drive pull0" is illegal. The argument text
                    // (if any) is the remainder of the same line.
                    // `nounconnected_drive takes no argument (IEEE 1800-2017 22.9).
                    // The argument, if any, must be on the same line; checking the
                    // next token's line avoids misreading a following token.
                    // `nounconnected_drive takes no argument (IEEE 1800-2017 22.9).
                    // The argument, if any, is on the same line; the following
                    // token on a new line must not be treated as one.
                    Color(CodeDrawStyle.ColorType.Keyword);
                    {
                        int newlineIndex = getLineEndIndex(wordPointer.Index + wordPointer.Length);
                        wordPointer.MoveNext();
                        if (!wordPointer.Eof && wordPointer.Index < newlineIndex)
                        {
                            wordPointer.AddError("illegal nounconnected_drive argument");
                        }
                    }
                    break;
                case "`begin_keywords":
                    // `begin_keywords "version"  (directive argument on the same line)
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNextUntilEol();
                    break;
                case "`end_keywords":
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    break;
                case "`unconnected_drive":
                    // `unconnected_drive pull0 | pull1 (IEEE 1800-2017 22.9).
                    // Only pull0 / pull1 are legal arguments.
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNextUntilEol();
                    validateUnconnectedDriveArgument();
                    break;
                case "`timescale":
                    // `timescale <time_unit> / <time_precision> (IEEE 1800-2017 22.7)
                    Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNextUntilEol();
                    validateTimescaleDirective();
                    break;
                case "`__FILE__":
                    // predefined macro `__FILE__ expands to the file name as a string literal
                    Color(CodeDrawStyle.ColorType.Identifier);
                    parsePredefinedFileMacro();
                    break;
                case "`__LINE__":
                    // predefined macro `__LINE__ expands to the current line number
                    Color(CodeDrawStyle.ColorType.Identifier);
                    parsePredefinedLineMacro();
                    break;
                default: // macro call
                    parseMacro();
                    break;
            }
        }

        private void skip()
        {
            int depth = 0;
            while (!wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective)
                {
                    switch (wordPointer.Text)
                    {
                        case "`ifdef":
                            depth++;
                            Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                        case "`ifndef":
                            depth++;
                            Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                        case "`else":
                            if (depth == 0)
                            {
                                Color(CodeDrawStyle.ColorType.Keyword);
                                wordPointer.MoveNext();
                                return;
                            }
                            else
                            {
                                Color(CodeDrawStyle.ColorType.Inactivated);
                                wordPointer.MoveNext();
                            }
                            break;
                        case "`endif":
                            if (depth == 0)
                            {
                                Color(CodeDrawStyle.ColorType.Keyword);
                                wordPointer.MoveNext();
                                return;
                            }
                            else
                            {
                                depth--;
                                Color(CodeDrawStyle.ColorType.Inactivated);
                                wordPointer.MoveNext();
                            }
                            break;
                        default:
                            Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                    }
                }
                else
                {
                    Color(CodeDrawStyle.ColorType.Inactivated);
                    wordPointer.MoveNext();
                }
            }
        }

        private void parseUndef()
        {
            Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNextUntilEol();

            string text = wordPointer.Text;
            text = text.TrimStart(new char[] { ' ', '\t' });
            text = text.TrimEnd(new char[] { ' ', '\t' });
            if (RootParsedDocument.Macros.ContainsKey(text))
            {
                RootParsedDocument.Macros.Remove(text);
            }
            wordPointer.MoveNext();
        }

        private void parseDefine()
        {
            Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNextUntilEol();

            WordReference wordRef = GetReference();
            string macroText = wordPointer.Text;
            int index = wordPointer.Index;

            while (macroText.EndsWith("\\") && !wordPointer.Eof)
            {
                macroText = macroText.Substring(0, macroText.Length - 1);
                wordPointer.MoveNextUntilEol();
                macroText = macroText + wordPointer.Text;
            }

            string identifier = "";

            // get identifier separator
            int separatorIndex;
            {
                int spaceIndex = int.MaxValue;
                if (macroText.Contains(" ")) spaceIndex = macroText.IndexOf(" ");

                int tabIndex = int.MaxValue;
                if (macroText.Contains("\t")) tabIndex = macroText.IndexOf("\t");

                int bracketIndex = int.MaxValue;
                if (macroText.Contains("(")) bracketIndex = macroText.IndexOf("(");

                separatorIndex = spaceIndex;
                if (tabIndex < separatorIndex) separatorIndex = tabIndex;
                if (bracketIndex < separatorIndex) separatorIndex = bracketIndex;
            }

            if (separatorIndex == int.MaxValue)
            { // identifier only
                identifier = macroText;
                Color(CodeDrawStyle.ColorType.Identifier);
                macroText = "";
            }
            else
            {
                identifier = macroText.Substring(0, separatorIndex);
                if(!prototype) wordPointer.Color(CodeDrawStyle.ColorType.Identifier, index, index + separatorIndex);
                macroText = macroText.Substring(separatorIndex);
            }

            Macro macro = Macro.Create(identifier, macroText);
            macro.DefineIndex = index;
            if (!General.IsIdentifier(macro.Name))
            {
                if (!SupressCompilerDerectiveError) wordRef.AddError("illegal macro identifier");
            }
            else if (compilerDirectiveNames.Contains(macro.Name))
            {
                // All compiler directives are considered predefined macro names;
                // it shall be illegal to redefine a compiler directive as a macro
                // name (IEEE 1800-2017 22.5.1).
                if (!SupressCompilerDerectiveError && !prototype)
                {
                    wordRef.AddError("a compiler directive shall not be redefined as a macro name");
                }
            }
            else if (RootParsedDocument.Macros.ContainsKey(macro.Name))
            {
                // recheckWord() processes compiler directives as a side effect
                // (NextText probe, MoveNext, the WordScanner.Clone constructor),
                // so the same `define directive can be processed more than once.
                // Only report a genuine redefinition, i.e. when the directive
                // being processed is located strictly after the definition that
                // is already registered. Re-processing an earlier (or the same)
                // directive must not report a duplicate. On a genuine
                // redefinition the entry is replaced so that re-processing the
                // newer directive does not report a second error.
                Macro existing = RootParsedDocument.Macros[macro.Name];
                if (index > existing.DefineIndex)
                {
                    if (!SupressCompilerDerectiveError & !prototype)
                    {
                        wordRef.AddError("duplicate macro name");
                    }
                    RootParsedDocument.Macros[macro.Name] = macro;
                }
            }
            else
            {
                RootParsedDocument.Macros.Add(macro.Name, macro);
            }


            wordPointer.MoveNext();
            recheckWord();
        }

        private void parseUndefineAll()
        {
            Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNext();
            RootParsedDocument.Macros.Clear();
        }

        private void parseInclude()
        {
            Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNext();

            /*
            # double quotes     ("filename")
              a relative path the compiler's current working directory, 
              and optionally user-specified locations are searched. 

            # angle brackets    (<filename>)
              then only an implementation dependent location containing files defined by the language standard is searched.
              Relative path names are interpreted relative to that location. 

              When the filename is an absolute path, only that filename is included and only the double quote form of the `include can be used.

            `include nesting levels must accept > 15
             */

            string filePath = null;
            if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective
                && wordPointer.Text.Length > 1)
            {
                // `include `macro(args) : expand the macro in place and use its text
                // as the file name (IEEE 1800-2017 22.5.1)
                string macroIdentifier = wordPointer.Text.Substring(1);
                Macro macro = null;
                if (RootParsedDocument.Macros.ContainsKey(macroIdentifier))
                {
                    macro = RootParsedDocument.Macros[macroIdentifier];
                }
                else if (RootParsedDocument.ProjectProperty.Macros.ContainsKey(macroIdentifier))
                {
                    macro = RootParsedDocument.ProjectProperty.Macros[macroIdentifier];
                }
                if (macro == null)
                {
                    if (!SupressCompilerDerectiveError) wordPointer.AddError("\" expected");
                    wordPointer.MoveNextUntilEol();
                    return;
                }
                string macroText = macro.MacroText;
                if (macro.Aurguments != null)
                {
                    parseMacroArguments(macro, out macroText);
                }
                macroText = macroText.Replace("`\"", "\"").Replace("\"`", "\"").Replace("``", "").Trim();
                // macro argument tokens are joined with spaces by parseMacroArguments
                // (e.g. "dummy_include . sv"); a file path has no spaces so the
                // separators are re-joined here
                macroText = macroText.Replace(" ", "").Replace("\t", "");
                if (wordPointer.Text == ")") wordPointer.MoveNext(); // closing parenthesis of the macro arguments
                if (macroText.Length < 2 || !macroText.StartsWith("\"") || !macroText.EndsWith("\""))
                {
                    if (!SupressCompilerDerectiveError) wordPointer.AddError("\" expected");
                    return;
                }
                filePath = macroText.Substring(1, macroText.Length - 2);
            }
            else
            {
                if (wordPointer.WordType != WordPointer.WordTypeEnum.String || wordPointer.Text.Length <= 2)
                {
                    if (!SupressCompilerDerectiveError) wordPointer.AddError("\" expected");
                    wordPointer.MoveNextUntilEol();
                    return;
                }
                string quote = wordPointer.Text.Substring(0, 1);
                filePath = wordPointer.Text;
                filePath = filePath.Substring(1, filePath.Length - 2);
            }

            // Project-relative paths use '/' as separator (VerilogFile.RelativePath).
            // Normalize any backslash separators so lookups and path joins stay consistent.
            filePath = filePath.Replace('\\', '/');

            // search in same folder with original verilog file
            Data.IVerilogRelatedFile rootFile;
            if (stock.Count == 0)
            {
                rootFile = wordPointer.VerilogFile;
            }
            else
            {
                rootFile = stock[0].VerilogFile;
            }
            //            Data.IVerilogRelatedFile file = wordPointer.VerilogFile;

            if (rootFile == null)
            {
                return;
            }

            {
                string sameFolderPath = (rootFile.RelativePath ?? "").Replace('\\', '/');
                int sameFolderLastSlash = sameFolderPath.LastIndexOf('/');
                if (sameFolderLastSlash >= 0)
                {
                    sameFolderPath = sameFolderPath.Substring(0, sameFolderLastSlash + 1) + filePath;
                }
                else
                {
                    sameFolderPath = filePath;
                }
                if (wordPointer.ParsedDocument == null) return;
                if (wordPointer.ParsedDocument.Project == null) return;

                List<CodeEditor2.Data.Item> items = wordPointer.ParsedDocument.Project.FindItems(
                    (item) =>
                    {
                        if (item == null) return false;
                        if (item is CodeEditor2.Data.TextFile)
                        {
                            var textFile = item as CodeEditor2.Data.TextFile;
                            if (textFile == null) throw new Exception();
                            string textFileRelative = (textFile.RelativePath ?? "").Replace('\\', '/');
                            if (textFileRelative == sameFolderPath)
                            {
                                return true;
                            }
                        }
                        return false;
                    },
                    (item) =>
                    {
                        return false;
                    }
                );
                if (items.Count > 0)
                {
                    //wordPointer.MoveNext();
                    diveIntoIncludeFile(sameFolderPath);
                    return;
                }

            }

            // search same filename in full project
            if (wordPointer.ParsedDocument != null && wordPointer.ParsedDocument.Project != null)
            {

                // Extract the file name portion so that a path including a
                // directory prefix (e.g. "include_directory/defs.sv") matches a
                // file registered with only its base name.
                string filePathNameOnly = filePath;
                int filePathSlash = filePath.LastIndexOf('/');
                if (filePathSlash >= 0) filePathNameOnly = filePath.Substring(filePathSlash + 1);

                CodeEditor2.Data.File? fFile = wordPointer.ParsedDocument.Project.SearchFile(
                    (f) =>
                    {
                        if (f.Name == filePath) return true;
                        if (f.Name == filePathNameOnly) return true;
                        return false;
                    });
                if (fFile != null)
                {
                    //wordPointer.MoveNext();
                    diveIntoIncludeFile(fFile.RelativePath);
                    return;
                }
            }
            if (!SupressCompilerDerectiveError) wordPointer.AddError("file not found");
            wordPointer.MoveNext();
            recheckWord();
            return;
        }

        private void diveIntoText(string macroText)
        {
            // expand the given text as a new macro-expansion document and push the
            // original scan position onto the hierarchy stock (same as parseMacro)
            stock.Add(wordPointer);
            CodeEditor.CodeDocument codeDocument = new CodeEditor.CodeDocument(macroText);
            string key = "";
            ParsedDocument newParsedDocument = new ParsedDocument(
                (IVerilogRelatedFile)wordPointer.ParsedDocument.TextFile,
                key,
                null,
                wordPointer.ParsedDocument.ParseMode);
            WordPointer newPointer = new WordPointer(codeDocument, newParsedDocument, 0);

            wordPointer = newPointer;

            while (!wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.Comment)
                {
                    wordPointer.MoveNext();
                    if (wordPointer.Eof) break;
                }
                else if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective)
                {
                    parseCompilerDirective();
                    break;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the index of the line terminator at or after <paramref name="index"/>
        /// (or the document length when the line has no terminator).
        /// </summary>
        private int getLineEndIndex(int index)
        {
            CodeEditor2.CodeEditor.CodeDocument document = wordPointer.Document;
            int position = index;
            while (position < document.Length)
            {
                char ch = document.GetCharAt(position);
                if (ch == '\n' || ch == '\r') return position;
                position++;
            }
            return document.Length;
        }

        /// <summary>
        /// Splits the current MoveNextUntilEol token (a whole directive line)
        /// into whitespace separated argument tokens.
        /// </summary>
        private List<string> getDirectiveLineTokens()
        {
            var tokens = new List<string>();
            string line = wordPointer.Text;
            if (string.IsNullOrEmpty(line)) return tokens;
            line = line.Replace('\t', ' ');
            foreach (string token in line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                tokens.Add(token);
            }
            return tokens;
        }

        /// <summary>
        /// Validates `line number "filename" level (IEEE 1800-2017 22.12).
        /// Signed values are kept intact (the minus sign is part of the
        /// number, e.g. "-12"), so the whole line is tokenized directly.
        /// </summary>
        private void parseLineDirective()
        {
            List<string> tokens = getDirectiveLineTokens();
            if (tokens.Count < 2)
            {
                wordPointer.AddError("illegal line directive: filename expected");
                return;
            }

            // number : positive integer
            if (!int.TryParse(tokens[0], out int number) || number <= 0)
            {
                wordPointer.AddError("illegal line directive: number shall be a positive integer");
            }

            // filename : string literal
            if (tokens[1].Length < 2 || tokens[1][0] != '"' || tokens[1][tokens[1].Length - 1] != '"')
            {
                wordPointer.AddError("illegal line directive: filename shall be a string literal");
            }

            // level : 0, 1 or 2 (required)
            if (tokens.Count < 3)
            {
                wordPointer.AddError("illegal line directive: level expected");
            }
            else if (tokens[2] != "0" && tokens[2] != "1" && tokens[2] != "2")
            {
                wordPointer.AddError("illegal line directive: level shall be 0, 1 or 2");
            }
        }

        /// <summary>
        /// Validates `unconnected_drive pull0|pull1 (IEEE 1800-2017 22.9).
        /// </summary>
        private void validateUnconnectedDriveArgument()
        {
            List<string> tokens = getDirectiveLineTokens();
            if (tokens.Count != 1 || (tokens[0] != "pull0" && tokens[0] != "pull1"))
            {
                wordPointer.AddError("illegal unconnected_drive argument: pull0 or pull1 expected");
            }
        }

        private static readonly string[] timescaleUnits = new[] { "s", "ms", "us", "ns", "ps", "fs" };

        /// <summary>
        /// Validates `timescale <time_unit> / <time_precision> (IEEE 1800-2017 22.7).
        /// The unit magnitude shall be 1, 10 or 100 and the precision shall be
        /// at least as precise as the unit.
        /// </summary>
        private void validateTimescaleDirective()
        {
            // "`timescale 1 ns / 1 ps" tokenizes as [1][ns][/][1][ps]
            // (a space is allowed between the magnitude and the time unit);
            // "`timescale 100ps/10ps" is a single [100ps/10ps] token.
            List<string> tokens = getDirectiveLineTokens();
            string unitText;
            string precisionText;
            if (tokens.Count == 1 && tokens[0].Contains('/'))
            {
                int slash = tokens[0].IndexOf('/');
                unitText = tokens[0].Substring(0, slash);
                precisionText = tokens[0].Substring(slash + 1);
            }
            else if (tokens.Count == 5 && tokens[2] == "/")
            {
                unitText = tokens[0] + tokens[1];
                precisionText = tokens[3] + tokens[4];
            }
            else
            {
                wordPointer.AddError("illegal timescale directive: <unit> / <precision> expected");
                return;
            }

            if (!tryParseTimescale(unitText, out int unitMagnitude, out int unitIndex))
            {
                wordPointer.AddError("illegal timescale argument: 1, 10 or 100 with a time unit expected");
                return;
            }
            if (!tryParseTimescale(precisionText, out int precisionMagnitude, out int precisionIndex))
            {
                wordPointer.AddError("illegal timescale argument: 1, 10 or 100 with a time unit expected");
                return;
            }

            // precision shall not be a longer unit of time than the unit
            if (precisionIndex < unitIndex ||
                (precisionIndex == unitIndex && precisionMagnitude > unitMagnitude))
            {
                wordPointer.AddError("illegal timescale: precision shall be at least as precise as time_unit");
            }
        }

        private static bool tryParseTimescale(string text, out int magnitude, out int unitIndex)
        {
            magnitude = 0;
            unitIndex = -1;
            if (string.IsNullOrEmpty(text)) return false;

            int index = 0;
            while (index < text.Length && char.IsDigit(text[index])) index++;
            if (index == 0) return false;
            if (!int.TryParse(text.Substring(0, index), out magnitude)) return false;
            if (magnitude != 1 && magnitude != 10 && magnitude != 100) return false;

            unitIndex = System.Array.IndexOf(timescaleUnits, text.Substring(index));
            return unitIndex >= 0;
        }

        private void parsePredefinedFileMacro()
        {
            // `__FILE__ : predefined macro expanding to the file name string literal
            // (IEEE 1800-2017 22.5.3). Not registered in RootParsedDocument.Macros so
            // the expand-on-the-fly path is always taken.
            string fileName = "";
            if (stock.Count != 0)
            {
                fileName = stock[0].VerilogFile != null ? System.IO.Path.GetFileName(stock[0].VerilogFile.RelativePath ?? "") : "";
            }
            else
            {
                fileName = wordPointer.VerilogFile != null ? System.IO.Path.GetFileName(wordPointer.VerilogFile.RelativePath ?? "") : "";
            }
            diveIntoText("\"" + fileName + "\"");
        }

        private void parsePredefinedLineMacro()
        {
            // `__LINE__ : predefined macro expanding to the current line number
            // (IEEE 1800-2017 22.5.3). Line numbers of the expansion site in the
            // original document.
            int line = 1;
            string docText = wordPointer.Document.CreateString();
            int index = wordPointer.Index;
            if (stock.Count != 0)
            {
                index = stock.Last().Index;
                docText = stock.Last().Document.CreateString();
            }
            for (int i = 0; i < index && i < docText.Length; i++)
            {
                if (docText[i] == '\n') line++;
            }
            diveIntoText(line.ToString());
        }

        // IEEE 1800-2017 20.14 predefined coverage-control macros. The language
        // predefines these names; they expand to the coverage control constants
        // consumed by the $coverage_* system functions.
        private static readonly Dictionary<string, string> predefinedMacros = new Dictionary<string, string>
        {
            { "SV_COV_START",     "0" },
            { "SV_COV_STOP",      "1" },
            { "SV_COV_RESET",     "2" },
            { "SV_COV_CHECK",     "3" },
            { "SV_COV_MODULE",    "10" },
            { "SV_COV_HIER",      "11" },
            { "SV_COV_ASSERTION", "20" },
            { "SV_COV_FSM_STATE", "21" },
            { "SV_COV_STATEMENT", "22" },
            { "SV_COV_TOGGLE",    "23" },
            { "SV_COV_OVERFLOW",  "-2" },
            { "SV_COV_ERROR",     "-1" },
            { "SV_COV_NOCOV",     "0" },
        };

        private void parseMacro()
        {
            Color(CodeDrawStyle.ColorType.Identifier);
            if (wordPointer.Text.Length <= 1)
            {
                if (!SupressCompilerDerectiveError) wordPointer.AddError("unsupported macro call");
                wordPointer.MoveNext();
                return;
            }
            string macroIdentifier = wordPointer.Text.Substring(1);

            Macro macro;
            if (RootParsedDocument.Macros.ContainsKey(macroIdentifier))
            {
                macro = RootParsedDocument.Macros[macroIdentifier];
            }
            else if (RootParsedDocument.ProjectProperty.Macros.ContainsKey(macroIdentifier))
            {
                macro = RootParsedDocument.ProjectProperty.Macros[macroIdentifier];
            }
            else if (predefinedMacros.TryGetValue(macroIdentifier, out string? predefinedText))
            {
                // predefined coverage-control macro (IEEE 1800-2017 20.14)
                macro = Macro.Create(macroIdentifier, predefinedText);
            }
            else
            {
                if (!SupressCompilerDerectiveError) wordPointer.AddError("unsupported macro call");
                wordPointer.MoveNext();
                if (wordPointer.Text == "(")
                {
                    int bracketCount = 1;
                    while (!wordPointer.Eof)
                    {
                        wordPointer.MoveNext();
                        if (wordPointer.Text == ")") bracketCount--;
                        if (bracketCount < 1)
                        {
                            wordPointer.MoveNext();
                            break;
                        }
                        if (General.ListOfStatementStopKeywords.Contains(Text)) break;
                    }
                }
                return;
            }

            string macroText = macro.MacroText;
            if (macro.Aurguments != null)
            {
                parseMacroArguments(macro, out macroText);
            }

            // token pasting / stringification operators (IEEE 1800-2017 22.5.1)
            //  `"..."`" : stringification - expand to a plain string literal
            //  ``       : token pasting  - the tokens around it are joined, so the
            //             operator itself disappears from the expanded text
            macroText = macroText.Replace("`\"", "\"");
            macroText = macroText.Replace("\"`", "\"");
            macroText = macroText.Replace("``", "");

            if (macroText == "")
            {
                wordPointer.MoveNext();
                return;
            }

            stock.Add(wordPointer);
            CodeEditor.CodeDocument codeDocument = new CodeEditor.CodeDocument(macroText);
            string key = "";// Verilog.ParsedDocument.KeyGenerator(wordPointer.VerilogFile,) .KeyGenerator(wordPointer.ParsedDocument, wordPointer.Index);
            ParsedDocument newParsedDocument = new ParsedDocument(
                (IVerilogRelatedFile)wordPointer.ParsedDocument.TextFile,
                key,
                null,
                wordPointer.ParsedDocument.ParseMode);
            WordPointer newPointer = new WordPointer(codeDocument, newParsedDocument, 0);// wordPointer.ParsedDocument);

            wordPointer = newPointer;

            while (!wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.Comment)
                {
                    wordPointer.MoveNext();
                    if (wordPointer.Eof) break;
                }
                else if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective)
                {
                    parseCompilerDirective();
                    break;
                }
                else
                {
                    break;
                }
            }
            return;
        }

        private void parseMacroArguments(Macro macro, out string macroText)
        {
            macroText = macro.MacroText;
            wordPointer.MoveNext();

            List<string> wordAssignment = new List<string>();
            if (wordPointer.Text != "(")
            {
                if (!SupressCompilerDerectiveError) wordPointer.AddError("missing macro arguments");
                wordPointer.MoveNext();
                return;
            }
            wordPointer.MoveNext();

            while (!wordPointer.Eof)
            {
                StringBuilder sb = new StringBuilder();
                int bracketCount = 0;
                while (!wordPointer.Eof)
                {
                    if (wordPointer.Text == "(")
                    {
                        bracketCount++;
                    }
                    else if (wordPointer.Text == ")")
                    {
                        if (bracketCount == 0)
                        {
                            break;
                        }
                        else
                        {
                            bracketCount--;
                        }
                    }

                    if (wordPointer.Text == "," && bracketCount == 0)
                    {
                        break;
                    }

                    if (sb.Length != 0) sb.Append(" ");
                    sb.Append(wordPointer.Text);
                    wordPointer.MoveNext();
                }
                wordAssignment.Add(sb.ToString());
                if (wordPointer.Text == ")")
                {
                    break;
                }
                if (wordPointer.Text == ",")
                {
                    wordPointer.MoveNext();
                    continue;
                }
                if (!SupressCompilerDerectiveError) wordPointer.AddError("illegal macro call");
                break;
            }

            // NOTE: do NOT consume the closing parenthesis of the macro call here.
            // parseMacro restores the original document via returnHierarchy(), which
            // itself advances one token after restoring. Leaving wordPointer on ")"
            // makes that advance land on the token following the macro call
            // (e.g. "endmodule"), whereas consuming ")" here would skip it.

            // If fewer actual arguments are specified than formal arguments and all
            // remaining formal arguments have defaults, substitute the defaults
            // (IEEE 1800-2017 22.5.1). Otherwise report the mismatch.
            if (wordAssignment.Count < macro.Aurguments.Count)
            {
                bool allDefaults = true;
                for (int i = wordAssignment.Count; i < macro.Aurguments.Count; i++)
                {
                    if (macro.ArgumentDefaults == null || i >= macro.ArgumentDefaults.Count || macro.ArgumentDefaults[i] == null)
                    {
                        allDefaults = false;
                        break;
                    }
                }
                if (allDefaults)
                {
                    for (int i = wordAssignment.Count; i < macro.Aurguments.Count; i++)
                    {
                        wordAssignment.Add("");
                    }
                }
            }

            if (macro.Aurguments.Count != wordAssignment.Count)
            {
                if (!SupressCompilerDerectiveError) wordPointer.AddError("macro arguments mismatch");
                return;
            }
            else
            {
                // two-phase replacement : first replace formal argument identifiers with placeholders,
                // then replace placeholders with actual arguments (word-boundary replacement to avoid
                // replacing identifiers embedded in other tokens, e.g. "a" inside "$display")
                for (int i = 0; i < macro.Aurguments.Count; i++)
                {
                    macroText = Macro.ReplaceArgument(macroText, macro.Aurguments[i], "\0" + i.ToString("X4"));
                }
                for (int i = 0; i < macro.Aurguments.Count; i++)
                {
                    // empty actual argument with formal argument default value : use default
                    string actual = wordAssignment[i];
                    if (actual == "" && macro.ArgumentDefaults != null && i < macro.ArgumentDefaults.Count && macro.ArgumentDefaults[i] != null)
                    {
                        actual = macro.ArgumentDefaults[i];
                    }
                    macroText = macroText.Replace("\0" + i.ToString("X4"), actual);
                }
            }
        }



        private void diveIntoIncludeFile(string relativeFilePath)
        {
            if (wordPointer.ParsedDocument.File == null) return;
            string id = wordPointer.ParsedDocument.File.ID + "," + relativeFilePath + "(" + wordPointer.Index + ")";

            Data.IVerilogRelatedFile rootFile;
            if (stock.Count == 0)
            {
                rootFile = wordPointer.VerilogFile;
            }
            else
            {
                rootFile = stock[0].VerilogFile;
            }

            IndexReference indexReference = IndexReference.Create(wordPointer.ParsedDocument.IndexReference, wordPointer.Index);
            ParsedDocument? newParsedDocument;

            if (prototype)
            {
                addIncludeFile(relativeFilePath, rootFile, id, indexReference, out newParsedDocument, wordPointer.Index);
            }
            else
            {
                Data.VerilogHeaderInstance vhInstance = null;
                // When including outside the module, prototype parse is not performed, so only this part is executed.

                // get vhInstance parsed @ prototype
                IndexReference currentIndex = CreateIndexReference();
                foreach (Data.VerilogHeaderInstance vh in wordPointer.ParsedDocument.IncludeFiles.Values)
                {
                    if (currentIndex.IsSameAs(vh.InstancedReference))
                    {
                        vhInstance = vh;
                    }
                }
                if (vhInstance != null)
                {
                    newParsedDocument = vhInstance.VerilogParsedDocument;
                }
                else
                {
                    addIncludeFile(relativeFilePath, rootFile, id, indexReference, out newParsedDocument, wordPointer.Index);
                }
            }
            if (newParsedDocument == null) return;

            WordPointer newPointer = new WordPointer(newParsedDocument.CodeDocument, newParsedDocument,0);

            stock.Add(wordPointer);
            wordPointer = newPointer;
            wordPointer.Document._tag = "diveInto";
            //            System.Diagnostics.Debug.Print("### "+newParsedDocument.File.Name+"  "+wordPointer.InhibitColor.ToString());

            if (wordPointer.Eof)
            {
                MoveNext();
                return;
            }

            while (!wordPointer.Eof)
            {
                if (wordPointer.WordType == WordPointer.WordTypeEnum.Comment)
                {
                    wordPointer.MoveNext();
                }
                else if (wordPointer.WordType == WordPointer.WordTypeEnum.CompilerDirective)
                {
                    parseCompilerDirective();
                    break;
                }
                else
                {
                    break;
                }
            }
        }

        private void addIncludeFile(string relativeFilePath, IVerilogRelatedFile rootFile, string id, IndexReference indexReference, out ParsedDocument? newParsedDocument, int callIndex)
        {
            string name;
            if (relativeFilePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativeFilePath.Substring(relativeFilePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativeFilePath;
            }

            name = name + "(" + callIndex.ToString() + ")";

            //if (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name))
            //{ // avoid duplicate name
            //    int count = 1;
            //    while (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name + ":" + count.ToString()))
            //    {
            //        count++;
            //    }
            //    name = name + ":" + count.ToString();
            //}

            //if (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name))
            //{ // avoid duplicate name
            //    int count = 1;
            //    while (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name + ":" + count.ToString()))
            //    {
            //        count++;
            //    }
            //    name = name + ":" + count.ToString();
            //}


            Data.VerilogHeaderInstance vhInstance = Data.VerilogHeaderInstance.Create(
                                            relativeFilePath,
                                            name,
                                            CreateIndexReference(),
                                            rootFile,
                                            wordPointer.ParsedDocument.Project,
                                            id
                                            );

            if (vhInstance == null)
            {
                if (!Prototype & !SupressCompilerDerectiveError) wordPointer.AddError("illegal file");
                if (wordPointer.Text.StartsWith("\"") || wordPointer.Text.StartsWith("'")) wordPointer.MoveNext();
                newParsedDocument = null;
                return;
            }



            if (!wordPointer.ParsedDocument.IncludeFiles.ContainsKey(vhInstance.ID))
            {
                wordPointer.ParsedDocument.IncludeFiles.Add(vhInstance.ID, vhInstance);
            }
            else
            {
                //                wordPointer.ParsedDocument.IncludeFiles[vhInstance.ID] = vhInstance;
                vhInstance = wordPointer.ParsedDocument.IncludeFiles[vhInstance.ID];
            }

            CodeEditor2.Data.Item? parent = wordPointer.ParsedDocument.File as CodeEditor2.Data.Item;
            if (parent == null) throw new Exception();
            vhInstance.Parent = parent;

            string key = Verilog.ParsedDocument.KeyGenerator(vhInstance.Parent.ID, vhInstance, null, null);
            newParsedDocument = new Verilog.ParsedDocument(vhInstance, key, indexReference, RootParsedDocument.ParseMode);
            CodeDocument? originalDocument = vhInstance.CodeDocument as CodeDocument;
            if (originalDocument == null) throw new Exception();

            // create codeDocument copy
            CodeDocument document = CodeDocument.SnapShotFrom(originalDocument);
            newParsedDocument.CodeDocument = document;

            newParsedDocument.SystemVerilog = wordPointer.ParsedDocument.SystemVerilog;
            if (rootFile != null && rootFile.VerilogParsedDocument != null && rootFile.VerilogParsedDocument.SystemVerilog)
            {
                newParsedDocument.SystemVerilog = true;
            }
            vhInstance.ParsedDocument = newParsedDocument;

        }

    }

}
