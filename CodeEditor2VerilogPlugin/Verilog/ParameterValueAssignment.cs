using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using System.Collections.Generic;
using System.Linq;

namespace pluginVerilog.Verilog
{
    public class ParameterValueAssignment
    {

        // port setup on instancing
        public static bool ParseCreate(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression> parameterOverrides,
            BuildingBlock? buildingBlock
            )
        {
            if (word.Text != "#") return false;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("( expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            word.MoveNext();

            if (word.Text == ".")
            { // named parameter assignment
                // (EOF just after "#(" followed by "."): suggest unassigned parameter names
                if (word.CompletionContext != null && word.Eof)
                {
                    appendParameterNameCandidates(word.CompletionContext, buildingBlock, parameterOverrides);
                    return true;
                }
                while (!word.Eof && word.Text == ".")
                {
                    bool error = false;
                    word.MoveNext();

                    // (EOF just after "."): suggest unassigned parameter names
                    if (word.CompletionContext != null && word.Eof)
                    {
                        appendParameterNameCandidates(word.CompletionContext, buildingBlock, parameterOverrides);
                        return true;
                    }

                    word.Color(CodeDrawStyle.ColorType.Parameter);
                    string paramName = word.Text;
                    if (buildingBlock != null && !buildingBlock.PortParameterNameList.Contains(paramName))
                    {
                        word.AddError("illegal parameter name");
                        error = true;
                    }
                    word.MoveNext();

                    // (EOF just after ".param"): hint to type "("
                    if (word.CompletionContext != null && word.Eof)
                    {
                        appendParameterLabel(word.CompletionContext, buildingBlock, paramName);
                        return true;
                    }

                    if (word.Text != "(")
                    {
                        word.AddError("( expected");
                    }
                    else
                    {
                        word.MoveNext();
                    }

                    // (EOF just after ".param("): hint for this parameter
                    if (word.CompletionContext != null && word.Eof)
                    {
                        appendParameterLabel(word.CompletionContext, buildingBlock, paramName);
                        return true;
                    }

                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null)
                    {
                        // EOF while typing the parameter value expression (e.g. ".P("):
                        // hint for this parameter
                        if (word.CompletionContext != null && word.Eof)
                        {
                            appendParameterLabel(word.CompletionContext, buildingBlock, paramName);
                        }
                        error = true;
                    }
                   else if (!expression.Constant)
                    {
                        word.AddError("port parameter should be constant");
                        error = true;
                    }

                    // EOF just after the parameter value expression (e.g. ".P(WIDTH"):
                    // show the hint for this parameter and return without side effects
                    if (word.CompletionContext != null && word.Eof)
                    {
                        appendParameterLabel(word.CompletionContext, buildingBlock, paramName);
                        return true;
                    }

                    if (!error)//& word.Prototype)
                    {
                        if (parameterOverrides.ContainsKey(paramName))
                        {
                            word.AddPrototypeError("duplicated");
                        }
                    }

                    if (General.IsSimpleIdentifier(paramName) && expression != null)
                    {
                        if (expression != null) parameterOverrides.Add(paramName, expression);
                    }

                    if (word.Text != ")")
                    {
                        word.AddError(") expected");
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    if (word.Text != ",")
                    {
                        break;
                    }
                    else
                    {
                        word.MoveNext();
                    }
                }
            }
            else
            { // ordered parameter assignment
                int i = 0;
                while (!word.Eof && word.Text != ")")
                {
                    // (EOF just after "#(" or ","): hint for the next ordered parameter
                    if (word.CompletionContext != null && word.Eof)
                    {
                        if (buildingBlock != null && i < buildingBlock.PortParameterNameList.Count)
                        {
                            appendParameterLabel(word.CompletionContext, buildingBlock, buildingBlock.PortParameterNameList[i]);
                        }
                        return true;
                    }

                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    // EOF just after an expression (e.g. "#(WIDTH"): hint for the current parameter
                    if (word.CompletionContext != null && word.Eof)
                    {
                        if (buildingBlock != null && i < buildingBlock.PortParameterNameList.Count)
                        {
                            appendParameterLabel(word.CompletionContext, buildingBlock, buildingBlock.PortParameterNameList[i]);
                        }
                        return true;
                    }

                    if (buildingBlock != null)
                    {
                        if (i >= buildingBlock.PortParameterNameList.Count)
                        {
                            word.AddError("too many parameters");
                        }
                        else
                        {
                            string paramName = buildingBlock.PortParameterNameList[i];
                            if (word.Prototype && expression != null)
                            {
                                if (parameterOverrides.ContainsKey(paramName))
                                {
                                    word.AddError("duplicated");
                                }
                                else
                                {
                                    parameterOverrides.Add(paramName, expression);
                                }
                            }
                        }

                    }
                    i++;
                    if (word.Text != ",")
                    {
                        break;
                    }
                    else
                    {
                        word.MoveNext();
                    }
                }
            }

            if (word.Text != ")")
            {
                word.AddError("( expected");
                return true;
            }
            word.MoveNext();
            return true;
        }

        /// <summary>
        /// Append the label (type/description) of the parameter with the given name to the input-time hint popup items.
        /// </summary>
        private static void appendParameterLabel(
            CompletionContext completionContext, BuildingBlock? buildingBlock, string paramName)
        {
            if (buildingBlock == null) return;
            if (!buildingBlock.NamedElements.ContainsKey(paramName)) return;
            if (buildingBlock.NamedElements[paramName] is not DataObjects.Constants.Constants parameter) return;

            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            parameter.AppendLabel(label);
            completionContext.CarletPopupItems.Add(
                new CodeEditor2.CodeEditor.PopupHint.PopupItem(label));
        }

        /// <summary>
        /// Append unassigned parameter names as autocomplete candidates (named parameter "." position).
        /// </summary>
        private static void appendParameterNameCandidates(
            CompletionContext completionContext, BuildingBlock? buildingBlock,
            Dictionary<string, Expressions.Expression> parameterOverrides)
        {
            if (buildingBlock == null) return;
            foreach (string paramName in buildingBlock.PortParameterNameList)
            {
                if (parameterOverrides.ContainsKey(paramName)) continue;   // already assigned
                if (!paramName.StartsWith(completionContext.CandidateWord)) continue;

                Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                    Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject,
                    paramName,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Parameter),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Parameter),
                    "CodeEditor2/Assets/Icons/tag.svg"
                    );
                completionContext.AutoCompleteItems.Add(acItem);
            }
        }
    }
}
