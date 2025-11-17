using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                while (!word.Eof && word.Text == ".")
                {
                    bool error = false;
                    word.MoveNext();
                    word.Color(CodeDrawStyle.ColorType.Parameter);
                    string paramName = word.Text;
                    if (buildingBlock != null && !buildingBlock.PortParameterNameList.Contains(paramName))
                    {
                        word.AddError("illegal parameter name");
                        error = true;
                    }
                    word.MoveNext();

                    if (word.Text != "(")
                    {
                        word.AddError("( expected");
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null)
                    {
                        error = true;
                    }
                    else if (!expression.Constant)
                    {
                        word.AddError("port parameter should be constant");
                        error = true;
                    }

                    if (!error)//& word.Prototype)
                    {
                        if (parameterOverrides.ContainsKey(paramName))
                        {
                            word.AddPrototypeError("duplicated");
                        }
                    }

                    if(General.IsSimpleIdentifier(paramName) && expression != null)
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
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
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
    }
}
