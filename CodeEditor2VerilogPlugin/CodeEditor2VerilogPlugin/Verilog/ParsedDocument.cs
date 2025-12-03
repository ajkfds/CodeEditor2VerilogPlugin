using Avalonia.Media.TextFormatting;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using ExCSS;
using pluginVerilog.CodeEditor;
using pluginVerilog.NavigatePanel;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.ModuleItems;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AjkAvaloniaLibs.Controls.ColorLabel;
using CodeComplete = CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog
{
    public class ParsedDocument : CodeEditor2.CodeEditor.ParsedDocument
    {
        public ParsedDocument(Data.IVerilogRelatedFile file,string key, IndexReference? indexReference, DocumentParser.ParseModeEnum parseMode) : base((CodeEditor2.Data.TextFile)file, key, getCodeDocument(file).Version,parseMode)
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
        }

        [Newtonsoft.Json.JsonConstructor]
        private ParsedDocument(Data.IVerilogRelatedFile file, string key, IndexReference? indexReference) : base((CodeEditor2.Data.TextFile)file, key, long.MaxValue, DocumentParser.ParseModeEnum.LoadParse)
        {
            System.Diagnostics.Debug.Print("## parsed " + key);
            if (file == null)
            {
                System.Diagnostics.Debug.Print("## parsed null " + key);
            }
        }

        private static CodeDocument getCodeDocument(Data.IVerilogRelatedFile file)
        {
            if (file == null) return null; // foe illeagal jon constructor 
            CodeDocument? codeDocument;
            codeDocument = file.CodeDocument as CodeDocument;
            if(codeDocument == null)
            {
                System.Diagnostics.Debugger.Break();
            }
            return codeDocument;
        }

        public static string KeyGenerator(
            Data.IVerilogRelatedFile verilogRelatedFile,
            string? moduleName,
            Dictionary<string, Verilog.Expressions.Expression>? parameterOverrides
            )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(verilogRelatedFile.RelativePath);
            if (moduleName == null) return sb.ToString(); // base Parse

            sb.Append(":");
            sb.Append(moduleName);
            if (parameterOverrides == null || parameterOverrides.Count == 0) return sb.ToString();

            sb.Append(":");
            bool firstItem = true;
            foreach (var pair in parameterOverrides)
            {
                if (!firstItem) sb.Append(",");
                firstItem = false;
                sb.Append(pair.Key);
                sb.Append("=");
                sb.Append(pair.Value.ConstantValueString());
            }
            return sb.ToString();
        }
        public static string KeyGenerator(
            string key,
            Data.IVerilogRelatedFile verilogRelatedFile,
            string? moduleName,
            Dictionary<string, Verilog.Expressions.Expression>? parameterOverrides
            )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(key);
            sb.Append(":");
            sb.Append(KeyGenerator(verilogRelatedFile, moduleName, parameterOverrides));
            return sb.ToString();
        }

        ~ParsedDocument()
        {
//            System.Diagnostics.Debug.Print("### pasedDocument.Finalize " + id+"::"+ObjectID);
        }

        private System.WeakReference<Data.IVerilogRelatedFile> fileRef;
        [Newtonsoft.Json.JsonIgnore]
        public Data.IVerilogRelatedFile? File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public IndexReference IndexReference;

        [Newtonsoft.Json.JsonIgnore]
        public List<int> Indexes = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;

        public bool RestrictBaseParse { get; set; } = false;
        public Root Root { set; get; }

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides = new Dictionary<string, Expressions.Expression>();
        [Newtonsoft.Json.JsonIgnore]
        public string? TargetBuildingBlockName = null;



        // for IndexReference
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
        [Newtonsoft.Json.JsonIgnore]
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

        [Newtonsoft.Json.JsonIgnore]
        public ProjectProperty? ProjectProperty
        {
            get
            {
                Data.IVerilogRelatedFile? file = File;
                if (file == null) return null;
                return file.ProjectProperty;
            }
        }

        public override void Dispose()
        {
            if (Root == null) return;

            Data.IVerilogRelatedFile? file = File;

            /*
            if (!Instance)
            {
                if (file is Data.VerilogFile)
                {
                    Data.VerilogFile? verilogFile = file as Data.VerilogFile;
                    if (verilogFile != null)
                    {
                        foreach (BuildingBlock module in Root.BuildingBlocks.Values)
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
            */
            base.Dispose();
        }

        [Newtonsoft.Json.JsonIgnore]
        public int ErrorCount = 0;
        [Newtonsoft.Json.JsonIgnore]
        public int WarningCount = 0;
        [Newtonsoft.Json.JsonIgnore]
        public int HintCount = 0;
        [Newtonsoft.Json.JsonIgnore]
        public int NoticeCount = 0;

        public void AddError(int index,int length ,string message)
        {
            if (Project == null) return;

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
            if (Project == null) return;

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
            if (Project == null) return;

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
            if (Project == null) return;

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


        public PopupItem? GetPopupItem(int index, string text)
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
                        ret.AppendIconImage(AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 255, 150, 150)
                            ));
                        ret.AppendText(message.Text+"\n", Avalonia.Media.Colors.Pink);
                        break;
                    case Message.MessageType.Warning:
                        ret.AppendIconImage(AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 255, 255, 150)
                            ));
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.Orange);
                        break;
                    case Message.MessageType.Notice:
                        ret.AppendIconImage(AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 150, 255, 150)
                            ));
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.LimeGreen);
                        break;
                    case Message.MessageType.Hint:
                        ret.AppendIconImage(AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                            Avalonia.Media.Color.FromArgb(100, 150, 150, 255)
                            ));
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.LightCyan);
                        break;
                }
            }

            Root? root = iref.RootParsedDocument.Root;
            if (root == null) return null;

            NameSpace? space = iref.RootParsedDocument.Root;
            if (space == null) return null;

            {
                BuildingBlock? buildingBlock = root.GetBuildingBlock(iref);
                if(buildingBlock != null) space = buildingBlock.GetHierarchyNameSpace(iref);
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

            if (text.StartsWith("`"))
            {
                if (Macros.ContainsKey(text.Substring(1)))
                {
                    Macro macro = Macros[text.Substring(1)];
                    macro.AppendLabel(ret, Macros);
                }
                else if(iref.RootParsedDocument.Macros.ContainsKey(text.Substring(1)))
                {
                    Macro macro = iref.RootParsedDocument.Macros[text.Substring(1)];
                    macro.AppendLabel(ret, Macros);
                }
            }


            if (space != null)
            {
                INamedElement? namedElement = space.GetNamedElementUpward(text);
                if (namedElement != null) { 
                    if(namedElement is DataObject)
                    {
                        ((DataObject)namedElement).AppendLabel(ret);
                    } else if(namedElement is Function)
                    {
                        ((Function)namedElement).AppendLabel(ret) ;
                    } else if(namedElement is Task)
                    {
                        ((Task)namedElement).AppendLabel(ret);
                    }
                }

                //if (space.NamedElements.ContainsKey(text))
                //{
                //    DataObject? dataObject = space.NamedElements.GetDataObject(text);
                //    if (dataObject != null) dataObject.AppendLabel(ret);
                //}
                //else if (space.Parent != null)
                //{
                //    DataObject? dataObject = space.Parent.NamedElements.GetDataObject(text);
                //    if (dataObject != null) dataObject.AppendLabel(ret);
                if (Macros.ContainsKey(text))
                {
                    Macros[text].AppendLabel(ret, Macros);
                }
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
            if (Root == null) return null;
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            foreach (BuildingBlock module in Root.BuildingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (module.LastIndexReference == null) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                return module;
            }
            return null;
        }

        public NameSpace? GetNameSpace(IndexReference iref)
        {
            if(Root == null) return null;
            // get current buldingBlock
            NameSpace? space = null;
            foreach (BuildingBlock buildingBlock in Root.BuildingBlocks.Values)
            {
                if (iref.IsSmallerThan(buildingBlock.BeginIndexReference)) continue;
                if (buildingBlock.LastIndexReference == null) break;
                if (iref.IsGreaterThan(buildingBlock.LastIndexReference)) continue;
                space = buildingBlock.GetHierarchyNameSpace(iref);
                break;
            }
            return space;
        }


        private NameSpace? getSearchNameSpace(NameSpace? nameSpace,List<string> hier)
        {
            if (ProjectProperty == null) return null;
            if (nameSpace == null) return null;
            BuildingBlock? buildingBlock = nameSpace.BuildingBlock;
            if (buildingBlock == null) return null;

            if (hier.Count == 0) return nameSpace;

            if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(hier[0]))
            {
                IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)buildingBlock.NamedElements[hier[0]];
                NameSpace? bBlock = ProjectProperty.GetInstancedBuildingBlock(instantiation);

                if (instantiation is InterfaceInstance)
                {
                    InterfaceInstance? interfaceInstantiation = instantiation as InterfaceInstance;
                    if (interfaceInstantiation == null) throw new Exception();
                    //if(interfaceInstantiation.ModPortName != null)
                    //{
                    //    Interface? instance = bBlock as Interface;
                    //    if (instance == null) throw new Exception();
                    //    if (
                    //        instance.NamedElements.ContainsKey(interfaceInstantiation.ModPortName) &&
                    //        instance.NamedElements[interfaceInstantiation.ModPortName] is ModPort
                    //        )
                    //    {
                    //        bBlock = (ModPort)instance.NamedElements[interfaceInstantiation.ModPortName];
                    //    }
                    //}
                }

                hier.RemoveAt(0);
                return getSearchNameSpace(bBlock,hier);
            }
            else if(nameSpace.NamedElements.ContainsKey(hier[0]))
            {
                NameSpace? space = nameSpace.NamedElements[hier[0]] as NameSpace;
                hier.RemoveAt(0);
                return getSearchNameSpace(space, hier);
            }
            return nameSpace;
        }

        public List<AutocompleteItem>? GetAutoCompleteItems(List<string> hierWords,int index,int line,CodeEditor.CodeDocument document,string candidateWord)
        {
            if (ProjectProperty == null) return null;
            if (hierWords.Count == 0 && candidateWord == "") return null;
            if (hierWords.Count==0 && candidateWord.Length < 2) return null;

            List<AutocompleteItem> items = new List<AutocompleteItem>();

            if (hierWords.Count == 0)
            {
                // special autocomplete tool
                AppendSpecialAutoCompleteItems(items, candidateWord);
                // keywords
                AppendKeywordAutoCompleteItems(items, candidateWord);
            }

            // on Root and sont have ant Building Blocks
            if (Root == null || Root.BuildingBlocks == null)
            {
                return items;
            }

            // get reference of current position
            IndexReference iref = IndexReference.Create(this.IndexReference, index);

            // get current buldingBlock
            NameSpace? space = GetNameSpace(iref);

            // parse macro in hierarchy words
            for (int i = 0; i < hierWords.Count; i++)
            {
                if (!hierWords[i].StartsWith("`")) continue;

                string macroText = hierWords[i].Substring(1);
                if (!Macros.ContainsKey(macroText)) continue;
                Macro macro = Macros[macroText];
                if (macro.MacroText.Contains(' ')) continue;
                if (macro.MacroText.Contains('\t')) continue;

                string[] swapTexts = macro.MacroText.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                hierWords.RemoveAt(i);
                for (int j = swapTexts.Length - 1; j >= 0; j--)
                {
                    hierWords.Insert(i, swapTexts[j]);
                }
            }

            // system task & functions
            // return system task and function if the word starts with "$"
            if (candidateWord.StartsWith("$"))
            {
                items = new List<AutocompleteItem>();
                foreach (string key in ProjectProperty.SystemFunctions.Keys)
                {
                    if (!key.StartsWith(candidateWord)) continue;
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
                    if (!key.StartsWith(candidateWord)) continue;
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

            if(hierWords.Count ==0)
            {
                // append INamedElements
                if (space != null) appendAutoCompleteINamedElements(items, space, candidateWord);

            }
            else
            {
                if(space!=null) appendHierElement(hierWords, items, space, candidateWord,true);
            }

            return items;
        }

        private void appendHierElement(List<string> hierWords, List<AutocompleteItem> items, INamedElement element, string candidate, bool first)
        {
            if (hierWords.Count == 0)
            {
                foreach (INamedElement subElement in element.NamedElements.Values)
                {
                    if (!subElement.Name.StartsWith(candidate)) continue;
                    items.Add(
                        new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                            subElement.Name,
                            CodeDrawStyle.ColorIndex(subElement.ColorType),
                            Global.CodeDrawStyle.Color(subElement.ColorType),
                            "CodeEditor2/Assets/Icons/tag.svg"
                            )
                    );
                }
                return;
            }
            else
            {
                INamedElement? subElement = null;
                if (element.NamedElements.ContainsKey(hierWords[0]))
                {
                    subElement = element.NamedElements[hierWords[0]];
                } else if(element is NameSpace && first)
                {
                    subElement = ((NameSpace)element).GetNamedElementUpward(hierWords[0]);
                }

                if (subElement != null)
                {
                    hierWords.RemoveAt(0);
                    if (subElement is IBuildingBlockInstantiation)
                    {
                        IBuildingBlockInstantiation inst = (IBuildingBlockInstantiation)subElement;
                        BuildingBlock? buildingBlock = inst.GetInstancedBuildingBlock();
                        if (buildingBlock != null)
                        {
                            appendHierElement(hierWords, items, buildingBlock, candidate, false);
                            return;
                        }
                        else
                        {
                            return;
                        }
                    }

                    appendHierElement(hierWords, items, subElement, candidate, false);
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        private void appendAutoCompleteINamedElements(List<AutocompleteItem> items, NameSpace nameSpace,string candidate)
        {
            bool add = false;
            foreach (INamedElement element in nameSpace.NamedElements.Values)
            {
                if (!element.Name.StartsWith(candidate)) continue;
                items.Add(
                    element.CreateAutoCompleteItem()
                );
                add = true;
            }

            if (add) return;
            if (nameSpace.Parent == null) return;
            if (nameSpace == Root) return;
            if (nameSpace is BuildingBlocks.BuildingBlock) return;
            if (nameSpace.Parent == nameSpace) return;

            // upward search if no items found in the namespace
            if (nameSpace.Parent is NameSpace) appendAutoCompleteINamedElements(items, nameSpace.Parent, candidate);
        }

        public delegate void AppendKeywordAutoCompleteItemsDelegate(List<AutocompleteItem> items, string cantidate);
        public static AppendKeywordAutoCompleteItemsDelegate AppendKeywordAutoCompleteItems = appendKeywordAutoCompleteItems;

        private static void appendKeywordAutoCompleteItems(List<AutocompleteItem> items,string cantidate)
        {
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
                if (!keyword.StartsWith(cantidate)) continue;
                AutocompleteItem item = new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                    keyword,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                    "CodeEditor2/Assets/Icons/bookmark.svg"
                    );
                items.Add(item);
            }
        }
        public delegate void AppendSpecialAutoCompleteItemsDelegate(List<AutocompleteItem> items, string cantidate);
        [Newtonsoft.Json.JsonIgnore]
        public static AppendSpecialAutoCompleteItemsDelegate AppendSpecialAutoCompleteItems = appendSpecialAutoCompleteItems;
        private static void appendSpecialAutoCompleteItems(List<AutocompleteItem> items, string cantidate)
        {
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
                if (!item.Text.StartsWith(cantidate)) continue;
                items.Add(item);
            }
        }



        private INamedElement? getSearchElement(NameSpace nameSpace, List<string> hier)
        {
            if (nameSpace == null) return null;
            if (hier.Count == 0) return nameSpace;

            if (nameSpace.NamedElements.ContainsKey(hier[0]))
            {
                INamedElement element = nameSpace.NamedElements[hier[0]];
                hier.RemoveAt(0);
                getSearchSubElement(element, hier);
            }

            while (nameSpace.Parent != null)
            {
                nameSpace = nameSpace.Parent;
                INamedElement element = nameSpace.NamedElements[hier[0]];
                hier.RemoveAt(0);
                getSearchSubElement(element, hier);
            }
            return nameSpace;
        }

        private INamedElement? getSearchSubElement(INamedElement element, List<string> hier)
        {
            if (element == null) return null;
            if (hier.Count == 0) return element;

            if (element.NamedElements.ContainsKey(hier[0]))
            {
                INamedElement subElement = element.NamedElements[hier[0]];
                hier.RemoveAt(0);
                getSearchSubElement(element, hier);
            }
            return element;
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
            [Newtonsoft.Json.JsonIgnore]
            public Data.IVerilogRelatedFile? File
            {
                get
                {
                    Data.IVerilogRelatedFile? file;
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
