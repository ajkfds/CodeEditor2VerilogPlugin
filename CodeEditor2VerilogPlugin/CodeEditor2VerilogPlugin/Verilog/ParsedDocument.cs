using Avalonia.Media.TextFormatting;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class ParsedDocument : CodeEditor2.CodeEditor.ParsedDocument
    {
        public ParsedDocument(Data.IVerilogRelatedFile file, IndexReference indexReference, CodeEditor2.CodeEditor.DocumentParser.ParseModeEnum parseMode) : base(file as CodeEditor2.Data.TextFile,file.CodeDocument.Version,parseMode)
        {
            fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
            if(indexReference == null)
            {
                IndexReference = IndexReference.Create(this);
            }
            else
            {
                IndexReference = indexReference;
            }
        }

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

        public List<int> Indexs = new List<int>();

        public bool SystemVerilog = false;
        public bool Instance = false;
//        public Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        public Root Root = null;

        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();

        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides;
        public string TargetBuldingBlockName = null;

        public Dictionary<int, ParsedDocument> ParsedDocumentIndexDictionary = new Dictionary<int, ParsedDocument>();

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
                    Data.VerilogFile verilogFile = file as Data.VerilogFile;
                    foreach (BuildingBlock module in Root.BuldingBlocks.Values)
                    {
                        verilogFile.ProjectProperty.RemoveBuildingBlock(module.Name);
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


        public CodeEditor2.CodeEditor.PopupItem GetPopupItem(int index, string text)
        {
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            if (iref.RootParsedDocument == null) return null;


            var ret = new CodeEditor2.CodeEditor.PopupItem();

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

            NameSpace space = iref.RootParsedDocument.Root;

            foreach (BuildingBlock module in iref.RootParsedDocument.Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                space = module.GetHierarchyNameSpace(index);
                break;
            }

            int count = ret.ItemCount;
            foreach (IInstantiation instantiation in space.BuildingBlock.Instantiations.Values)
            {
                if (iref.IsSmallerThan(instantiation.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(instantiation.LastIndexReference)) continue;
                instantiation.AppendLabel(iref,ret);
                break;
            }
            if (ret.ItemCount != count) return ret;

            if (space.DataObjects.ContainsKey(text))
            {
                space.DataObjects[text].AppendLabel(ret);
                //                ret.Add(space.DataObjects[text]   new Popup.VariablePopup(space.DataObjects[text]));
            }

            {
                DataObjects.Constants.Constants param = space.GetConstants(text);
                if (param != null)
                {
                    param.AppendLabel(ret);
//                    ret.Add(new Popup.ParameterPopup(param));
                }
            }

            if (text.StartsWith("`") && iref.RootParsedDocument.Macros.ContainsKey(text.Substring(1)))
            {
//                ret.Add(new Popup.MacroPopup(text.Substring(1), iref.RootParsedDocument.Macros[text.Substring(1)].MacroText));
            }
            if (space.BuildingBlock.Functions.ContainsKey(text))
            {
//                ret.Add(new Popup.FunctionPopup(space.BuildingBlock.Functions[text]));
            }
            if (space.BuildingBlock.Tasks.ContainsKey(text))
            {
//                ret.Add(new Popup.TaskPopup(space.BuildingBlock.Tasks[text]));
            }
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


        private static List<CodeEditor2.CodeEditor.AutocompleteItem> verilogKeywords = new List<CodeEditor2.CodeEditor.AutocompleteItem>()
        {
            new CodeEditor2.CodeEditor.AutocompleteItem("always",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("and",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("assign",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("automatic",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),



            new AutoComplete.BeginAutoCompleteItem("begin",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("case",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("casex",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("casez",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("deassign",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("default",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("defparam",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("design",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("disable",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("edge",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("else",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("end",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endcase",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endfunction",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endgenerate",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endmodule",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endspecify",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endtask",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endprimitive",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("for",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("force",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("forever",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("fork",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new AutoComplete.FunctionAutocompleteItem("function",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new AutoComplete.GenerateAutoCompleteItem("generate",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("genvar",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("if",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("incdir",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("include",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("initial",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("inout",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("input",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("integer",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("join",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("localparam",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new AutoComplete.ModuleAutocompleteItem("module",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new AutoComplete.InterfaceAutocompleteItem("interface",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("nand",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("negedge",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("nor",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("not",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("or",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("output",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("parameter",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("posedge",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("pulldown",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("pullup",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("real",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("realtime",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("reg",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("release",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("repeat",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("signed",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("time",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new AutoComplete.TaskAutocompleteItem("task",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("tri0",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("tri1",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("trireg",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("unsigned",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("vectored",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("wait",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("weak0",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("weak1",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("while",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            new CodeEditor2.CodeEditor.AutocompleteItem("wand",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("wire",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("wor",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),

            // SystemVerilog
            new CodeEditor2.CodeEditor.AutocompleteItem("accept_on",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("alias",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("always_comb",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("always_ff",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("always_latch",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("assert",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("assume",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("before",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("bind",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("bins",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("binsof",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("bit",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("break",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("byte",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("chandle",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("checker",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("class",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("clocking",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("const",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("constraint",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("context",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("continue",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("cover",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("covergroup",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("coverpoint",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("cross",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("dist",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("do",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endchecker",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endclass",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endclocking",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endgroup",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endinterface",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endpackage",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endprogram",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endproperty",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("endsequence",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("enum",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("eventually",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("expect",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("export",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("extends",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("extern",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("final",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("first_match",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("foreach",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("forkjoin",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("global",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("iff",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("ignore_bins",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("illegal_bins",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("implements",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("implies",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("import",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("inside",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("int",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("interconnect",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("interface",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("intersect",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("join_any",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("join_none",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("let",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("local",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("logic",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("longint",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("matches",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("modport",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("nettype",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("new",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("nexttime",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("null",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("package",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("packed",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("priority",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("program",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("property",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("protected",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("pure",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("rand",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("randc",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("randcase",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("randsequence",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("ref",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("reject_on",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("restrict",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("return",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("s_always",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("s_eventually",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("s_nexttime",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("s_until",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("s_until_with",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("sequence",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("shortint",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("shortreal",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("soft",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("solve",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("static",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("string",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("strong",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("struct",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("super",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("sync_accept_on",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("sync_reject_on",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("tagged",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("this",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("throughout",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("timeprecision",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("timeunit",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("type",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("typedef",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("union",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("unique",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("unique0",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("until",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("until_with",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("untyped",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("uwire",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("var",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("virtual",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("void",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("wait_order",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("weak",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("wildcard",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("with",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
            new CodeEditor2.CodeEditor.AutocompleteItem("within",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),



            new AutoComplete.NonBlockingAssignmentAutoCompleteItem("<=",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Normal), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
        };

        private NameSpace? getSearchNameSpace(NameSpace nameSpace,List<string> hier)
        {
            BuildingBlock? buildingBlock = nameSpace.BuildingBlock;
//            IBuildingBlockWithModuleInstance? buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;
            if (buildingBlock == null) return null;

            if(nameSpace == null) return null;
            if (hier.Count == 0) return nameSpace;

            if (buildingBlock.Instantiations.ContainsKey(hier[0]))
            {
                IInstantiation inst = buildingBlock.Instantiations[hier[0]];
                NameSpace bBlock = ProjectProperty.GetInstancedBuildingBlock(inst);

                if (inst is InterfaceInstantiation)
                {
                    InterfaceInstantiation? interfaceInstantiation = inst as InterfaceInstantiation;
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
            else if(nameSpace.NameSpaces.ContainsKey(hier[0]))
            {
                NameSpace space = nameSpace.NameSpaces[hier[0]];
                hier.RemoveAt(0);
                return getSearchNameSpace(space, hier);
            }
            return nameSpace;
        }

        public List<CodeEditor2.CodeEditor.AutocompleteItem> GetAutoCompleteItems(List<string> hierWords,int index,int line,CodeEditor.CodeDocument document,string cantidateWord)
        {


            List<CodeEditor2.CodeEditor.AutocompleteItem> items = null;

            if (Root == null || Root.BuldingBlocks == null)
            {
                items = verilogKeywords.ToList();
                return items;
            }

            IndexReference iref = IndexReference.Create(this.IndexReference, index);

            // get current nameSpace
            NameSpace space = null;
            foreach (BuildingBlock module in Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                space = module.GetHierarchyNameSpace(index);
                break;
            }

            if (space == null) // external module/class/program
            {
                items = verilogKeywords.ToList();
                int headIndex;
                int length;
                document.GetWord(index, out headIndex, out length);
                return items;
            }

            //bool endWithDot;
            //List<string> words = document.GetHierWords(index, out endWithDot);
            //if (words.Count == 0)
            //{
            //    return new List<CodeEditor2.CodeEditor.AutocompleteItem>();
            //}

            if (hierWords.Count == 0 && cantidateWord.StartsWith("$"))
            {
                items = new List<CodeEditor2.CodeEditor.AutocompleteItem>();
                foreach (string key in ProjectProperty.SystemFunctions.Keys)
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.AutocompleteItem(key, CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword))
                    );
                }
                foreach (string key in ProjectProperty.SystemTaskParsers.Keys)
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.AutocompleteItem(key, CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword))
                    );
                }
                return items;
            }

            if (hierWords.Count == 0)
            {
                items = verilogKeywords.ToList();
            }
            else
            {
                items = new List<CodeEditor2.CodeEditor.AutocompleteItem>();
            }

            if (hierWords.Count == 0)
            {
                List<string> objectList = ProjectProperty.GetObjectsNameList();
                foreach(var name in objectList) 
                {
                    items.Add(
                        new CodeEditor2.CodeEditor.AutocompleteItem(name, CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Identifier), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier))
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

            // get target autocomplete item
            NameSpace? target = getSearchNameSpace(space, hierWords);
            if(target != null) target.AppendAutoCompleteItem(items);

            return items;
        }


        private CodeEditor2.CodeEditor.AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
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
