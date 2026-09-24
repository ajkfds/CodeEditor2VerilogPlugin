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



        public static Verilog.CompletionContext? GetAutoCompleteItems(IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index)
        {

            List<CodeEditor2.CodeEditor.PopupMenu.ToolItem> items = new List<CodeEditor2.CodeEditor.PopupMenu.ToolItem>();

            Verilog.CompletionContext completionContextResult = new Verilog.CompletionContext(item, parsedDocument, index);
            if (completionContextResult.AutoCompleteItems.Count == 0) completionContextResult.AppendAll();
            return completionContextResult;
        }

        public static bool GetAutoCompleteTarget(Data.IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, out NameSpace? nameSpace, out INamedElement? element, out string candidate, out int candidateStartIndex)
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

                if (element is Verilog.Items.IBuildingBlockInstantiation)
                {
                    Verilog.Items.IBuildingBlockInstantiation inst = (Verilog.Items.IBuildingBlockInstantiation)element;
                    element = inst.GetInstancedBuildingBlock();
                }

            }

            if (candidate == "") // search undefined macro
            {
                int wordIndex = index;

                while(wordIndex>0)
                {
                    char preChar = item.CodeDocument.GetCharAt(wordIndex - 1);
                    if (preChar == ' ') break;
                    if (preChar == '\r') break;
                    if (preChar == '\n') break;
                    if (preChar == '\t') break;
                    wordIndex--;
                }
                if (item.CodeDocument.GetCharAt(wordIndex) != '`') return true;

                int lastIndex = wordIndex;
                while (lastIndex+1 < item.CodeDocument.Length)
                {
                    char nextChar = item.CodeDocument.GetCharAt(lastIndex+1);
                    if (nextChar == ' ') break;
                    if (nextChar == '\r') break;
                    if (nextChar == '\n') break;
                    if (nextChar == '\t') break;
                    lastIndex++;
                }
                candidate = item.CodeDocument.CreateString(wordIndex, lastIndex - wordIndex+1);
                candidateStartIndex = wordIndex;
            }

            return true;
        }

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
