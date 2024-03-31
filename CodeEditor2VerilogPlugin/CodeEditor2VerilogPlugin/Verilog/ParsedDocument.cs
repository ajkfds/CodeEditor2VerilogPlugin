using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
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
                        verilogFile.ProjectProperty.RemoveModule(module.Name, verilogFile);
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
                        ret.AppendText(message.Text, Avalonia.Media.Colors.Pink);
                        break;
                    case Message.MessageType.Warning:
                        ret.AppendText(message.Text, Avalonia.Media.Colors.Orange);
                        break;
                    case Message.MessageType.Notice:
                        ret.AppendText(message.Text, Avalonia.Media.Colors.LimeGreen);
                        break;
                    case Message.MessageType.Hint:
                        ret.AppendText(message.Text, Avalonia.Media.Colors.LightCyan);
                        break;
                }
            }

            NameSpace space = iref.RootParsedDocument.Root;

            foreach (BuildingBlock module in iref.RootParsedDocument.Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                space = module.GetHierNameSpace(index);
                break;
            }

            if (text.StartsWith(".") && space is IModuleOrGeneratedBlock)
            {
                IModuleOrGeneratedBlock block = space as IModuleOrGeneratedBlock;
                ModuleItems.ModuleInstantiation inst = null;
                foreach (ModuleItems.ModuleInstantiation i in block.ModuleInstantiations.Values)
                {
                    if (iref.IsSmallerThan(i.BeginIndexReference)) continue;
                    if (iref.IsGreaterThan(i.LastIndexReference)) continue;
                    inst = i;
                    break;
                }
                if (inst != null)
                {
                    string portName = text.Substring(1);
                    Module originalModule = ProjectProperty.GetBuildingBlock(inst.SourceName) as Module;
                    if (originalModule == null) return ret;
                    if (!originalModule.Ports.ContainsKey(portName)) return ret;
                    Verilog.DataObjects.Port port = originalModule.Ports[portName];
                    ret.AppendLabel(port.GetLabel());
                }
            }

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

        public BuildingBlock GetBuidingBlockAt(int index)
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

            new AutoComplete.NonBlockingAssignmentAutoCompleteItem("<=",CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Normal), Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)),
        };

        private NameSpace getSearchNameSpace(NameSpace nameSpace,List<string> hier)
        {
            IBuildingBlockWithModuleInstance buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;
            if (buildingBlock == null) System.Diagnostics.Debugger.Break();

            if(nameSpace == null) return null;
            if (hier.Count == 0) return nameSpace;

            if (buildingBlock.Instantiations.ContainsKey(hier[0]))
            {
                IInstantiation inst = buildingBlock.Instantiations[hier[0]];
                BuildingBlock module = ProjectProperty.GetInstancedBuildingBlock(inst);
                hier.RemoveAt(0);
                return getSearchNameSpace(module,hier);
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
            IndexReference iref = IndexReference.Create(this.IndexReference, index);

            List<CodeEditor2.CodeEditor.AutocompleteItem> items = null;

            // get current nameSpace
            NameSpace space = null;
            foreach (BuildingBlock module in Root.BuldingBlocks.Values)
            {
                if (iref.IsSmallerThan(module.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(module.LastIndexReference)) continue;
                space = module.GetHierNameSpace(index);
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
            //List<string> words = document.GetHierWords(index,out endWithDot);
            //if(words.Count == 0)
            //{
            //    return new List<CodeEditor2.CodeEditor.AutocompleteItem>();
            //}

            if(hierWords.Count == 0 && cantidateWord.StartsWith("$"))
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


            // parse macro in hier words
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
            NameSpace target = getSearchNameSpace(space, hierWords);
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
