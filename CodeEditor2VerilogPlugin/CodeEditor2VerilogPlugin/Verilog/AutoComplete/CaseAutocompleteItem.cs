using AvaloniaEdit.Rendering;
using CodeEditor2.CodeEditor.CodeComplete;
using Microsoft.Playwright;
using pluginVerilog.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.AutoComplete
{
    public class CaseAutocompleteItem : AutocompleteItem
    {
        public CaseAutocompleteItem() : base("case", CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Keyword), Plugin.ThemeColor, "CodeEditor2/Assets/Icons/screwdriver.svg")
        {
        }

        private string CaseHeader = "P_";

        public override System.Threading.Tasks.Task ApplyAsync()
        {
            if (codeDocument == null) return System.Threading.Tasks.Task.CompletedTask;
            CodeEditor.CodeDocument? document = (codeDocument as CodeEditor.CodeDocument);
            if (document == null) return System.Threading.Tasks.Task.CompletedTask;

            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            int headIndex, length;

            document.GetWord(prevIndex, out headIndex, out length);
            string indent = document.GetIndentString(prevIndex);
            string cr = document.NewLine;

            char currentChar = document.GetCharAt(document.CaretIndex);

            string appendText = "case([])" + cr;
            appendText += indent + "\t" + cr;
            appendText += indent + "endcase" + cr;

            IVerilogRelatedFile vfile = document.VerilogFile;

            {
                BuildingBlocks.BuildingBlock? buildingBlock = vfile.VerilogParsedDocument?.GetBuildingBlockAt(prevIndex);
                if(buildingBlock != null)
                {
                    foreach(INamedElement namedElement in buildingBlock.NamedElements)
                    {
                        if (namedElement is not Verilog.DataObjects.Constants.Constants) continue;
                        if (namedElement.Name.StartsWith(CaseHeader))
                        {
                            appendText += indent + "\t" + namedElement.Name + ":" + cr;
                        }
                    }
                }
            }

            //if (currentChar != '\r' && currentChar != '\n')
            //{
            //    appendText = "";
            //}
            int selectStart = appendText.IndexOf("[");
            int selectLast = appendText.IndexOf("]");
            appendText = appendText.Replace("[", "");
            appendText = appendText.Replace("]", "");

            document.Replace(headIndex, length, ColorIndex, appendText);
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(headIndex + selectStart);
            CodeEditor2.Controller.CodeEditor.SetSelection(headIndex + selectStart, headIndex + selectLast - 2);

            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
