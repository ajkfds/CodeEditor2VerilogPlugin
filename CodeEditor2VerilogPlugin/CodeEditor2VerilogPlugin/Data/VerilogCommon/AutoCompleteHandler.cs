using Avalonia.Input;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            toolItems.Add(new Verilog.Snippets.ModPortSnippet());

            if(AppendToolItems != null) AppendToolItems(toolItems, item, index);

            return toolItems;
        }
        // Append Tools
        public delegate void AppendToolItemDelegate (List<ToolItem> toolItems, IVerilogRelatedFile item, int index);
        public static AppendToolItemDelegate? AppendToolItems;

        public static List<AutocompleteItem>? GetAutoCompleteItems(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, out string candidateWord)
        {
            candidateWord = "";

            if (item.VerilogParsedDocument == null) return null;
            if (item.CodeDocument == null) return null;

            int line = item.CodeDocument.GetLineAt(index);
            int lineStartIndex = item.CodeDocument.GetLineStartIndex(line);

            { // pre carlet char check
                if(index != 0)
                {
                    char preChar = item.CodeDocument.GetCharAt(index - 1);
                    if (preChar == ' ') return null;
                    if (preChar == '\t') return null;
                }
            }

            // get text chunk
            string preText = "";
            string postText = "";
            {
                string text = item.CodeDocument.CreateLineString(line).Substring(0, index - lineStartIndex);
                string[] texts = text.Split(new char[] { '\n', '\r', ' ', '\t', '=','{','}','(',')' });//, StringSplitOptions.RemoveEmptyEntries);
                if (texts.Length == 0) return null;
                text = texts.Last();
                int dotIndex = text.IndexOf('.');
                if (dotIndex > 0)
                {
                    preText = text.Substring(0, dotIndex);
                }
                if (dotIndex > 0)
                {
                    postText = text.Substring(dotIndex + 1);
                }
                else
                {
                    postText = text;
                }
            }
            candidateWord = postText;

            List<AutocompleteItem> items = new List<AutocompleteItem>();

            // system task & functions
            // return system task and function if the word starts with "$"
            if (candidateWord.StartsWith("$") && parsedDocument.ProjectProperty != null)
            {
                items = new List<AutocompleteItem>();
                foreach (string key in parsedDocument.ProjectProperty.SystemFunctions.Keys)
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
                foreach (string key in parsedDocument.ProjectProperty.SystemTaskParsers.Keys)
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

            // get namespace
            NameSpace? nameSpace = null;
            {
                // namespace must get from linestart index, because current index cann't match last parsed document
                IndexReference iref = IndexReference.Create(parsedDocument.IndexReference, lineStartIndex);
                nameSpace = parsedDocument.GetNameSpace(iref);
            }
            
            if (nameSpace == null)
            {
                if (preText == "")
                {
                    // special autocomplete tools
                    Verilog.ParsedDocument.AppendSpecialAutoCompleteItems(items, candidateWord);
                    // keywords
                    Verilog.ParsedDocument.AppendKeywordAutoCompleteItems(items, candidateWord, parsedDocument.SystemVerilog);
                }
                return items;
            }

            INamedElement? element = null;
            {
                // create short document to parse current pretext
                pluginVerilog.CodeEditor.CodeDocument document = new pluginVerilog.CodeEditor.CodeDocument(preText);
                WordScanner word = new WordScanner(document, parsedDocument, parsedDocument.SystemVerilog);

                Verilog.Expressions.Expression? expression = null;
                while (!word.Eof)
                {
                    expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null) word.MoveNext();
                }
                if (expression is Verilog.Expressions.DataObjectReference)
                {
                    Verilog.Expressions.DataObjectReference dataObjectReference = (Verilog.Expressions.DataObjectReference)expression;
                    element = dataObjectReference.DataObject;
                }
                else if (expression is Verilog.Expressions.NameSpaceReference)
                {
                    NameSpace targetNameSpace = ((Verilog.Expressions.NameSpaceReference)expression).NameSpace;
                    element = targetNameSpace;
                }
            }

            if (preText == "")
            {
                // append INamedElements
                if (nameSpace != null)
                {
                    // search upward
                    appendItemsUpward(items, nameSpace, candidateWord);
                }
            }
            else
            {
                // append sub-element items
                if (element != null)
                {
                    foreach (INamedElement subElement in element.NamedElements.Values)
                    {
                        if (!subElement.Name.StartsWith(candidateWord)) continue;
                        if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                        items.Add(
                            new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                                subElement.Name,
                                CodeDrawStyle.ColorIndex(subElement.ColorType),
                                Global.CodeDrawStyle.Color(subElement.ColorType),
                                "CodeEditor2/Assets/Icons/tag.svg"
                                )
                        );
                    }
                }


            }

            if (preText == "")
            {
                // special autocomplete tools
                Verilog.ParsedDocument.AppendSpecialAutoCompleteItems(items, candidateWord);
                // keywords
                Verilog.ParsedDocument.AppendKeywordAutoCompleteItems(items, candidateWord, parsedDocument.SystemVerilog);
            }

            return items;
        }

        public static void appendItemsUpward(List<AutocompleteItem> items,NameSpace nameSpace,string candidateWord)
        {
            foreach (INamedElement subElement in nameSpace.NamedElements.Values)
            {
                if (!subElement.Name.StartsWith(candidateWord)) continue;
                if (subElement.Name.StartsWith("\0", StringComparison.Ordinal)) continue; // reject unnamed elements
                if (items.Find(x => x.Text == subElement.Name) != null) continue;   // reject duplicated elements
                items.Add(
                    new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                        subElement.Name,
                        CodeDrawStyle.ColorIndex(subElement.ColorType),
                        Global.CodeDrawStyle.Color(subElement.ColorType),
                        "CodeEditor2/Assets/Icons/tag.svg"
                        )
                );
            }
            if(nameSpace.Parent != null)
            {
                appendItemsUpward(items, nameSpace.Parent, candidateWord);
            }
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
                    CodeEditor2.Controller.CodeEditor.SetCaretPosition( item.CodeDocument.CaretIndex + prevTabs + 1 + 1 - indentLength);
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
            if(item.CodeDocument == null) return false;

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
