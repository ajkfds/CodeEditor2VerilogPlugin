﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class ParameterReference : Primary
    {
        protected ParameterReference() { }
        public string ParameterName { get; protected set; }

        public override AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            AppendLabel(label);
            return label;
        }
        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(ParameterName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Parameter));
        }

        public new static ParameterReference ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            DataObjects.Constants.Constants parameter = nameSpace.GetConstants(word.Text);
            if (parameter == null) return null;

            ParameterReference val = new ParameterReference();
            val.ParameterName = word.Text;
            val.Constant = true;
            val.Reference = word.GetReference();

            word.Color(CodeDrawStyle.ColorType.Parameter);
            word.MoveNext();

            if (parameter.Expression != null) val.Value = parameter.Expression.Value;

            if (word.GetCharAt(0) == '[')
            {
                //                word.AddError("bit select can't used for parameters");
                word.MoveNext();

                Expression exp1 = Expression.ParseCreate(word, nameSpace);
                Expression exp2;
                RangeExpression range;
                switch (word.Text)
                {
                    case ":":
                        word.MoveNext();
                        exp2 = Expression.ParseCreate(word, nameSpace);
                        if (word.Text != "]")
                        {
                            word.AddError("illegal range");
                            return null;
                        }
                        word.MoveNext();
                        range = new AbsoluteRangeExpression(exp1, exp2);
                        break;
                    case "+:":
                        word.MoveNext();
                        exp2 = Expression.ParseCreate(word, nameSpace);
                        if (word.Text != "]")
                        {
                            word.AddError("illegal range");
                            return null;
                        }
                        word.MoveNext();
                        range = new RelativePlusRangeExpression(exp1, exp2);
                        break;
                    case "-:":
                        word.MoveNext();
                        exp2 = Expression.ParseCreate(word, nameSpace);
                        if (word.Text != "]")
                        {
                            word.AddError("illegal range");
                            return null;
                        }
                        word.MoveNext();
                        range = new RelativeMinusRangeExpression(exp1, exp2);
                        break;
                    case "]":
                        word.MoveNext();
                        range = new SingleBitRangeExpression(exp1);
                        break;
                    default:
                        word.AddError("illegal range/dimension");
                        return null;
                }
            }
            val.Constant = true;
            return val;
        }
    }

}
