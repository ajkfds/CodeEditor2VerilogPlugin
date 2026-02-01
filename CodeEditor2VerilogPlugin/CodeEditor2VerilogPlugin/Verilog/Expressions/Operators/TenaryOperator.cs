using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class TenaryOperator : Operator
    {
        protected TenaryOperator(string text, byte precedence) : base(text, precedence) { }

        public static TenaryOperator Create()
        {
            return new TenaryOperator("?", 0);
        }

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Condition.DisposeSubReference(false);
            Primary1.DisposeSubReference(false);
            Primary2.DisposeSubReference(false);
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            Condition.AppendLabel(label);
            label.AppendText(" ? ");
            Primary1.AppendLabel(label);
            label.AppendText(" : ");
            Primary2.AppendLabel(label);
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            Condition.AppendString(stringBuilder);
            stringBuilder.Append(" ? ");
            Primary1.AppendString(stringBuilder);
            stringBuilder.Append(" : ");
            Primary2.AppendString(stringBuilder);
        }

        public delegate void OperatedAction(TenaryOperator tenaryOperator);
        public static OperatedAction Operated;

        public Primary Condition;
        public Primary Primary1;
        public Primary Primary2;
        public TenaryOperator Operate(Primary condition, Primary primary1, Primary primary2, bool prototype)
        {
            Condition = condition;
            Primary1 = primary1;
            Primary2 = primary2;

            bool constant = false;
            double? value = null;
            int? bitWidth = null;

            if (primary1.Constant && primary2.Constant & condition.Constant) constant = true;
            if (condition.Value != null && primary1.Value != null && primary2.Value != null) value = getValue((double)condition.Value, (double)primary1.Value, (double)primary2.Value);
            if (primary1.BitWidth != null && primary2.BitWidth != null) bitWidth = getBitWidth(Text, (int)primary1.BitWidth, (int)primary2.BitWidth);

            Constant = constant;
            Value = value;
            BitWidth = bitWidth;
            if (Primary2.Reference != null && Condition.Reference != null)
            {
                Reference = WordReference.CreateReferenceRange(Condition.Reference, Primary2.Reference);
            }
            if (Operated != null) Operated(this);
            return this;
        }

        private int? getBitWidth(string operatorText, int bitWidth1, int bitWidth2)
        {
            int maxWidth = bitWidth1;
            if (bitWidth2 > bitWidth1) maxWidth = bitWidth2;


            return bitWidth1;
        }

        private double? getValue(double condition, double value1, double value2)
        {
            if (condition != 0)
            {
                return value1;
            }
            else
            {
                return value2;
            }
        }
    }
}
