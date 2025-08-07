using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items.Generate
{
    public class GenvarInitialization
    {
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // loop_generate_construct::= for (genvar_initialization; genvar_expression; genvar_iteration) generate_block
            // genvar_initialization ::= [genvar] genvar_identifier = constant_expression

            Expressions.DataObjectReference? genvar;
            if (word.Text == "genvar")
            {
                // define new genvar
                if (!word.SystemVerilog) word.AddSystemVerilogError();
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Eof) return true;
                DataObjects.Variables.Genvar gvar = new DataObjects.Variables.Genvar(word.Text);
                gvar.DefinedReference = word.GetReference();
                if (word.Prototype)
                {
                    if (nameSpace.NamedElements.ContainsKey(gvar.Name))
                    {
                        word.AddError("iillegal genvar name");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(gvar.Name, gvar);
                    }
                }
                else
                {
                    nameSpace.NamedElements.Replace(gvar.Name, gvar);
                }
                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();
                genvar = Expressions.DataObjectReference.Create(gvar, nameSpace);
                if (genvar == null) return true;
            }
            else
            {
                genvar = Expressions.DataObjectReference.ParseCreate(word, nameSpace, nameSpace, true);
                if (genvar == null)
                {
                    word.AddError("must be genvar");
                    return false;
                }
                if (genvar.DataObject is not Genvar)
                {
                    genvar.Reference.AddError("must be genvar");
                }
            }

            if (word.Text != "=")
            {
                word.AddError("( expected");
                return true;
            }
            word.MoveNext();
            Expressions.Expression? constant = Expressions.Expression.ParseCreate(word, nameSpace);
            if (constant == null) return false;
            if (!constant.Constant)
            {
                word.AddError("should be constant");
            }
            return true;
        }
    }
}
