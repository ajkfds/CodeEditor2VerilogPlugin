using Avalonia.Media.TextFormatting;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using ExCSS;
using pluginVerilog.CodeEditor;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CodeComplete = CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog
{
    public class ParsedDocument : CodeEditor2.CodeEditor.ParsedDocument
    {
        public ParsedDocument(Data.IVerilogRelatedFile file, IndexReference? indexReference, DocumentParser.ParseModeEnum parseMode) : base(file as CodeEditor2.Data.TextFile,file.CodeDocument.Version,parseMode)
        {
            CodeDocument? document = file.CodeDocument as CodeDocument;
            if (document == null) throw new Exception();
            codeDocument = document;

            fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
            if(indexReference == null)
            {
                IndexReference = IndexReference.Create(this,document,0);
            }
            else
            {
                IndexReference = indexReference;
            }

            tag = "verilogParsedDocument" + tagCount.ToString();
            if (tagCount == int.MaxValue)
            {
                tagCount = 0;
            }
            else
            {
                tagCount++;
            }
        }

        public static int tagCount = 0;
        public string tag;

        private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
        public Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public IndexReference IndexReference;

        public List<int> Indexes = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;

        public Root? Root;

        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();

        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides = new Dictionary<string, Expressions.Expression>();
        public string TargetBuildingBlockName = null;

        // for IndexReference
//        public Dictionary<int, ParsedDocument> ParsedDocumentIndexDictionary = new Dictionary<int, ParsedDocument>();

        private bool reparseRequested = false;
        public bool ReparseRequested
        {
            get { return reparseRequested; }
            set {
                reparseRequested = value;
            }
        }

        public void ReloadIncludeFiles()
        {
            foreach(var includeFile in IncludeFiles.Values)
            {
                includeFile.Close();
            }
        }

        CodeDocument codeDocument;
        public CodeDocument CodeDocument
        {
            get
            {
                return codeDocument;
            }
            set
            {
                codeDocument = value;
            }
        }

        public ProjectProperty ProjectProperty
        {
            get
            {
                Data.IVerilogRelatedFile file = File;
                if (file == null) return null;
                return file.ProjectProperty;
            }
        }

        public override void Dispose()
        {
            Data.IVerilogRelatedFile file = File;

            if (!Instance)
            {
                if (file is Data.VerilogFile)
                {
                    Data.VerilogFile? verilogFile = file as Data.VerilogFile;
                    if(verilogFile!= null)
                    {
                        foreach (BuildingBlock module in Root.BuldingBlocks.Values)
                        {
                            verilogFile.ProjectProperty.RemoveBuildingBlock(module.Name);
                        }
                    }
                }
                foreach (var includeFile in IncludeFiles.Values)
                {
                    includeFile.Close();
                }
            }
            base.Dispose();
        }

        public int ErrorCount = 0;
        public int WarningCount = 0;
        public int HintCount = 0;
        public int NoticeCount = 0;

        public void AddError(int index,int length ,string message)
        {
            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (ErrorCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Error, index, lineNo, length, Project));
            }
            else if (ErrorCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 errors", Verilog.ParsedDocument.Message.MessageType.Error, 0, 0, 0, Project)); ;
            }

            // increment message count
            ErrorCount++;

            // add mark
            document.Marks.SetMarkAt(index, length, 0);
        }

        public void AddWarning(int index, int length, string message)
        {
            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (WarningCount < 100)
            {
                int lineNo = document.GetLineAt(index);
               Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Warning, index, lineNo, length, Project));
            }
            else if (WarningCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 warnings", Verilog.ParsedDocument.Message.MessageType.Warning, 0, 0, 0, Project));
            }

            // increment message count
            WarningCount++;

            // add mark
            document.Marks.SetMarkAt(index, length, 1);
        }
        public void AddNotice(int index, int length, string message)
        {
            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (NoticeCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Notice, index, lineNo, length, Project));
            }
            else if (NoticeCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Notice, 0, 0, 0, Project));
            }

            // increment message count
            NoticeCount++;

            // add mark
            document.Marks.SetMarkAt(index, length, 2);
        }
        public void AddHint(int index, int length, string message)
        {
            CodeDocument? document = CodeDocument;
            if (document == null) return;

            Data.IVerilogRelatedFile? vFile = document.TextFile as Data.IVerilogRelatedFile;
            if (vFile == null) return;

            // add message
            if (HintCount < 100)
            {
                int lineNo = document.GetLineAt(index);
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, message, Verilog.ParsedDocument.Message.MessageType.Hint, index, lineNo, length, Project));
            }
            else if (HintCount == 100)
            {
                Messages.Add(new Verilog.ParsedDocument.Message(vFile, ">100 notices", Verilog.ParsedDocument.Message.MessageType.Hint, 0, 0, 0, Project));
            }

            // increment message count
            HintCount++;

            // add mark
            document.Marks.SetMarkAt(index, length, 3);
        }


        public PopupItem GetPopupItem(int index, string text)
        {
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            if (iref.RootParsedDocument == null) return null;


            var ret = new CodeEditor2.CodeEditor.PopupHint.PopupItem();

            // add messages
            foreach (Message message in Messages)
            {
                if (index < message.Index) continue;
                if (index > message.Index + message.Length) continue;
                switch (message.Type)
                {
                    case Message.MessageType.Error:
                        ret.AppendText(message.Text+"\n", Avalonia.Media.Colors.Pink);
                        break;
                    case Message.MessageType.Warning:
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.Orange);
                        break;
                    case Message.MessageType.Notice:
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.LimeGreen);
                        break;
                    case Message.MessageType.Hint:
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.LightCyan);
                        break;
                }
            }

            NameSpace? space = iref.RootParsedDocument.Root;

            foreach (BuildingBlock module in iref.RootParsedDocument.Root.BuldingBlocks.Values)
            {
                if (module.BeginIndexReference == null) continue;
                if (module.LastIndexReference == null) continue;

                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                space = module.GetHierarchyNameSpace(iref);
                break;
            }

            int count = ret.ItemCount;

            if(space != null)
            {
                foreach (IBuildingBlockInstantiation instantiation in space.BuildingBlock.NamedElements.Values.OfType<IBuildingBlockInstantiation>())
                {
                    if (instantiation.BeginIndexReference == null) continue;
                    if (instantiation.LastIndexReference == null) continue;

                    if (iref.IsSmallerThan(instantiation.BeginIndexReference)) continue;
                    if (iref.IsGreaterThan(instantiation.LastIndexReference)) continue;
                    instantiation.AppendLabel(iref, ret);
                    break;
                }
            }

            if (ret.ItemCount != count) return ret;

            if (text.StartsWith("`") && Macros.ContainsKey(text.Substring(1)))
            {
                Macro macro = Macros[text.Substring(1)];
                macro.AppendLabel(ret,Macros);
            }

            if (space != null && space.NamedElements.ContainsKey(text))
            {
                DataObject? dataObject = space.NamedElements.GetDataObject(text);
                if(dataObject!=null) dataObject.AppendLabel(ret);
            }

            {
                DataObjects.Constants.Constants param = space.GetConstants(text);
                if (param != null)
                {
                    param.AppendLabel(ret);
                }
            }

            if (text.StartsWith("`") && iref.RootParsedDocument.Macros.ContainsKey(text.Substring(1)))
            {
//                ret.Add(new Popup.MacroPopup(text.Substring(1), iref.RootParsedDocument.Macros[text.Substring(1)].MacroText));
            }

