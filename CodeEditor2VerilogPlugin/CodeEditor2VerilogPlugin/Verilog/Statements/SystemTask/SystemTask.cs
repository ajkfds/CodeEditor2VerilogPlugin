using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements.SystemTask
{
    public class SystemTask : IStatement
    {
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public void DisposeSubReference()
        {
            return;
        }
        public static SystemTask ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SystemTask taskEnable = new SystemTask();

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                if (word.Text == ")")
                {
                    word.MoveNext();
                    word.AddWarning("remove ()");
                }
                else
                {

                    while (!word.Eof)
                    {
                        Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expression == null)
                        {
                            word.AddError("missed expression");
                            word.SkipToKeyword(";");
                            if(word.Text == ";")
                            {
                                word.MoveNext();
                            }
                            return null;
                        }
                        if (word.Text == ")")
                        {
                            break;
                        }
                        else if (word.Text == ",")
                        {
                            word.MoveNext();
                            continue;
                        }
                        else
                        {
                            word.AddError("illegal expression");
                            return null;
                        }
                    }
                    if (word.Text == ")") word.MoveNext();
                    else word.AddError(") required");
                }
            }

            if (word.Text == ";") word.MoveNext();
            else word.AddError("; required");

            return taskEnable;

        }
    }
}