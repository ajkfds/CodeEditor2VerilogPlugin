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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace pluginVerilog.Verilog
{
    public class CompletionContext : CodeEditor2.CodeEditor.CodeComplete.CompletionContext
    {
        public CompletionContext(Data.IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index)
        {

            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return;
            CandidateWord = "";

            this.item = item;
            this.parsedDocument = parsedDocument;
            this.index = index;

            if (!Data.VerilogCommon.AutoComplete.GetAutoCompleteTarget(item, parsedDocument, index, out NameSpace, out NamedElement, out CandidateWord, out CandidateStartIndex))
            {
                return;
            }

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);

            onLineStart = false;
            while (true)
            {
                string lineString = codeDocument.CreateLineString(line);
                int inlinePosition = CandidateStartIndex - lineStartIndex;
                if (inlinePosition < 0) break;
                if (inlinePosition > lineString.Length) break;

                string headString = lineString.Substring(0, inlinePosition);
                headString = headString.Replace("\t", " ");
                headString = headString.Replace(" ", "");
                if (headString.Length == 0) onLineStart = true;
                break;
            }

            // partial parse
            int parseBlockIndex = 0;

            if (NameSpace == null) return;
            if (item.CodeDocument == null) return;
            parseBlockIndex = NameSpace.BeginIndexReference.RootIndex;

            IndexReference iref = IndexReference.Create(lineStartIndex, parsedDocument);
            iitem = parsedDocument.GetDocumentRegionAt(iref);

            if (iitem != null && iitem.BeginIndexReference != null)
            {
                parseBlockIndex = iitem.BeginIndexReference.RootIndex;
            }
            if (CandidateStartIndex - parseBlockIndex < 1) return;

            string blockText = item.CodeDocument.CreateString(parseBlockIndex, CandidateStartIndex - parseBlockIndex);
            pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(blockText);
            
            WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);
            if (iitem is Verilog.Items.ModuleInstantiation)
            {
                #pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                Verilog.Items.ModuleInstantiation.ParseAsync(word, NameSpace, this).GetAwaiter().GetResult();
                #pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }else if(iitem is Module module)
            {
                Module.ParseCreateAsync(word, module.ParameterOverrides, module.Attribute, module.BuildingBlock, item, false, this).GetAwaiter().GetResult();
            }
        }

        private Data.IVerilogRelatedFile item { get; init; } = null!;
        private Verilog.ParsedDocument parsedDocument { get; init; } = null!;
        int index { get; init; } = 0;
        private INamedElement? NamedElement;
        private NameSpace? NameSpace;
        private Verilog.Items.IDocumentRegeion? iitem = null;
        private int line = 0;
        private int lineStartIndex = 0;
        private bool onLineStart = false;

        public void AppendAll()
        {
            appendNamedElements((acItem) => true);
            appendKeyword((acItem) => true);
            appendSystemFunction((acItem) => true);
            appendSystemTask((acItem) => true);
        }
        public void AppendExpression()
        {
            appendNamedElements((acItem) => {
                if (acItem.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject) return true;
                if (acItem.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.Function) return true;
                if (acItem.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace) return true;
                return false;
            });
            appendSystemFunction((acItem) => true);
        }
        public void AppendKeywords(List<string> keywords)
        {
            appendKeyword((acItem) =>
            {
                if (keywords.Contains(acItem.Text)) return true;
                return false;
            });
        }

        //private void AppendNameSpace(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        //{
        //    if (NamedElement == null) return;

        //    foreach (INamedElement subElement in NamedElement.NamedElements.Values)
        //    {
        //        if (CandidateWord != "" && !subElement.Name.StartsWith(CandidateWord)) continue;
        //        if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

        //        Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
        //            Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace,
        //            subElement.Name,
        //            CodeDrawStyle.ColorIndex(subElement.ColorType),
        //            Global.CodeDrawStyle.Color(subElement.ColorType),
        //            "CodeEditor2/Assets/Icons/tag.svg"
        //            );
        //        if (filter(acItem)) AutoCompleteItems.Add(acItem);
        //    }
        //}
        private void appendSystemTask(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            if (!CandidateWord.StartsWith("$")) return;
            if (parsedDocument.ProjectProperty == null) return;

            foreach (string key in parsedDocument.ProjectProperty.SystemTaskParsers.Keys)
            {
                if (!key.StartsWith(CandidateWord)) continue;
                Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                    Data.VerilogCommon.AutoCompleteItem.CompleteType.Task,
                    key,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                    );
                if (filter(acItem)) AutoCompleteItems.Add(acItem);
            }
        }
        private void appendSystemFunction(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            if (!CandidateWord.StartsWith("$")) return;
            if (parsedDocument.ProjectProperty == null) return;

            foreach (string key in parsedDocument.ProjectProperty.SystemFunctions.Keys)
            {
                if (!key.StartsWith(CandidateWord)) continue;
                Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                    Data.VerilogCommon.AutoCompleteItem.CompleteType.Function,
                    key,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                    );
                if (filter(acItem)) AutoCompleteItems.Add(acItem);
            }
        }
        public void AppendModuleInstanceSnippets(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            if (onLineStart && NameSpace != null && NameSpace.BuildingBlock is Module && CandidateWord.Length > 1 && (iitem is Module ||iitem is GenerateBlock))
            {
                CodeEditor2.Data.Project project = NameSpace.Project;
                ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();

                List<string> moduleNames = projectProperty.DefinitionNameSpace.GetNameList((x) => { return (x is Module); });
                foreach (string moduleName in moduleNames)
                {
                    AutoCompleteItems.Add(new Verilog.Snippets.ModuleInstanceSnippet(moduleName));
                }
            }

        }
        private void appendKeyword(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            // keywords
            Data.VerilogCommon.AutoCompleteKeyword.AppendKeywordAutoCompleteItems(
                AutoCompleteItems, CandidateWord, CandidateStartIndex, lineStartIndex, parsedDocument.SystemVerilog, filter
                );
        }

        private void appendNamedElements(Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            if (NamedElement == null)
            {
                if (NameSpace != null)
                {
                    // search upward
                    appendItemsUpward(NameSpace, filter);
                }
            }
            else // sub element
            {
                // append sub-element items
                foreach (INamedElement subElement in NamedElement.NamedElements.Values)
                {
                    if (CandidateWord != "" && !subElement.Name.StartsWith(CandidateWord)) continue;
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
                    if (filter(acItem)) AutoCompleteItems.Add(acItem);
                }
            }

            return;
        }

        public void appendItemsUpward(NameSpace nameSpace, Func<Data.VerilogCommon.AutoCompleteItem, bool> filter)
        {
            foreach (INamedElement subElement in nameSpace.NamedElements.Values)
            {
                if (!subElement.Name.StartsWith(CandidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (AutoCompleteItems.Find(x => x.Text == subElement.Name) != null) continue;   // reject duplicated elements

                Data.VerilogCommon.AutoCompleteItem.CompleteType completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword;
                if (subElement is NameSpace) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
                if (subElement is Verilog.DataObjects.DataObject || subElement is Verilog.DataObjects.Typedef) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject;
                if (subElement is Verilog.Items.ModuleInstantiation) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
                if (subElement is Verilog.Items.UdpInstantiation) completeType = Data.VerilogCommon.AutoCompleteItem.CompleteType.NameSpace;
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
                if (filter(acItem)) AutoCompleteItems.Add(acItem);
            }
            if (nameSpace.Parent != null)
            {
                appendItemsUpward(nameSpace.Parent, filter);
            }

        }
    }
}
