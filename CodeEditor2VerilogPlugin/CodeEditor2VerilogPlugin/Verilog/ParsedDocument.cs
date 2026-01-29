using Avalonia.Media.TextFormatting;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using ExCSS;
using pluginVerilog.CodeEditor;
using pluginVerilog.FileTypes;
using pluginVerilog.NavigatePanel;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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

            if (file is SystemVerilogFile || file is SystemVerilogHeaderFile) SystemVerilog = true;

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

        private ParsedDocument(Data.IVerilogRelatedFile file, string key, IndexReference? indexReference) : base((CodeEditor2.Data.TextFile)file, key, long.MaxValue, DocumentParser.ParseModeEnum.LoadParse)
        {
            if (file is SystemVerilogFile || file is SystemVerilogHeaderFile) SystemVerilog = true;

            System.Diagnostics.Debug.Print("## parsed " + key);
            if (file == null)
            {
                System.Diagnostics.Debug.Print("## parsed null " + key);
            }
        }

        private static CodeDocument getCodeDocument(Data.IVerilogRelatedFile file)
        {
            if (file == null) return null; // foe illeagal jon constructor 
            CodeDocument? codeDocument = file.CodeDocument as CodeDocument;

            codeDocument = file.CodeDocument as pluginVerilog.CodeEditor.CodeDocument;
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
        [JsonIgnore]
        public Data.IVerilogRelatedFile? File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }


        [JsonIgnore]
        public IndexReference IndexReference;

        [JsonIgnore]
        public List<int> Indexes = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;

        public bool RestrictBaseParse { get; set; } = false;
        public Root Root { set; get; }

        [JsonIgnore]
        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
        [JsonIgnore]
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();

        [JsonIgnore]
        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides = new Dictionary<string, Expressions.Expression>();
        [JsonIgnore]
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
        [JsonIgnore]
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

        [JsonIgnore]
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
            base.Dispose();
        }

        [JsonIgnore]
        public int ErrorCount = 0;
        [JsonIgnore]
        public int WarningCount = 0;
        [JsonIgnore]
        public int HintCount = 0;
        [JsonIgnore]
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
                    } else if(namedElement is DataObjects.Typedef)
                    {
                        ((DataObjects.Typedef)namedElement).AppendLabel(ret);
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

        //public void appendHierElement(List<string> hierWords, List<AutocompleteItem> items, INamedElement element, string candidate, bool first)
        //{
        //    if (hierWords.Count == 0)
        //    {
        //        foreach (INamedElement subElement in element.NamedElements.Values)
        //        {
        //            if (!subElement.Name.StartsWith(candidate)) continue;
        //            items.Add(
        //                new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
        //                    subElement.Name,
        //                    CodeDrawStyle.ColorIndex(subElement.ColorType),
        //                    Global.CodeDrawStyle.Color(subElement.ColorType),
        //                    "CodeEditor2/Assets/Icons/tag.svg"
        //                    )
        //            );
        //        }
        //        return;
        //    }
        //    else
        //    {
        //        INamedElement? subElement = null;
        //        if (element.NamedElements.ContainsKey(hierWords[0]))
        //        {
        //            subElement = element.NamedElements[hierWords[0]];
        //        } else if(element is NameSpace && first)
        //        {
        //            subElement = ((NameSpace)element).GetNamedElementUpward(hierWords[0]);
        //        }

        //        if (subElement != null)
        //        {
        //            hierWords.RemoveAt(0);
        //            if (subElement is IBuildingBlockInstantiation)
        //            {
        //                IBuildingBlockInstantiation inst = (IBuildingBlockInstantiation)subElement;
        //                BuildingBlock? buildingBlock = inst.GetInstancedBuildingBlock();
        //                if (buildingBlock != null)
        //                {
        //                    appendHierElement(hierWords, items, buildingBlock, candidate, false);
        //                    return;
        //                }
        //                else
        //                {
        //                    return;
        //                }
        //            } else if(subElement is Variable)
        //            {
        //                Variable variable = (Variable)subElement;
        //                if (variable.UnpackedArrays.Count != 0) return; // unpackedarray is not target of autocomplete
        //            }


        //            appendHierElement(hierWords, items, subElement, candidate, false);
        //            return;
        //        }
        //        else
        //        {
        //            return;
        //        }
        //    }
        //}

        public void appendAutoCompleteINamedElements(List<AutocompleteItem> items, NameSpace nameSpace,string candidate)
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

        public delegate void AppendKeywordAutoCompleteItemsDelegate(List<AutocompleteItem> items, string cantidate,bool systemVerilog);
        public static AppendKeywordAutoCompleteItemsDelegate AppendKeywordAutoCompleteItems = appendKeywordAutoCompleteItems;

        private static void appendKeywordAutoCompleteItems(List<AutocompleteItem> items,string cantidate, bool systemVerilog)
        {
            
            List<(string, int)> keywords = new List<(string, int)>
                {
                // Verilog
                ("always",1),
                ("and",1),
                ("assign",1),
                ("automatic",2),
                /*"begin",*/    
                ("case",1),
                ("casex",1),
                ("casez",1),

                ("deassign",1),
                ("default",1),
                ("defparam",1),
                ("design",1),
                ("disable",1),
                ("edge",1),
                ("else",1),
                ("end",1),
                ("endcase",1),
                ("endfunction",1),
                ("endgenerate",1),
                ("endmodule",1),
                ("endprimitive",1),
                ("endspecify",1),
                ("endtask",1),
                ("for",1),
                ("force",1),
                ("forever",1),
                ("fork",1),
                /*"function",1),*/
                /*"generate",1),*/
                ("genvar",1),
                ("if",1),
                ("incdir",2),
                ("include",1),
                ("initial",1),
                ("inout",1),
                ("input",1),
                ("integer",1),
                /*"interface",1),*/
                ("join",1),
                ("localparam",1),
                /*"module",1),*/  
                ("nand",1),
                ("negedge",1),
                ("nor",1),
                ("not",1),
                ("or",1),
                ("output",1),
                ("parameter",1),
                ("posedge",1),
                ("pulldown",1),    ("pullup",1),       ("real",1),
               ("realtime",1),    ("reg",1),         ("release",1),      ("repeat",1),
               ("signed",1),       /*"task",1),*/    ("time",1),         ("tri0",1),
               ("tri1",1),        ("trireg",1),      ("unsigned",1),    ("vectored",1),
               ("wait",1),        ("wand",1),        ("weak0",1),       ("weak1",1),
               ("while",1),       ("wire",1),        ("wor",1)};
            List<(string, int)> systemVerilogKeywords = new List<(string, int)> {
               // SystemVerilog
               ("accept_on",1),
                ("alias",1),
                ("always_comb",1),
                ("always_ff",1),
               ("always_latch",1),
                ("assert",1),
                ("assume",1),
                ("before",1),
               ("bind",1),
                ("bins",1),        ("binsof",1),      ("bit",1),
               ("break",1),       ("byte",1),        ("chandle",1),     ("checker",1),
               ("class",1),       ("clocking",1),    ("const",1),       ("constraint",1),
               ("context",1),     ("continue",1),    ("cover",1),       ("covergroup",1),
               ("coverpoint",1),  ("cross",1),       ("dist",1),        ("do",1),
               ("endchecker",1),  ("endclass",1),    ("endclocking",1), ("endgroup",1),
               ("endinterface",1),("endpackage",1),  ("endprogram",1),  ("endproperty",1),
               ("endsequence",1), ("enum",1),        ("eventually",1),  ("expect",1),
               ("export",1),      ("extends",1),     ("extern",1),      ("final",1),
               ("first_match",1), ("foreach",1),     ("forkjoin",1),    ("global",1),
               ("iff",1),         ("ignore_bins",1), ("illegal_bins",1),("implements",1),
               ("implies",1),     ("import",1),      ("inside",1),      ("int",1),
               ("interconnect",1),("interface",1),   ("intersect",1),   ("join_any",1),
               ("join_none",1),   ("let",1),         ("local",1),       ("logic",1),
               ("longint",1),     ("matches",1),     ("modport",1),     ("nettype",1),
               ("new",1),         ("nexttime",1),    ("null",1),        ("package",1),
               ("packed",1),      ("priority",1),    ("program",1),     ("property",1),
               ("protected",1),   ("pure",1),        ("rand",1),        ("randc",1),
               ("randcase",1),    ("randsequence",1),("ref",1),         ("reject_on",1),
               ("restrict",1),    ("return",1),      ("s_always",1),    ("s_eventually",1),
               ("s_nexttime",1),  ("s_until",1),     ("s_until_with",1),("sequence",1),
               ("shortint",1),    ("shortreal",1),   ("soft",1),        ("solve",1),
               ("static",1),      ("string",1),      ("strong",1),      ("struct",1),
               ("super",1),       ("sync_accept_on",1),  ("sync_reject_on",1),  ("tagged",1),
               ("this",1),        ("throughout",1),  ("timeprecision",1),   ("timeunit",1),
               ("type",1),        ("typedef",1),     ("union",1),       ("unique",1),
               ("unique0",1),     ("until",1),       ("until_with",1),  ("untyped",1),
               ("uwire",1),       ("var",1),         ("virtual",1),     ("void",1),
               ("wait_order",1),  ("weak",1),        ("wildcard",1),    ("with",1),
               ("within",1)
            };


            
            foreach ((string,int) keyword in keywords)
            {
                if (!keyword.Item1.StartsWith(cantidate)) continue;
                if (cantidate.Length < keyword.Item2) continue;
                AutocompleteItem item = new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                    keyword.Item1,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                    "CodeEditor2/Assets/Icons/bookmark.svg"
                    );
                items.Add(item);
            }

            if (systemVerilog)
            {
                foreach ((string, int) keyword in systemVerilogKeywords)
                {
                    if (!keyword.Item1.StartsWith(cantidate)) continue;
                    if (cantidate.Length < keyword.Item2) continue;
                    AutocompleteItem item = new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                        keyword.Item1,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword),
                        "CodeEditor2/Assets/Icons/bookmark.svg"
                        );
                    items.Add(item);
                }

            }

        }
        public delegate void AppendSpecialAutoCompleteItemsDelegate(List<AutocompleteItem> items, string cantidate);
        [JsonIgnore]
        public static AppendSpecialAutoCompleteItemsDelegate AppendSpecialAutoCompleteItems = appendSpecialAutoCompleteItems;
        private static void appendSpecialAutoCompleteItems(List<AutocompleteItem> items, string cantidate)
        {
            List<AutocompleteItem> specialItems = new List<AutocompleteItem>()
                {
                    new AutoComplete.BeginAutoCompleteItem(),
                    new AutoComplete.CaseAutocompleteItem(),
                    new AutoComplete.FunctionAutocompleteItem(),
                    new AutoComplete.GenerateAutoCompleteItem(),
                    new AutoComplete.ModuleAutocompleteItem(),
                    new AutoComplete.InterfaceAutocompleteItem(),
                    new AutoComplete.TaskAutocompleteItem(),
                    new AutoComplete.NonBlockingAssignmentAutoCompleteItem()
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
            [JsonIgnore]
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
