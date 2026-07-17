using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using pluginVerilog.CodeEditor;
using pluginVerilog.FileTypes;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog
{
    public class ParsedDocument : CodeEditor2.CodeEditor.ParsedDocument
    {
        public ParsedDocument(Data.IVerilogRelatedFile file, string key, IndexReference? indexReference, DocumentParser.ParseModeEnum parseMode) : base((CodeEditor2.Data.TextFile)file, key, getCodeDocument(file).Version, parseMode)
        {
            CodeDocument? document = file.CodeDocument as CodeDocument;
            if (document == null) throw new Exception();
            codeDocument = document;

            if (file is SystemVerilogFile || file is SystemVerilogHeaderFile) SystemVerilog = true;

            fileRef = new WeakReference<Data.IVerilogRelatedFile>(file);
            if (indexReference == null)
            {
                IndexReference = IndexReference.Create(this, document, 0);
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
            if (codeDocument == null)
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
        public Root Root { set; get; } = null!;

        [JsonIgnore]
        public Dictionary<string, Data.VerilogHeaderInstance> IncludeFiles = new Dictionary<string, Data.VerilogHeaderInstance>();
        [JsonIgnore]
        public Dictionary<string, Macro> Macros = new Dictionary<string, Macro>();
public List<string> ImportedPackages = new List<string>();

        [JsonIgnore]
        public Dictionary<string, Verilog.Expressions.Expression> ParameterOverrides = new Dictionary<string, Expressions.Expression>();
        [JsonIgnore]
        public string? TargetBuildingBlockName = null;

        public List<string> ExternalRefrenceModules = new List<string>();
        public List<string> UnfoundModules = new List<string>();

        // for IndexReference

        private bool reparseRequested = true;
        public bool ReparseRequested
        {
            get
            {
                return reparseRequested;
            }
            set
            {
                reparseRequested = value;
            }
        }

        public void ReloadIncludeFiles()
        {
            foreach (var includeFile in IncludeFiles.Values)
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

        public void AddError(int index, int length, string message)
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
                        ret.AppendText(message.Text + "\n", Avalonia.Media.Colors.Pink);
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

            if (File == null || !getPopupTarget(index, out NameSpace? nameSpace, out INamedElement? element))
            {
                return null;
            }

            if (element != null)
            {
                if (element is DataObject)
                {
                    ((DataObject)element).AppendLabel(ret);
                }
                else if (element is Function)
                {
                    ((Function)element).AppendLabel(ret);
                }
                else if (element is Task)
                {
                    ((Task)element).AppendLabel(ret);
                }
                else if (element is DataObjects.Typedef)
                {
                    ((DataObjects.Typedef)element).AppendLabel(ret);
                }
            }

            if (Macros.ContainsKey(text))
            {
                Macros[text].AppendLabel(ret, Macros);
            }
            return ret;



            Root? root = iref.RootParsedDocument.Root;
            if (root == null) return null;

            NameSpace? space = iref.RootParsedDocument.Root;
            if (space == null) return null;

            {
                BuildingBlock? buildingBlock = root.GetBuildingBlock(iref);
                if (buildingBlock != null) space = buildingBlock.GetHierarchyNameSpace(iref);
            }

            int count = ret.ItemCount;

            if (space != null)
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
                else if (iref.RootParsedDocument.Macros.ContainsKey(text.Substring(1)))
                {
                    Macro macro = iref.RootParsedDocument.Macros[text.Substring(1)];
                    macro.AppendLabel(ret, Macros);
                }
            }


            if (space != null)
            {
                INamedElement? namedElement = space.GetNamedElementUpward(text);
                if (namedElement != null)
                {
                    if (namedElement is DataObject)
                    {
                        ((DataObject)namedElement).AppendLabel(ret);
                    }
                    else if (namedElement is Function)
                    {
                        ((Function)namedElement).AppendLabel(ret);
                    }
                    else if (namedElement is Task)
                    {
                        ((Task)namedElement).AppendLabel(ret);
                    }
                    else if (namedElement is DataObjects.Typedef)
                    {
                        ((DataObjects.Typedef)namedElement).AppendLabel(ret);
                    }
                }

                if (Macros.ContainsKey(text))
                {
                    Macros[text].AppendLabel(ret, Macros);
                }
            }


            return ret;
        }

        private bool getPopupTarget(int index, out NameSpace? nameSpace, out INamedElement? element)
        {
            nameSpace = null;
            element = null;

            int line = CodeDocument.GetLineAt(index);
            int lineStartIndex = CodeDocument.GetLineStartIndex(line);
            CodeDocument.GetWord(index, out int wordHeadIndex, out int wordLength);

            string lineText = CodeDocument.CreateLineString(line);
            int lineLength = wordHeadIndex + wordLength - lineStartIndex;
            if (lineLength > lineText.Length) lineLength = lineText.Length;
            lineText = lineText.Substring(0, lineLength);

            { // pre carlet char check
                if (index != 0)
                {
                    char preChar = CodeDocument.GetCharAt(index - 1);
                    if (preChar == ' ') return false;
                    if (preChar == '\t') return false;
                }
            }

            //{ // remove comment start to activate auto complete in comments
            //    int commentIndex = lineText.LastIndexOf("/*");
            //    if (commentIndex > 0)
            //    {
            //        commentIndex = commentIndex + 2;
            //        lineText = lineText.Substring(commentIndex);
            //        candidateStartIndex += commentIndex;
            //    }
            //}
            //{ // remove comment start to activate auto complete in comments
            //    int commentIndex = lineText.LastIndexOf("//");
            //    if (commentIndex > 0)
            //    {
            //        commentIndex = commentIndex + 2;
            //        lineText = lineText.Substring(commentIndex);
            //        candidateStartIndex += commentIndex;
            //    }
            //}


            int blockStartIndex = 0;
            int blockEndIndex = 0;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(lineText);
                WordScanner word = new WordScanner(document, this, SystemVerilog);
                word.SupressMessage = true;

                List<(string, int)> words = new List<(string, int)>();
                while (!word.Eof)
                {
                    if (General.IsIdentifier(word.Text))
                    {
                        words.Add((word.Text, word.RootIndex));
                        word.MoveNext();
                        if (word.Text == "::" || word.Text == "->" || word.Text == ".")
                        {
                            words.Add((word.Text, word.RootIndex));
                            word.MoveNext();
                        }
                        else
                        {
                            if (word.Eof) break;
                            words.Clear();
                        }
                    }
                    else
                    {   // illegal text
                        words.Add((word.Text, word.RootIndex));
                        word.MoveNext();
                        if (word.Eof) break;
                        words.Clear();
                    }
                }

                if (words.Count == 0) return false;

                blockStartIndex = words[0].Item2;
                blockEndIndex = words.Last().Item2 + words.Last().Item1.Length;

            }

            // get namespace
            {
                // namespace must get from linestart index, because current index cann't match last parsed document
                IndexReference iref = IndexReference.Create(IndexReference, lineStartIndex);
                nameSpace = GetNameSpace(iref);
            }

            if(lineText.Length<blockStartIndex) return false;
            if(lineText.Length<blockEndIndex) return false;

            string elementText = lineText.Substring(blockStartIndex, blockEndIndex - blockStartIndex);
            element = null;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(elementText);
                WordScanner word = new WordScanner(document, this, SystemVerilog);
                word.SupressMessage = true;

                Verilog.Expressions.Expression? expression = null;
                while (!word.Eof)
                {
                    if (nameSpace != null) expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null) word.MoveNext();
                }
                if (expression is Verilog.Expressions.DataObjectReference)
                {
                    Verilog.Expressions.DataObjectReference dataObjectReference = (Verilog.Expressions.DataObjectReference)expression;
                    element = dataObjectReference.TargetDataObject;
                }
                else if (expression is Verilog.Expressions.NameSpaceReference)
                {
                    NameSpace targetNameSpace = ((Verilog.Expressions.NameSpaceReference)expression).NameSpace;
                    element = targetNameSpace;
                }
            }
            return true;
        }





        public BuildingBlock? GetBuildingBlockAt(int index)
        {
            if (Root == null) return null;
            IndexReference iref = IndexReference.Create(this.IndexReference, index);
            foreach (var moduleKvp in Root.BuildingBlocks)
            {
                if (iref.IsSmallerThan(moduleKvp.Value.BeginIndexReference)) continue;
                if (moduleKvp.Value.LastIndexReference == null) continue;
                if (iref.IsGreaterThan(moduleKvp.Value.LastIndexReference)) continue;
                return moduleKvp.Value;
            }
            return null;
        }


        public bool TryGetRegion(IndexReference iref, out NameSpace? nameSpace, out IRegion? item)
        {
            item = null;
            nameSpace = GetNameSpace(iref);
            if (nameSpace == null) return false;

            IRegion? find = null;
            foreach (IRegion subItem in nameSpace.Regions)
            {
                if (subItem.BeginIndexReference == null) continue;
                if (subItem.LastIndexReference == null) continue;
                if (iref.IsSmallerThan(subItem.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(subItem.LastIndexReference)) continue;
                find = subItem;
                break;
            }
            if (find != null)
            {
                item = find;
                return true;
            }
            else
            {
                return false;
            }
        }



        public NameSpace? GetNameSpace(IndexReference iref)
        {
            if (Root == null) return null;
            // get current buldingBlock
            NameSpace? space = null;
            foreach (var buildingBlockKvp in Root.BuildingBlocks)
            {
                if (iref.IsSmallerThan(buildingBlockKvp.Value.BeginIndexReference)) continue;
                if (buildingBlockKvp.Value.LastIndexReference == null) break;
                if (iref.IsGreaterThan(buildingBlockKvp.Value.LastIndexReference)) continue;
                space = buildingBlockKvp.Value.GetHierarchyNameSpace(iref);
                break;
            }
            return space;
        }


        private NameSpace? getSearchNameSpace(NameSpace? nameSpace, List<string> hier)
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
                }

                hier.RemoveAt(0);
                return getSearchNameSpace(bBlock, hier);
            }
            else if (nameSpace.NamedElements.ContainsKey(hier[0]))
            {
                NameSpace? space = nameSpace.NamedElements[hier[0]] as NameSpace;
                hier.RemoveAt(0);
                return getSearchNameSpace(space, hier);
            }
            return nameSpace;
        }


        private void appendAutoCompleteINamedElements(List<AutocompleteItem> items, NameSpace nameSpace, string candidate)
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
            public Message(Data.IVerilogRelatedFile file, string text, MessageType type, int index, int lineNo, int length, CodeEditor2.Data.Project project)
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
