using Avalonia.Remote.Protocol;
using pluginVerilog.CodeEditor;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog
{
    public class WordScanner : IDisposable
    {
        public WordScanner( CodeEditor.CodeDocument document, Verilog.ParsedDocument parsedDocument,bool systemVerilog)
        {
            RootParsedDocument = parsedDocument;
            wordPointer = new WordPointer(document, parsedDocument);
            this.systemVerilog = systemVerilog;
        }

        public DefaultNetTypeEnum DefaultNetType = WordScanner.DefaultNetTypeEnum.none;

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
                if(stock.Count == 0)
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
        public WordScanner Clone()
        {
            WordScanner ret = new WordScanner(wordPointer.Document, RootParsedDocument,systemVerilog);
            ret.wordPointer = wordPointer.Clone();
            ret.nonGeneratedCount = nonGeneratedCount;
            ret.prototype = prototype;
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
            //            if (nonGeneratedCount != 0 || prototype) return;
            if (wordPointer.VerilogFile != null && wordPointer.VerilogFile.RelativePath.EndsWith(".vh"))
            {
                string s = "";
            }
            wordPointer.Color(colorType);
        }

        public void AppendBlock(IndexReference startIndexReference, IndexReference lastIndexReference)
        {
            appendBlock(startIndexReference, lastIndexReference, null, null);
        }

        public void AppendBlock(IndexReference startIndexReference, IndexReference lastIndexReference, string name,bool defaultClose)
        {
            appendBlock(startIndexReference, lastIndexReference, name, defaultClose);
        }
        public void appendBlock(IndexReference startIndexReference, IndexReference lastIndexReference, string? name,bool? defaultClose)
        {
            if (startIndexReference.Indexes.Count != lastIndexReference.Indexes.Count) return;
            for (int i = 0; i < startIndexReference.Indexes.Count - 1; i++)
            {
                if (startIndexReference.Indexes[i] != lastIndexReference.Indexes[i]) return;
            }

            if (wordPointer.Document.GetLineAt(startIndexReference.Indexes.Last()) == wordPointer.Document.GetLineAt(lastIndexReference.Indexes.Last())) return;

            if(name == null)
            {
                wordPointer.AppendBlock(startIndexReference.Indexes.Last(), lastIndexReference.Indexes.Last());
            }
            else if(defaultClose != null)
            {
                 wordPointer.AppendBlock(startIndexReference.Indexes.Last(), lastIndexReference.Indexes.Last(),name, (bool)defaultClose);
            }
        }

        private bool systemVerilogError = false;
        public void AddSystemVerilogError()
        {
            if (RootParsedDocument.SystemVerilog) return;
            if (systemVerilogError) return;
            AddError("SystemVerilog Description");
        }

        public void AddError(string message)
        {
            if (prototype) return;
            wordPointer.AddError(message);
        }

        public void AddWarning(string message)
        {
            if (prototype) return;
            wordPointer.AddWarning(message);
        }

        public void AddPrototypeError(string message)
        {
//            if (prototype) return;
            wordPointer.AddError(message);
        }

        public void AddPrototypeWarning(string message)
        {
//            if (prototype) return;
            wordPointer.AddWarning(message);
        }

        public void AddNotice(string message)
        {
            if (prototype) return;
            wordPointer.AddNotice(message);
        }
        public void AddHint(string message)
        {
            if (prototype) return;
            wordPointer.AddHint(message);
        }

        public void ApplyPrototypeRule(Rule rule)
        {
            ApplyPrototypeRule(rule, "");
        }
        public void ApplyPrototypeRule(Rule rule,string message)
        {
//            if (!prototype) System.Diagnostics.Debugger.Break();
            applyRule(rule, message);
        }
        public void ApplyRule(Rule rule)
        {
            ApplyRule(rule, "");
        }
        public void ApplyRule(Rule rule, string message)
        {
            if (prototype) return;
            applyRule(rule,message);
        }
        private void applyRule(Rule rule,string message)
        {
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
                if(stock.Count == 0)
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
            moveNext();

        }

        private void moveNext()
        {
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
                        wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
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
                    wordPointer.AddError("include errors");
                }
                else if (warning)
                {
                    wordPointer.AddWarning("include warnings");
                }
            }
            wordPointer.MoveNext();
        }

        public bool Eof
        {
            get
            {
                if(stock.Count == 0) return wordPointer.Eof;
                for(int i= stock.Count - 1; i >= 0; i--)
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
                if(stock.Count != _stock.Count)
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

        private void parseCompilerDirective()
        {
            switch (wordPointer.Text)
            {
                case "`include":
                    parseInclude();
                    break;
                case "`define":
                    parseDefine();
                    break;
                case "`celldefine":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    cellDefine = true;
                    wordPointer.MoveNext();
                    break;
                case "`resetall":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    break;
                case "`endcelldefine":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    cellDefine = false;
                    wordPointer.MoveNext();
                    break;
                case "`default_nettype":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
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
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "tri0":
                            DefaultNetType = DefaultNetTypeEnum.tri0;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "wand":
                            DefaultNetType = DefaultNetTypeEnum.wand;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "triand":
                            DefaultNetType = DefaultNetTypeEnum.triand;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "wor":
                            DefaultNetType = DefaultNetTypeEnum.wor;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "trior":
                            DefaultNetType = DefaultNetTypeEnum.trior;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        case "trireg":
                            DefaultNetType = DefaultNetTypeEnum.trireg;
                            wordPointer.AddError("not supported");
                            wordPointer.MoveNext();
                            break;
                        default:
                            wordPointer.AddError("illegal netType");
                            wordPointer.MoveNext();
                            break;
                    }
                    break;
                case "`endif":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    if (ifDefList.Count != 0) ifDefList.Remove(ifDefList.Last());
                    break;
                case "`ifdef":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        RootParsedDocument.Macros.ContainsKey(wordPointer.Text) ||
                        RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
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
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        !RootParsedDocument.Macros.ContainsKey(wordPointer.Text) &&
                        !RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
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
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    skip();
                    break;
                case "`elsif":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNext();
                    wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                    if (
                        RootParsedDocument.Macros.ContainsKey(wordPointer.Text) ||
                        RootParsedDocument.ProjectProperty.Macros.ContainsKey(wordPointer.Text)
                        )
                    {   // true
                        wordPointer.MoveNext();
                        if (wordPointer.Text == "`else")
                        {
                            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
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
                case "`nounconnected_drive":
                case "`unconnected_drive":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.AddError("unsupported compiler directive");
                    wordPointer.MoveNext();
                    break;
                case "`timescale":
                    wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                    wordPointer.MoveNextUntilEol();
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
                            wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                        case "`ifndef":
                            depth++;
                            wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                        case "`else":
                            if (depth == 0)
                            {
                                wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                                wordPointer.MoveNext();
                                return;
                            }
                            else
                            {
                                wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                                wordPointer.MoveNext();
                            }
                            break;
                        case "`endif":
                            if (depth == 0)
                            {
                                wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
                                wordPointer.MoveNext();
                                return;
                            }
                            else
                            {
                                depth--;
                                wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                                wordPointer.MoveNext();
                            }
                            break;
                        default:
                            wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                            wordPointer.MoveNext();
                            break;
                    }
                }
                else
                {
                    wordPointer.Color(CodeDrawStyle.ColorType.Inactivated);
                    wordPointer.MoveNext();
                }
            }
        }

        private void parseUndef()
        {
            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
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
            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNextUntilEol();

            WordReference wordRef = GetReference();
            string macroText = wordPointer.Text;
            int index = wordPointer.Index;

            while(macroText.EndsWith("\\") && !wordPointer.Eof)
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
                wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                macroText = "";
            }
            else
            {
                identifier = macroText.Substring(0, separatorIndex);
                wordPointer.Color(CodeDrawStyle.ColorType.Identifier, index, index+separatorIndex);
                macroText = macroText.Substring(separatorIndex);
            }

            Macro macro = Macro.Create(identifier, macroText);
            if (!General.IsIdentifier(macro.Name))
            {
                wordRef.AddError("illegal macro identifier");
            }
            else if (RootParsedDocument.Macros.ContainsKey(macro.Name))
            {
                wordRef.AddError("duplicate macro name");
            }
            else
            {
                RootParsedDocument.Macros.Add(macro.Name, macro);
            }


            wordPointer.MoveNext();
            recheckWord();
        }

        private void parseInclude()
        {
            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNext();
            if(wordPointer.WordType != WordPointer.WordTypeEnum.String || wordPointer.Text.Length<=2)
            {
                wordPointer.AddError("\" expected");
                wordPointer.MoveNextUntilEol();
                return;
            }

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

            string quote = wordPointer.Text.Substring(0, 1);
            string filePath = wordPointer.Text;
            filePath = filePath.Substring(1, filePath.Length - 2);

            if (filePath.Contains('/') && System.IO.Path.DirectorySeparatorChar !='/')
            {
                filePath = filePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            if (filePath.Contains('\\') && System.IO.Path.DirectorySeparatorChar != '\\')
            {
                filePath = filePath.Replace('\\', System.IO.Path.DirectorySeparatorChar);
            }

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
                throw new Exception();
            }

            {
                string sameFolderPath = rootFile.RelativePath;
                if (sameFolderPath.Contains(System.IO.Path.DirectorySeparatorChar))
                {
                    sameFolderPath = sameFolderPath.Substring(0, sameFolderPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
                    sameFolderPath = sameFolderPath + System.IO.Path.DirectorySeparatorChar + filePath;
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
                            if (textFile.RelativePath == sameFolderPath)
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
            if(wordPointer.ParsedDocument!= null && wordPointer.ParsedDocument.Project !=null) {

                CodeEditor2.Data.File? fFile = wordPointer.ParsedDocument.Project.SearchFile(
                    (f)=> {
                    if (f.Name == filePath) return true;
                    return false;
                    });
                if (fFile != null)
                {
                    //wordPointer.MoveNext();
                    diveIntoIncludeFile(fFile.RelativePath);
                    return;
                }
            }
            wordPointer.AddError("file not found");
            wordPointer.MoveNext();
            recheckWord();
            return;
        }

        private void parseMacro()
        {
            wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
            string macroIdentifier = wordPointer.Text.Substring(1);

            Macro macro;
            if (RootParsedDocument.Macros.ContainsKey(macroIdentifier))
            {
                macro = RootParsedDocument.Macros[macroIdentifier];
            }
            else if(RootParsedDocument.ProjectProperty.Macros.ContainsKey(macroIdentifier))
            {
                macro = RootParsedDocument.ProjectProperty.Macros[macroIdentifier];
            }
            else
            {
                wordPointer.AddError("unsupported macro call");
                wordPointer.MoveNext();
                if(wordPointer.Text == "(")
                {
                    int bracketCount = 1;
                    while (true)
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
                parseMacroArguments(macro,out macroText);
            }

            if(macroText == "")
            {
                wordPointer.MoveNext();
                return;
            }

            stock.Add(wordPointer);
            CodeEditor.CodeDocument codeDocument = new CodeEditor.CodeDocument(macroText);
            ParsedDocument newParsedDocument = new ParsedDocument((IVerilogRelatedFile)wordPointer.ParsedDocument.TextFile, null, wordPointer.ParsedDocument.ParseMode);
            WordPointer newPointer = new WordPointer(codeDocument, newParsedDocument );// wordPointer.ParsedDocument);

            wordPointer = newPointer;

            while (true)
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

        private   void parseMacroArguments(Macro macro,out string macroText)
        {
            macroText = macro.MacroText;
            wordPointer.MoveNext();

            List<string> wordAssignment = new List<string>();
            if (wordPointer.Text != "(")
            {
                wordPointer.AddError("missing macro arguments");
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
                wordPointer.AddError("illegal macro call");
                break;
            }

            if (macro.Aurguments.Count != wordAssignment.Count)
            {
                wordPointer.AddError("macro arguments mismatch");
                return;
            }
            else
            {
                for (int i = 0; i < macro.Aurguments.Count; i++)
                {
                    macroText = macroText.Replace(macro.Aurguments[i], "\0" + i.ToString("X4"));
                }
                for (int i = 0; i < macro.Aurguments.Count; i++)
                {
                    macroText = macroText.Replace("\0" + i.ToString("X4"), wordAssignment[i]);
                }
            }
        }



        private void diveIntoIncludeFile(string relativeFilePath)
        {
            if (wordPointer.ParsedDocument.File == null) return;
            string id = wordPointer.ParsedDocument.File.ID + ","+ relativeFilePath +"_"+ wordPointer.ParsedDocument.IncludeFiles.Count.ToString();

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
                addIncludeFile(relativeFilePath, rootFile, id, indexReference, out newParsedDocument);
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
                    addIncludeFile(relativeFilePath, rootFile, id, indexReference, out newParsedDocument);
                }
            }
            if (newParsedDocument == null) return;

            WordPointer newPointer = new WordPointer(newParsedDocument.CodeDocument, newParsedDocument);

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

        private void addIncludeFile(string relativeFilePath, IVerilogRelatedFile rootFile,string id, IndexReference indexReference, out ParsedDocument? newParsedDocument)
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

            if (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name))
            { // avoid duplicate name
                int count = 1;
                while (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name + ":" + count.ToString()))
                {
                    count++;
                }
                name = name + ":" + count.ToString();
            }

            if (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name))
            { // avoid duplicate name
                int count = 1;
                while (wordPointer.ParsedDocument.IncludeFiles.ContainsKey(name + ":" + count.ToString()))
                {
                    count++;
                }
                name = name + ":" + count.ToString();
            }


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
                wordPointer.AddError("illegal file");
                newParsedDocument = null;
                return;
            }



            if (!wordPointer.ParsedDocument.IncludeFiles.ContainsKey(vhInstance.ID))
            {
                wordPointer.ParsedDocument.IncludeFiles.Add(vhInstance.ID, vhInstance);
            }
            else
            {
                vhInstance = wordPointer.ParsedDocument.IncludeFiles[vhInstance.ID];
            }

            CodeEditor2.Data.Item? parent = wordPointer.ParsedDocument.File as CodeEditor2.Data.Item;
            if (parent == null) throw new Exception();
            vhInstance.Parent = parent;

            newParsedDocument = new Verilog.ParsedDocument(vhInstance, indexReference, RootParsedDocument.ParseMode);
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