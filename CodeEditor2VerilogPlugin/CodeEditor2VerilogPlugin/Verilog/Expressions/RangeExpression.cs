using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    //  range_expression ::=    expression        
    //                          | msb_constant_expression : lsb_constant_expression
    //                          | base_expression +: width_constant_expression
    //                          | base_expression -: width_constant_expression 
    //         | hierarchical_identifier          | hierarchical_identifier [ expression ] { [ expression ] }          | hierarchical_identifier [ expression ] { [ expression ] }  [ range_expression ]          | hierarchical_identifier [ range_expression ]  
    public class RangeExpression
    {
        public int BitWidth;
        public virtual AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            AppendLabel(label);
            return label;
        }

        public virtual void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
        }
        public virtual string CreateString()
        {
            return "";
        }

    }
    public class SingleBitRangeExpression : RangeExpression
    {
        protected SingleBitRangeExpression() { }
        public SingleBitRangeExpression(Expression expression)
        {
            Expression = expression;
            BitWidth = 1;
        }
        public Expression? Expression;
        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (Expression == null) return;
            label.AppendText("[");
            label.AppendLabel(Expression.GetLabel());
            label.AppendText("]");
        }
        public override string CreateString()
        {
            if (Expression == null) return "[?]";
            return "[" + Expression.CreateString() + "]";
        }
    }
    public class AbsoluteRangeExpression : RangeExpression
    {
        protected AbsoluteRangeExpression() { }
        public AbsoluteRangeExpression(Expression expression1, Expression expression2)
        {
            MsbExpression = expression1;
            LsbExpression = expression2;
            if (LsbExpression == null || MsbExpression == null) return;
            if(MsbExpression.Constant && LsbExpression.Constant && MsbExpression.Value != null && LsbExpression.Value != null)
            {
                BitWidth = (int)MsbExpression.Value - (int)LsbExpression.Value + 1;
            }
        }
        public Expression? MsbExpression;
        public Expression? LsbExpression;

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (LsbExpression == null || MsbExpression == null) return;
            label.AppendText("[");
            label.AppendLabel(MsbExpression.GetLabel());
            label.AppendText(":");
            label.AppendLabel(LsbExpression.GetLabel());
            label.AppendText("]");
        }
        public override string CreateString()
        {
            if (LsbExpression == null || MsbExpression == null) return "[:]";
            return "[" + MsbExpression.CreateString() +":"+ LsbExpression.CreateString() + "]";
        }
    }
    public class RelativePlusRangeExpression : RangeExpression
    {
        protected RelativePlusRangeExpression() { }
        public RelativePlusRangeExpression(Expression expression1, Expression expression2)
        {
            BaseExpression = expression1;
            WidthExpression = expression2;
            if (WidthExpression.Constant && WidthExpression.Value != null)
            {
                BitWidth = (int)WidthExpression.Value;
            }
        }
        public Expression? BaseExpression;
        public Expression? WidthExpression;

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (BaseExpression == null || WidthExpression == null) return;
            label.AppendText("[");
            label.AppendLabel(BaseExpression.GetLabel());
            label.AppendText("+:");
            label.AppendLabel(WidthExpression.GetLabel());
            label.AppendText("]");
        }
        public override string CreateString()
        {
            if (BaseExpression == null || WidthExpression == null) return "[?]";
            return "[" + BaseExpression.CreateString() + "+:" + WidthExpression.CreateString() + "]";
        }
    }
    public class RelativeMinusRangeExpression : RangeExpression
    {
        protected RelativeMinusRangeExpression() { }
        public RelativeMinusRangeExpression(Expression expression1, Expression expression2)
        {
            BaseExpression = expression1;
            WidthExpression = expression2;
            if (WidthExpression != null && WidthExpression.Constant && WidthExpression.Value != null)
            {
                BitWidth = (int)WidthExpression.Value;
            }
        }
        public Expression? BaseExpression;
        public Expression? WidthExpression;

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (BaseExpression == null || WidthExpression == null) return;
            label.AppendText("[");
            label.AppendLabel(BaseExpression.GetLabel());
            label.AppendText("-:");
            label.AppendLabel(WidthExpression.GetLabel());
            label.AppendText("]");
        }
        public override string CreateString()
        {
            if (BaseExpression == null || WidthExpression == null) return"[?]";
            return "[" + BaseExpression.CreateString() + "-:" + WidthExpression.CreateString() + "]";
        }
    }
}
