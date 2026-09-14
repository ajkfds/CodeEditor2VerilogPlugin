using Avalonia.Input;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using pluginVerilog.Parser;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Items.Generate;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace pluginVerilog.Verilog
{
    public record class CompletionContextResult
    {
        public CompletionContextResult(Data.IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index)
        {

            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return;
            candidateWord = "";

            this.item = item;
            this.parsedDocument = parsedDocument;
            this.index = index;

            if (!Data.VerilogCommon.AutoComplete.GetAutoCompleteTarget(item, parsedDocument, index, out nameSpace, out element, out candidateWord, out candidateStartIndex))
            {
                return;
            }

        }

        private Data.IVerilogRelatedFile item { get; init; } = null!;
        private Verilog.ParsedDocument parsedDocument { get; init; } = null!;
        int index { get; init; } = 0;
        private INamedElement? element;
        public string candidateWord = "";
        private int candidateStartIndex;
        private NameSpace? nameSpace;


        public List<ToolItem> autoCompleteItems = new List<ToolItem>();
        public List<AjkAvaloniaLibs.Controls.ColorLabel> popupLabels = new List<AjkAvaloniaLibs.Controls.ColorLabel>();

        public void Append()
        {
            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return;

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);

            Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter = (Data.VerilogCommon.AutoCompleteItem ac) =>
            {
                return true;
            };

            itemFilter = GetItemFilter();

            if (element != null)
            {   // has hier nameSpace cantidate
                foreach (INamedElement subElement in element.NamedElements.Values)
                {
                    if (candidateWord != "" && !subElement.Name.StartsWith(candidateWord)) continue;
                    if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace,
                        subElement.Name,
                        CodeDrawStyle.ColorIndex(subElement.ColorType),
                        Global.CodeDrawStyle.Color(subElement.ColorType),
                        "CodeEditor2/Assets/Icons/tag.svg"
                        );
                    if (itemFilter(acItem)) autoCompleteItems.Add(acItem);
                }
                return;
            }

            // hier cantidate : get current line namespace and region
            IndexReference iref = Verilog.IndexReference.Create(parsedDocument, codeDocument, lineStartIndex);
            Verilog.Items.IItem? currentItem = parsedDocument.GetItemAt(iref);

            bool onLineStart = false;
            while (true)
            {
                string lineString = codeDocument.CreateLineString(line);
                int inlinePosition = candidateStartIndex - lineStartIndex;
                if (inlinePosition < 0) break;
                if (inlinePosition > lineString.Length) break;

                string headString = lineString.Substring(0, inlinePosition);
                headString = headString.Replace("\t", " ");
                headString = headString.Replace(" ", "");
                if (headString.Length == 0) onLineStart = true;
                break;
            }

            // system task & functions
            // return system task and function if the word starts with "$"
            if (candidateWord.StartsWith("$") && parsedDocument.ProjectProperty != null)
            {
                foreach (string key in parsedDocument.ProjectProperty.SystemFunctions.Keys)
                {
                    if (!key.StartsWith(candidateWord)) continue;
                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        Data.VerilogCommon.AutoCompleteItem.CompleteType.Function,
                        key,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                        );
                    if (itemFilter(acItem)) autoCompleteItems.Add(acItem);
                }
                foreach (string key in parsedDocument.ProjectProperty.SystemTaskParsers.Keys)
                {
                    if (!key.StartsWith(candidateWord)) continue;
                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        Data.VerilogCommon.AutoCompleteItem.CompleteType.Task,
                        key,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                        );
                    if (itemFilter(acItem)) autoCompleteItems.Add(acItem);
                }
                return;
            }

            if (onLineStart && nameSpace != null && nameSpace.BuildingBlock is Module && candidateWord.Length > 1 && (currentItem is Module || currentItem is GenerateBlock))
            {
                CodeEditor2.Data.Project project = nameSpace.Project;
                ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();

                List<string> moduleNames = projectProperty.DefinitionNameSpace.GetNameList((x) => { return (x is Module); });
                foreach (string moduleName in moduleNames)
                {
                    autoCompleteItems.Add(new Verilog.Snippets.ModuleInstanceSnippet(moduleName));
                }
            }

            if (element == null)
            {
                if (nameSpace != null)
                {
                    // search upward
                    appendItemsUpward(nameSpace,itemFilter);
                }
                // keywords
                Data.VerilogCommon.AutoCompleteKeyword.AppendKeywordAutoCompleteItems(autoCompleteItems, candidateWord, candidateStartIndex, lineStartIndex, parsedDocument.SystemVerilog, itemFilter);
            }
            else // sub element
            {
                // append sub-element items
                foreach (INamedElement subElement in element.NamedElements.Values)
                {
                    if (candidateWord != "" && !subElement.Name.StartsWith(candidateWord)) continue;
                    if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

                    Data.VerilogCommon.AutoCompleteItem.CompleteType completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword;
                    if (subElement is NameSpace) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
                    if (subElement is Verilog.DataObjects.DataObject) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject;
                    if (completeType == Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword && System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }

                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        completeType,
                        subElement.Name,
                        CodeDrawStyle.ColorIndex(subElement.ColorType),
                        Global.CodeDrawStyle.Color(subElement.ColorType),
                        "CodeEditor2/Assets/Icons/tag.svg"
                        );
                    if (itemFilter(acItem)) autoCompleteItems.Add(acItem);
                }
            }

            return;
        }

        public Func<Data.VerilogCommon.AutoCompleteItem, bool> GetItemFilter()
        {
            Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter = (Data.VerilogCommon.AutoCompleteItem ac) =>
            {
                return true;
            };

            List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items = new List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>();

            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return null;

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);
            System.Diagnostics.Debug.Print("##partial parse start");
            int parseBlockIndex = 0;

            Verilog.Items.IItem? iitem = null;

            if (nameSpace == null) return itemFilter;
            if (item.CodeDocument == null) return itemFilter;
            parseBlockIndex = nameSpace.BeginIndexReference.RootIndex;

            IndexReference iref = IndexReference.Create(lineStartIndex, parsedDocument);
            iitem = parsedDocument.GetItemAt(iref);

            if (iitem != null && iitem.BeginIndexReference != null)
            {
                parseBlockIndex = iitem.BeginIndexReference.RootIndex;
            }
            if (candidateStartIndex - parseBlockIndex < 1) return itemFilter;

            string blockText = item.CodeDocument.CreateString(parseBlockIndex, candidateStartIndex - parseBlockIndex);
            pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(blockText);
            WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);


            System.Diagnostics.Debug.Print("##partial parse" + iitem.GetType().Name);
            if (iitem is Verilog.Items.ModuleInstantiation)
            {
                itemFilter = Verilog.Items.ModuleInstantiation.ParseAsync(word, nameSpace).GetAwaiter().GetResult();
            }

            return itemFilter;
        }

        public void appendItemsUpward(NameSpace nameSpace, Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter)
        {
            foreach (INamedElement subElement in nameSpace.NamedElements.Values)
            {
                if (!subElement.Name.StartsWith(candidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (autoCompleteItems.Find(x => x.Text == subElement.Name) != null) continue;   // reject duplicated elements

                Data.VerilogCommon.AutoCompleteItem.CompleteType completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword;
                if (subElement is NameSpace) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
                if (subElement is Verilog.DataObjects.DataObject || subElement is Verilog.DataObjects.Typedef) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject;
                if (subElement is Verilog.Items.ModuleInstantiation) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
                if (completeType == Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword && System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }

                Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                    completeType,
                    subElement.Name,
                    CodeDrawStyle.ColorIndex(subElement.ColorType),
                    Global.CodeDrawStyle.Color(subElement.ColorType),
                    "CodeEditor2/Assets/Icons/tag.svg"
                    );
                if (itemFilter(acItem)) autoCompleteItems.Add(acItem);
            }
            if (nameSpace.Parent != null)
            {
                appendItemsUpward(nameSpace.Parent,itemFilter);
            }
        }
    }
}