//            if (space.BuildingBlock.Functions.ContainsKey(text))
//            {
////                ret.Add(new Popup.FunctionPopup(space.BuildingBlock.Functions[text]));
//            }

//            if (space.BuildingBlock.Tasks.ContainsKey(text))
//            {
////                ret.Add(new Popup.TaskPopup(space.BuildingBlock.Tasks[text]));
//            }

            return ret;
        }


        public BuildingBlock? GetBuildingBlockAt(int index)
        {
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            foreach (BuildingBlock module in Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                return module;
            }
            return null;
        }


        private static List<AutocompleteItem>? verilogAutoCompleteItems = null;
        private static List<AutocompleteItem> VerilogAutoCompleteItems
        {
            get
            {
                if(verilogAutoCompleteItems == null)
                {
                    verilogAutoCompleteItems = new List<AutocompleteItem>();

                    List<string> keywords = new List<string>
                    {
                        // Verilog
                        "always",       "and",          "assign",       "automatic",
                        /*"begin",*/    "case",         "casex",        "casez",
                        "deassign",     "default",      "defparam",     "design",
                        "disable",      "edge",         "else",         "end",
                        "endcase",      "endfunction",  "endgenerate",  "endmodule",
                        "endprimitive", "endspecify",   "endtask",      "for",
                        "force",        "forever",      "fork",         /*"function",*/
                        /*"generate",*/ "genvar",       "if",           "incdir",
                        "include",      "initial",      "inout",        "input",
                        "integer",      /*"interface",*/"join",         "localparam",
                        /*"module",*/   "nand",         "negedge",      "nor",
                        "not",          "or",           "output",       "parameter",
                        "posedge",      "pulldown",     "pullup",       "real",
                        "realtime",     "reg",          "release",      "repeat",
                        "signed",       /*"task",*/     "time",         "tri0",
                        "tri1",         "trireg",       "unsigned",     "vectored",
                        "wait",         "wand",         "weak0",        "weak1",    
                        "while",        "wire",         "wor",
                        // SystemVerilog
                        "accept_on",    "alias",        "always_comb",  "always_ff",
                        "always_latch", "assert",       "assume",       "before",
                        "bind",         "bins",         "binsof",       "bit",
                        "break",        "byte",         "chandle",      "checker",
                        "class",        "clocking",     "const",        "constraint",
                        "context",      "continue",     "cover",        "covergroup",
                        "coverpoint",   "cross",        "dist",         "do",
                        "endchecker",   "endclass",     "endclocking",  "endgroup",
                        "endinterface", "endpackage",   "endprogram",   "endproperty",
                        "endsequence",  "enum",         "eventually",   "expect",
                        "export",       "extends",      "extern",       "final",
                        "first_match",  "foreach",      "forkjoin",     "global",
                        "iff",          "ignore_bins",  "illegal_bins", "implements",
                        "implies",      "import",       "inside",       "int",
                        "interconnect", "interface",    "intersect",    "join_any",
                        "join_none",    "let",          "local",        "logic",
                        "longint",      "matches",      "modport",      "nettype",
                        "new",          "nexttime",     "null",         "package",
                        "packed",       "priority",     "program",      "property",
                        "protected",    "pure",         "rand",         "randc",
                        "randcase",     "randsequence", "ref",          "reject_on",
                        "restrict",     "return",       "s_always",     "s_eventually",
                        "s_nexttime",   "s_until",      "s_until_with", "sequence",
                        "shortint",     "shortreal",    "soft",         "solve",
                        "static",       "string",       "strong",       "struct",
                        "super",        "sync_accept_on",   "sync_reject_on",   "tagged",
                        "this",         "throughout",   "timeprecision",    "timeunit",
                        "type",         "typedef",      "union",        "unique",
                        "unique0",      "until",        "until_with",   "untyped",
                        "uwire",        "var",          "virtual",      "void",
                        "wait_order",   "weak",         "wildcard",     "with",
                        "within",
                    };
                    foreach (string keyword in keywords)
                    {
                        verilogAutoCompleteItems.Add(
                            new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                            keyword,
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), 
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            )
                        );
                    }

                    List<AutocompleteItem> specialItems = new List<AutocompleteItem>()
                    {
                        new AutoComplete.BeginAutoCompleteItem(
                            "begin",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), 
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.FunctionAutocompleteItem(
                            "function",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), 
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.GenerateAutoCompleteItem(
                            "generate",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), 
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.ModuleAutocompleteItem(
                            "module",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.InterfaceAutocompleteItem(
                            "interface",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.TaskAutocompleteItem(
                            "task",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            ),
                        new AutoComplete.NonBlockingAssignmentAutoCompleteItem(
                            "<=",
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Normal),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal)
                            ),
                    };

                    foreach (AutocompleteItem item in specialItems)
                    {
                        verilogAutoCompleteItems.Add(item);
                    }




                }
                return verilogAutoCompleteItems;
            }
        }




        private NameSpace? getSearchNameSpace(NameSpace? nameSpace,List<string> hier)
        {
            if (nameSpace == null) return null;
            BuildingBlock? buildingBlock = nameSpace.BuildingBlock;
            if (buildingBlock == null) return null;

            if (hier.Count == 0) return nameSpace;

            if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(hier[0]))
            {
                IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)buildingBlock.NamedElements[hier[0]];
                NameSpace? bBlock = ProjectProperty.GetInstancedBuildingBlock(instantiation);

                if (instantiation is InterfaceInstantiation)
                {
                    InterfaceInstantiation? interfaceInstantiation = instantiation as InterfaceInstantiation;
                    if (interfaceInstantiation == null) throw new Exception();
                    if(interfaceInstantiation.ModPortName != null)
                    {
                        Interface? instance = bBlock as Interface;
                        if (instance == null) throw new Exception();
                        if (instance.ModPorts.ContainsKey(interfaceInstantiation.ModPortName)) bBlock = instance.ModPorts[interfaceInstantiation.ModPortName];
                    }
                }

                hier.RemoveAt(0);
                return getSearchNameSpace(bBlock,hier);
            }
            else if(nameSpace.NamedElements.ContainsKey(hier[0]))
            {
                NameSpace space = nameSpace.NamedElements[hier[0]] as NameSpace;
                hier.RemoveAt(0);
                return getSearchNameSpace(space, hier);
            }
            return nameSpace;
        }

        public List<AutocompleteItem>? GetAutoCompleteItems(List<string> hierWords,int index,int line,CodeEditor.CodeDocument document,string candidateWord)
        {
            List<AutocompleteItem>? items = null;

            if (Root == null || Root.BuldingBlocks == null)
            {
                items = VerilogAutoCompleteItems.ToList();
                return items;
            }

            // get reference of current position
            IndexReference iref = IndexReference.Create(this.IndexReference, index);

            // get current buldingBlock
            NameSpace? space = null;
            foreach (BuildingBlock buildingBlock in Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(buildingBlock.BeginIndexReference)) continue;
                if (buildingBlock.LastIndexReference == null) break;
                if (iref.IsGreaterThan(buildingBlock.LastIndexReference)) continue;
                space = buildingBlock.GetHierarchyNameSpace(iref);
                break;
            }

            // external module/class/program
            if (space == null)
            {
                items = VerilogAutoCompleteItems.ToList();
                int headIndex;
                int length;
                document.GetWord(index, out headIndex, out length);
                return items;
            }

            // system task & functions
            // return system task and function if the word starts with "$"
            if (candidateWord.StartsWith("$"))
            {
                items = new List<AutocompleteItem>();
                foreach (string key in ProjectProperty.SystemFunctions.Keys)
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                            key,
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            )
                    );
                }
                foreach (string key in ProjectProperty.SystemTaskParsers.Keys)
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                            key,
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                            )
                    );
                }
                return items;
            }

            // if no hierarchy, add standerd compelete items
            if (hierWords.Count == 0)
            {
                items = VerilogAutoCompleteItems.ToList();
            }
            else
            {
                items = new List<AutocompleteItem>();
            }

            // if no hierarchy, add root level object name list
            if (hierWords.Count == 0)
            {
                List<string> objectList = ProjectProperty.GetObjectsNameList();
                foreach(var name in objectList) 
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                            name,
                            CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Identifier),
                            Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier)
                            )
                    );
                }
            }

            // parse macro in hierarchy words
            for (int i = 0; i < hierWords.Count;i++)
            {
                if (!hierWords[i].StartsWith("`")) continue;
                
                string macroText = hierWords[i].Substring(1);
                if (!Macros.ContainsKey(macroText)) continue;
                Macro macro = Macros[macroText];
                if (macro.MacroText.Contains(' ')) continue;
                if (macro.MacroText.Contains('\t')) continue;

                string[] swapTexts = macro.MacroText.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                hierWords.RemoveAt(i);
                for(int j = swapTexts.Length - 1; j >= 0; j--)
                {
                    hierWords.Insert(i, swapTexts[j]);
                }
            }

            // GetDataObject


            // get nameSpace autocomplete item
            NameSpace? nameSpace = getSearchNameSpace(space, hierWords);
            if (nameSpace != null)
            {
                nameSpace.AppendAutoCompleteItem(items);
            }

            return items;
        }


        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }


        public new class Message : CodeEditor2.CodeEditor.ParsedDocument.Message
        {
            public Message(Data.IVerilogRelatedFile file,string text, MessageType type, int index, int lineNo,int length,CodeEditor2.Data.Project project)
            {
                this.fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
                this.Text = text;
                this.Length = length;
                this.Index = index;
                this.LineNo = lineNo;
                this.Type = type;
                this.Project = project;
            }

            private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
            public Data.IVerilogRelatedFile File
            {
                get
                {
                    Data.IVerilogRelatedFile file;
                    if (!fileRef.TryGetTarget(out file)) return null;
                    return file;
                }
            }

            public int LineNo { get; protected set; }

            public enum MessageType
            {
                Error,
                Warning,
                Notice,
                Hint
            }
            public MessageType Type { get; protected set; }

            public override CodeEditor2.MessageView.MessageNode CreateMessageNode()
            {
                MessageView.MessageNode node = new MessageView.MessageNode(File, this);
                return node;
            }

        }
    }

}
