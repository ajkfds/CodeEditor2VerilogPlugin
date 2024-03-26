using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CodeEditor2.CodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.CodeEditor
{
    public class CodeDocument : CodeEditor2.CodeEditor.CodeDocument
    {
        public CodeDocument(Data.IVerilogRelatedFile file) :base(file as CodeEditor2.Data.TextFile) 
        { 
        }

        public CodeDocument(string text) : base(null,text)
        {
        }

        public static CodeDocument SnapShotFrom(CodeDocument codeDocument)
        {
            CodeDocument document = new CodeDocument(codeDocument.VerilogFile);
            document.textDocument.Text = codeDocument.textDocument.CreateSnapshot().Text;
            document.textFileRef = codeDocument.textFileRef;
            return document;
        }


        public Data.IVerilogRelatedFile VerilogFile
        {
            get { return TextFile as Data.IVerilogRelatedFile; }
        }

        public override void SetColorAt(int index, byte value)
        {
            if (TextDocument == null) return;
            DocumentLine line = TextDocument.GetLineByOffset(index);
            LineInfomation lineInfo = GetLineInfomation(line.LineNumber);
            Color color = Global.CodeDrawStyle.ColorPallet[value];
            lineInfo.Colors.Add(new LineInfomation.Color(index, 1, color));
        }

        public override void SetColorAt(int index, byte value, int length)
        {
            if (TextDocument == null) return;

            DocumentLine lineStart = TextDocument.GetLineByOffset(index);
            DocumentLine lineLast = TextDocument.GetLineByOffset(index + length);
            Color color = Global.CodeDrawStyle.ColorPallet[value];

            if (lineStart == lineLast)
            {
                LineInfomation lineInfo = GetLineInfomation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInfomation.Color(index, length, color));
            }
            else
            {
                for (int line = lineStart.LineNumber; line <= lineLast.LineNumber; line++)
                {
                    LineInfomation lineInfo = GetLineInfomation(line);
                    lineInfo.Colors.Add(new LineInfomation.Color(index, index + length, color));
                }
            }
        }

        public override void SetMarkAt(int index, byte value)
        {
            if (index >= Length) return;
            if (TextDocument == null) return;
            DocumentLine line = TextDocument.GetLineByOffset(index);
            LineInfomation lineInfo = GetLineInfomation(line.LineNumber);
            Color color = Global.CodeDrawStyle.MarkColor[value];
            lineInfo.Effects.Add(new LineInfomation.Effect(index, 1, color, null));
        }

        // get word boundery for editor word selection

        public override void GetWord(int index, out int headIndex, out int length)
        {
            lock (this)
            {
                int line = GetLineAt(index);
                headIndex = GetLineStartIndex(line);
    //            headIndex = index;
                //length = 0;
                //char ch = GetCharAt(index);
                //if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t') return;

                //while (headIndex >= 0)
                //{
                //    ch = GetCharAt(headIndex);
                //    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                //    {
                //        break;
                //    }
                //    headIndex--;
                //}
                //headIndex++;
                //if (index < headIndex) headIndex = index;

                int nextIndex;
                Verilog.WordPointer.WordTypeEnum wordType;
                string sectionName = "";
                Verilog.WordPointer.FetchNext(this, ref headIndex, out length, out nextIndex, out wordType,ref sectionName);

                while(nextIndex <= index && index < Length)
                {
                    headIndex = nextIndex;
                    Verilog.WordPointer.FetchNext(this, ref headIndex, out length, out nextIndex, out wordType,ref sectionName);
                }
            }
        }

        public List<string> GetHierWords(int index,out bool endWithDot)
        {
            lock (this)
            {

                List<string> ret = new List<string>();
                int headIndex = GetLineStartIndex(GetLineAt(index));
                int length;
                int nextIndex = headIndex;
                Verilog.WordPointer.WordTypeEnum wordType;
                endWithDot = true;

                // return blank if on space char
                if (index != 0)
                {
                    char ch = GetCharAt(index - 1);
                    if (ch == ' ' || ch == '\t')
                    {
                        endWithDot = false;
                        return new List<string>();
                    }
                }

                string sectioName = "";
                // get words on the index line until index
                while (headIndex < Length)
                {
                    Verilog.WordPointer.FetchNext(this, ref headIndex, out length, out nextIndex, out wordType,ref sectioName);
                    if (length == 0) break;
                    if (headIndex >= index) break;
                    ret.Add(CreateString(headIndex, length));
                    headIndex = nextIndex;
                }

                // search wors from end
                int i= ret.Count - 1;
                if (i >= 0 && ret[i] != ".")
                {
                    endWithDot = false;
                    i--; // skip last non . word
                }

                while (i>=0)
                {
                    if (ret[i] != ".") break; // end if not .
                    ret.RemoveAt(i);
                    i--;

    //                if (i == 0) break;
                    i--;
                }

                for(int j = 0; j <= i; j++) // remove before heir description
                {
                    ret.RemoveAt(0);
                }

                return ret;
            }
        }

        public string GetIndentString(int index)
        {
            lock (this)
            {
                StringBuilder sb = new StringBuilder();
                int line = GetLineAt(index);
                int headIndex = GetLineStartIndex(GetLineAt(index));
                int lineLength = GetLineLength(line);

                int i = headIndex;
                while( i < headIndex + lineLength)
                {
                    if (GetCharAt(i) != '\t' && GetCharAt(i) != ' ') break;
                    sb.Append(GetCharAt(i));
                    i++;
                }
                return sb.ToString();
            }
        }

    }
}
