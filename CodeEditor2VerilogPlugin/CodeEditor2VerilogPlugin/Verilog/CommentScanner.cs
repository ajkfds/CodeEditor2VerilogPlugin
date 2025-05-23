﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class CommentScanner
    {
        public CommentScanner(CodeEditor2.CodeEditor.CodeDocument document,int commentStart,int commentEnd)
        {
            this.document = document;
            this.commentStart = commentStart;
            this.commentEnd = commentEnd;

            index = commentStart;
            indexEnd = commentStart;
        }
        CodeEditor2.CodeEditor.CodeDocument document;
        int commentStart;
        int commentEnd;

        int index;
        int indexEnd;

        public void SkipToChar(char ch)
        {
            while(index < commentEnd)
            {
                if(document.GetCharAt(index) == ch)
                {
                    getNext();
                    return;
                }
                index++;
            }
        }

        public void MoveNext()
        {
            getNext();
        }

        public void MoveNextUntilEol()
        {
            index = indexEnd;
            char ch;

            // skip to 1st of next word
            while (index < commentEnd)
            {
                ch = document.GetCharAt(index);
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    index++;
                    continue;
                }
                break;
            }

            // get end index
            indexEnd = index;
            while (indexEnd < commentEnd)
            {
                ch = document.GetCharAt(indexEnd);
                if (ch == '\n' || ch == '\r')
                {
                    break;
                }
                indexEnd++;
            }
        }

        public void Color(CodeDrawStyle.ColorType colorType)
        {
            document.TextColors.SetColorAt(index, CodeDrawStyle.ColorIndex(colorType),indexEnd-index);
        }
        public string Text
        {
            get
            {
                return document.CreateString(index, indexEnd - index);
            }
        }

        public bool EOC
        {
            get
            {
                if (index >= commentEnd) return true;
                return false;
            }
        }

        private void getNext()
        {
            if (EOC) return;
            index = indexEnd;
            char ch;

            // skip to 1st of next word
            while (index < commentEnd)
            {
                ch = document.GetCharAt(index);
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    index++;
                    continue;
                }
                break;
            }

            // get end index
            indexEnd = index;
            while (indexEnd < commentEnd)
            {
                ch = document.GetCharAt(indexEnd);
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    break;
                }
                indexEnd++;
                if (ch == ':')
                {
                    return;
                }
                break;
            }

            while (indexEnd < commentEnd)
            {
                ch = document.GetCharAt(indexEnd);
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == ':')
                {
                    break;
                }
                indexEnd++;
            }
        }

    }
}
