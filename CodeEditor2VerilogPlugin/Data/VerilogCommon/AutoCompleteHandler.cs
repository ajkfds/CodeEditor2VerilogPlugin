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
using System.Threading;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class AutoComplete
    {
        public static void AfterKeyDown(IVerilogRelatedFile item, Avalonia.Input.KeyEventArgs e)
        {
            if (item.VerilogParsedDocument == null) return;
            switch (e.Key)
            {
                case Key.Return:
                    applyAutoInput(item);
                    break;
                case Key.Space:
                    break;
                default:
                    break;
            }
        }

        public static void AfterKeyPressed(IVerilogRelatedFile item, Avalonia.Input.KeyEventArgs e)
        {
            if (item.VerilogParsedDocument == null) return;
        }

        public static void BeforeKeyPressed(IVerilogRelatedFile item, Avalonia.Input.KeyEventArgs e)
        {
        }

        public static void BeforeKeyDown(IVerilogRelatedFile item, Avalonia.Input.KeyEventArgs e)
        {
        }


        public static PopupItem? GetPopupItem(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, ulong version, int index)
        {
            if (parsedDocument == null) return null;
            if (parsedDocument.Version != version) return null;
            if (item.CodeDocument == null) return null;

            int headIndex, length;
            item.CodeDocument.GetWord(index, out headIndex, out length);
            string text = item.CodeDocument.CreateString(headIndex, length);
            return parsedDocument.GetPopupItem(index, text);
        }


        public static List<ToolItem> GetToolItems(IVerilogRelatedFile item, int index)
        {
            List<ToolItem> toolItems = new List<ToolItem>();
            toolItems.Add(new Verilog.Snippets.AlwaysFFSnippet());
            toolItems.Add(new Verilog.Snippets.AutoConnectSnippet());
            toolItems.Add(new Verilog.Snippets.AutoFormatSnippet());
            toolItems.Add(new Verilog.Snippets.ModuleInstanceMenuSnippet());
            toolItems.Add(new Verilog.Snippets.PortConnectionCreateSnippet());
            toolItems.Add(new Verilog.Snippets.PortInvertSnippet());
            toolItems.Add(new Verilog.Snippets.ModPortSnippet());
            toolItems.Add(new Verilog.Snippets.ConnectionCheckSnippet());

            if (AppendToolItems != null) AppendToolItems(toolItems, item, index);

            return toolItems;
        }
        // Append Tools
        public delegate void AppendToolItemDelegate(List<ToolItem> toolItems, IVerilogRelatedFile item, int index);
        public static AppendToolItemDelegate? AppendToolItems;


        public static Func<Data.VerilogCommon.AutoCompleteItem, bool> RunPartialParse(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index)
        {
            Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter = (Data.VerilogCommon.AutoCompleteItem ac) =>
            {
                return true;
            };

            string candidateWord = "";
            List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items = new List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>();

            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return null;

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);
            System.Diagnostics.Debug.Print("##partial parse start");

            int parseBlockIndex = 0;
            NameSpace? nameSpace = null;
            Verilog.Items.IItem? iitem = null;
            if (!GetAutoCompleteTarget(item, parsedDocument, index, out nameSpace, out INamedElement? element, out candidateWord, out int candidateStartIndex))
            {
                return itemFilter;
            }
            if (nameSpace == null) return itemFilter;
            if (item.CodeDocument == null) return itemFilter;
            parseBlockIndex = nameSpace.BeginIndexReference.RootIndex;

            IndexReference iref = IndexReference.Create(lineStartIndex, parsedDocument);
            iitem = parsedDocument.GetItemAt(iref);

            if(iitem != null && iitem.BeginIndexReference != null)
            {
                parseBlockIndex = iitem.BeginIndexReference.RootIndex;
            }
            if (candidateStartIndex - parseBlockIndex < 1) return itemFilter;

            string blockText = item.CodeDocument.CreateString(parseBlockIndex, candidateStartIndex - parseBlockIndex);
            pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(blockText);
            WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);


            System.Diagnostics.Debug.Print("##partial parse"+iitem.GetType().Name);
            if( iitem is Verilog.Items.ModuleInstantiation)
            {
                itemFilter = Verilog.Items.ModuleInstantiation.ParseAsync(word, nameSpace).GetAwaiter().GetResult();
            }

            return itemFilter;
        }

        public static List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>? GetAutoCompleteItems(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, out string candidateWord)
        {
            candidateWord = "";

            List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items = new List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>();

            CodeEditor.CodeDocument? codeDocument = item.CodeDocument as CodeEditor.CodeDocument;
            if (codeDocument == null) return null;

            int line = codeDocument.GetLineAt(index);
            int lineStartIndex = codeDocument.GetLineStartIndex(line);

            if (!GetAutoCompleteTarget(item, parsedDocument, index, out NameSpace? nameSpace, out INamedElement? element, out candidateWord, out int candidateStartIndex))
            {
                return null;
            }

            Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter = (Data.VerilogCommon.AutoCompleteItem ac) =>
            {
                return true;
            };

            itemFilter = RunPartialParse(item, parsedDocument, index);

            if (element != null)
            {   // has hier nameSpace cantidate
                foreach (INamedElement subElement in element.NamedElements.Values)
                {
                    if (candidateWord != "" && !subElement.Name.StartsWith(candidateWord)) continue;
                    if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

                    Data.VerilogCommon.AutoCompleteItem acItem = new AutoCompleteItem(
                        AutoCompleteItem.CompleteType.NameSpace,
                        subElement.Name,
                        CodeDrawStyle.ColorIndex(subElement.ColorType),
                        Global.CodeDrawStyle.Color(subElement.ColorType),
                        "CodeEditor2/Assets/Icons/tag.svg"
                        );
                    if (itemFilter(acItem)) items.Add(acItem);
                }
                return items;
            }

            // hier cantidate : get current line namespace and region
            IndexReference iref = Verilog.IndexReference.Create(parsedDocument, codeDocument, lineStartIndex);
            Verilog.Items.IItem? currentItem = parsedDocument.GetItemAt(iref);



            bool onLineStart = false;
            while(true){
                string lineString =codeDocument.CreateLineString(line);
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
                items = new List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>();
                foreach (string key in parsedDocument.ProjectProperty.SystemFunctions.Keys)
                {
                    if (!key.StartsWith(candidateWord)) continue;
                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        AutoCompleteItem.CompleteType.Function,
                        key,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                        );
                    if (itemFilter(acItem)) items.Add(acItem);
                }
                foreach (string key in parsedDocument.ProjectProperty.SystemTaskParsers.Keys)
                {
                    if (!key.StartsWith(candidateWord)) continue;
                    Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                        AutoCompleteItem.CompleteType.Task,
                        key,
                        CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword),
                        Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword)
                        );
                    if (itemFilter(acItem)) items.Add(acItem);
                }
                return items;
            }

            if(onLineStart && nameSpace != null && nameSpace.BuildingBlock is Module && candidateWord.Length > 1 && (currentItem is Module || currentItem is GenerateBlock) )
            {
                CodeEditor2.Data.Project project = nameSpace.Project;
                ProjectProperty? projectProperty = project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();

                List<string> moduleNames = projectProperty.DefinitionNameSpace.GetNameList((x) => { return (x is Module); });
                foreach (string moduleName in moduleNames)
                {
                    items.Add(new Verilog.Snippets.ModuleInstanceSnippet(moduleName));
                }
            }

            if (element == null)
            {
                if (nameSpace != null)
                {
                    // search upward
                    appendItemsUpward(items, nameSpace, candidateStartIndex, candidateWord,itemFilter);
                }
                // keywords
                VerilogCommon.AutoCompleteKeyword.AppendKeywordAutoCompleteItems(items, candidateWord, candidateStartIndex, lineStartIndex, parsedDocument.SystemVerilog,itemFilter);
            }
            else // sub element
            {
                // append sub-element items
                foreach (INamedElement subElement in element.NamedElements.Values)
                {
                    if (candidateWord != "" && !subElement.Name.StartsWith(candidateWord)) continue;
                    if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements

                    AutoCompleteItem.CompleteType completeType = AutoCompleteItem.CompleteType.Keyword;
                    if (subElement is NameSpace) completeType = AutoCompleteItem.CompleteType.NameSpace;
                    if (subElement is Verilog.DataObjects.DataObject) completeType = AutoCompleteItem.CompleteType.DataObject;
                    if(completeType == AutoCompleteItem.CompleteType.Keyword && System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }

                    AutoCompleteItem acItem = new AutoCompleteItem(
                        completeType,
                        subElement.Name,
                        CodeDrawStyle.ColorIndex(subElement.ColorType),
                        Global.CodeDrawStyle.Color(subElement.ColorType),
                        "CodeEditor2/Assets/Icons/tag.svg"
                        );
                    if (itemFilter(acItem)) items.Add(acItem);
                }
            }

            return items;
        }


        public static void appendItemsUpward(List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items, NameSpace nameSpace, int candidateStartIndex, string candidateWord, Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter)
        {
            foreach (INamedElement subElement in nameSpace.NamedElements.Values)
            {
                if (!subElement.Name.StartsWith(candidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (items.Find(x => x.Text == subElement.Name) != null) continue;   // reject duplicated elements

                AutoCompleteItem.CompleteType completeType = AutoCompleteItem.CompleteType.Keyword;
                if (subElement is NameSpace) completeType = AutoCompleteItem.CompleteType.NameSpace;
                if (subElement is Verilog.DataObjects.DataObject) completeType = AutoCompleteItem.CompleteType.DataObject;
                if (subElement is Verilog.Items.ModuleInstantiation) completeType = AutoCompleteItem.CompleteType.NameSpace;
                if (completeType == AutoCompleteItem.CompleteType.Keyword && System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }

                AutoCompleteItem acItem = new AutoCompleteItem(
                    completeType,
                    subElement.Name,
                    CodeDrawStyle.ColorIndex(subElement.ColorType),
                    Global.CodeDrawStyle.Color(subElement.ColorType),
                    "CodeEditor2/Assets/Icons/tag.svg"
                    );
                if (itemFilter(acItem)) items.Add(acItem);
            }
            if (nameSpace.Parent != null)
            {
                appendItemsUpward(items, nameSpace.Parent, candidateStartIndex, candidateWord,itemFilter);
            }
        }
        public static bool GetAutoCompleteTarget(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, out NameSpace? nameSpace, out INamedElement? element, out string candidate, out int candidateStartIndex)
        {
            candidate = "";
            candidateStartIndex = 0;
            nameSpace = null;
            element = null;
            if (item.VerilogParsedDocument == null) return false;
            if (item.CodeDocument == null) return false;

            int line = item.CodeDocument.GetLineAt(index);
            int lineStartIndex = item.CodeDocument.GetLineStartIndex(line);

            string lineText = item.CodeDocument.CreateLineString(line).Substring(0, index - lineStartIndex);
            candidateStartIndex = lineStartIndex;

            { // pre carlet char check
                if (index != 0)
                {
                    char preChar = item.CodeDocument.GetCharAt(index - 1);
                    if (preChar == ' ') return false;
                    if (preChar == '\t') return false;
                }
            }

            { // remove comment start to activate auto complete in comments
                int commentIndex = lineText.LastIndexOf("/*");
                if (commentIndex > 0)
                {
                    commentIndex = commentIndex + 2;
                    lineText = lineText.Substring(commentIndex);
                    candidateStartIndex += commentIndex;
                }
            }
            { // remove comment start to activate auto complete in comments
                int commentIndex = lineText.LastIndexOf("//");
                if (commentIndex > 0)
                {
                    commentIndex = commentIndex + 2;
                    lineText = lineText.Substring(commentIndex);
                    candidateStartIndex += commentIndex;
                }
            }


            int blockStartIndex = 0;
            int blockEndIndex = 0;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(lineText);
                WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);

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

                (string, int) lastWord = ("", 0);
                if (words.Count == 0)
                {
                    lastWord = ("", 0);
                }
                else if (words.Last().Item1 == "." || words.Last().Item1 == "::" || words.Last().Item1 == "->")
                {
                    lastWord = ("", words.Last().Item2 + words.Last().Item1.Length);
                    blockEndIndex = words.Last().Item2;
                    blockStartIndex = words[0].Item2;
                }
                else
                {
                    // word . lastword
                    lastWord = words.Last();
                    blockStartIndex = words[0].Item2;
                    if (words.Count > 2)
                    {
                        (string, int) prevLast = words[words.Count - 2];
                        blockEndIndex = prevLast.Item2;
                    }
                    else
                    {
                        blockEndIndex = blockStartIndex;
                    }
                }
                candidate = lastWord.Item1;
                candidateStartIndex += lastWord.Item2;
            }

            // get namespace
            {
                // namespace must get from linestart index, because current index cann't match last parsed document
                IndexReference iref = IndexReference.Create(parsedDocument.IndexReference, lineStartIndex);
                nameSpace = parsedDocument.GetNameSpace(iref);
            }

            string elementText = lineText.Substring(blockStartIndex, blockEndIndex - blockStartIndex);
            element = null;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(elementText);
                WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);

                if (nameSpace != null)
                {
                    Verilog.Expressions.NameReference? nameReference = Verilog.Expressions.NameReference.ParseCreate(word, nameSpace, true);
                    if (nameReference != null)
                    {
                        INamedElement? targetElement;
                        NameSpace? targetNameSpace;
                        (element, targetElement) = nameReference.GetElement(nameSpace);
                        targetNameSpace = targetElement as NameSpace;
                    }

                }

                if(element is Verilog.Items.IBuildingBlockInstantiation)
                {
                    Verilog.Items.IBuildingBlockInstantiation inst = (Verilog.Items.IBuildingBlockInstantiation)element;
                    element = inst.GetInstancedBuildingBlock();
                }

                /*
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
                */
            }
            return true;
        }



        // Append Tools
        //        public delegate void AppendAutocompleteItemDelegate(List<AutocompleteItem>? toolItems, IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, ref string? candidateWord);
        //        public static AppendAutocompleteItemDelegate? AppendAutocompleteItems;



        private static void applyAutoInput(IVerilogRelatedFile item)
        {
            if (item.CodeDocument == null) return;

            int index = item.CodeDocument.CaretIndex;
            int line = item.CodeDocument.GetLineAt(index);
            if (line == 0) return;

            int lineHeadIndex = item.CodeDocument.GetLineStartIndex(line);

            int prevTabs = 0;
            if (line != 1)
            {
                int prevLine = line - 1;
                int prevLineHeadIndex = item.CodeDocument.GetLineStartIndex(prevLine);
                for (int i = prevLineHeadIndex; i < lineHeadIndex; i++)
                {
                    char ch = item.CodeDocument.GetCharAt(i);
                    if (ch == '\t')
                    {
                        prevTabs++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            int indentLength = 0;
            for (int i = lineHeadIndex; i < item.CodeDocument.Length; i++)
            {
                char ch = item.CodeDocument.GetCharAt(i);
                if (ch == '\t')
                {
                    indentLength++;
                }
                else if (ch == ' ')
                {
                    indentLength++;
                }
                else
                {
                    break;
                }
            }


            bool prevBegin = isPrevBegin(item, lineHeadIndex);
            bool nextEnd = isNextEnd(item, lineHeadIndex);

            if (prevBegin)
            {
                if (nextEnd) // caret is sandwiched beteen begin and end
                {
                    // BEFORE
                    // begin[enter] end

                    // AFTER
                    // begin
                    //     [caret]
                    // end
                    item.CodeDocument.Replace(lineHeadIndex, indentLength, 0, new String('\t', prevTabs + 1) + "\r\n" + new String('\t', prevTabs));
                    CodeEditor2.Controller.CodeEditor.SetCaretPosition(item.CodeDocument.CaretIndex + prevTabs + 1 + 1 - indentLength);
                    return;
                }
                else
                {   // add indent
                    prevTabs++;
                }
            }

            if (prevTabs != 0) item.CodeDocument.Replace(lineHeadIndex, indentLength, 0, new String('\t', prevTabs));
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(item.CodeDocument.CaretIndex + prevTabs - indentLength);
        }

        private static bool isPrevBegin(IVerilogRelatedFile item, int index)
        {
            if (item.CodeDocument == null) return false;

            int prevInex = index;
            if (prevInex > 0) prevInex--;

            if (prevInex > 0 && item.CodeDocument.GetCharAt(prevInex) == '\n') prevInex--;
            if (prevInex > 0 && item.CodeDocument.GetCharAt(prevInex) == '\r') prevInex--;

            if (prevInex == 0 || item.CodeDocument.GetCharAt(prevInex) != 'n') return false;
            prevInex--;
            if (prevInex == 0 || item.CodeDocument.GetCharAt(prevInex) != 'i') return false;
            prevInex--;
            if (prevInex == 0 || item.CodeDocument.GetCharAt(prevInex) != 'g') return false;
            prevInex--;
            if (prevInex == 0 || item.CodeDocument.GetCharAt(prevInex) != 'e') return false;
            prevInex--;
            if (item.CodeDocument.GetCharAt(prevInex) != 'b') return false;
            return true;
        }

        private static bool isNextEnd(IVerilogRelatedFile item, int index)
        {
            if (item.CodeDocument == null) return false;

            int prevInex = index;
            if (prevInex < item.CodeDocument.Length &&
                (
                    item.CodeDocument.GetCharAt(prevInex) == ' ' || item.CodeDocument.GetCharAt(prevInex) == '\t'
                )
            ) prevInex++;

            if (prevInex >= item.CodeDocument.Length || item.CodeDocument.GetCharAt(prevInex) != 'e') return false;
            prevInex++;
            if (prevInex >= item.CodeDocument.Length || item.CodeDocument.GetCharAt(prevInex) != 'n') return false;
            prevInex++;
            if (item.CodeDocument.GetCharAt(prevInex) != 'd') return false;
            return true;
        }

    }
}
