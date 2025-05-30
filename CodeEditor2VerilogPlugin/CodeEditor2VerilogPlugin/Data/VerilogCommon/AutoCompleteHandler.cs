﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;

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

            if(item.VerilogParsedDocument == null) return null;
            if(item.CodeDocument == null) return null;

            int line = item.CodeDocument.GetLineAt(index);
            int lineStartIndex = item.CodeDocument.GetLineStartIndex(line);
            bool endWithDot;
            List<string> words = ((pluginVerilog.CodeEditor.CodeDocument)item.CodeDocument).GetHierWords(index, out endWithDot);
            if (endWithDot)
            {
                candidateWord = "";
            }
            else
            {
                candidateWord = words.LastOrDefault();
                if (words.Count > 0)
                {
                    words.RemoveAt(words.Count - 1);
                }
            }
            if (candidateWord == null) candidateWord = "";

            List<AutocompleteItem>? items = parsedDocument.GetAutoCompleteItems(words, lineStartIndex, line, (CodeEditor.CodeDocument)item.CodeDocument, candidateWord);

            if (AppendAutocompleteItems != null) AppendAutocompleteItems(items, item, parsedDocument, index, ref candidateWord);

            return items;
        }
        // Append Tools
        public delegate void AppendAutocompleteItemDelegate(List<AutocompleteItem>? toolItems, IVerilogRelatedFile item, Verilog.ParsedDocument parsedDocument, int index, ref string? candidateWord);
        public static AppendAutocompleteItemDelegate? AppendAutocompleteItems;



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
