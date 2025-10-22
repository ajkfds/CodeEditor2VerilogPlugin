using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items.Generate
{
    public class GenvarIteration
    {

        //genvar_iteration::= 
        //    genvar_identifier assignment_operator genvar_expression 
        //    | inc_or_dec_operator genvar_identifier 
        //    | genvar_identifier inc_or_dec_operator

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            //    genvar_assignment::= genvar_identifier = constant_expression
            if (word.NextText == "=")
            {
                Expressions.DataObjectReference? genvar = Expressions.DataObjectReference.ParseCreate(word, nameSpace, nameSpace.BuildingBlock, true);
                if (genvar == null) return false;
                if (!(genvar.DataObject is DataObjects.Variables.Genvar))
                {
                    word.AddError("should be genvar");
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
            }
            else
            {
                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    word.AddError("should be genvar_iteration");
                    return false;
                }
                if(expression is not Expressions.Operators.IncDecOperator)
                {
                    word.AddError("should be genvar_iteration");
                    return true;
                }
                Expressions.Operators.IncDecOperator op = (Expressions.Operators.IncDecOperator)expression;
                if(op.Primary is not Expressions.DataObjectReference)
                {
                    op.Primary.Reference.AddError("must be genvar");
                    return true;
                }
                Expressions.DataObjectReference dataObjectReference = (Expressions.DataObjectReference)op.Primary;
                if(dataObjectReference.DataObject is not DataObjects.Variables.Genvar)
                {
                    op.Primary.Reference.AddError("must be genvar");
                    return true;
                }
            }


            return true;
        }

    }
}