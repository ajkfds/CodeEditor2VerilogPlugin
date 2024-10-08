using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class PackedArray
    {
        public PackedArray(Expressions.Expression? expression0, Expressions.Expression? expression1)
        {
            if (expression0 == null || expression1 == null) return;

            SizeExpression0 = expression0;
            SizeExpression1 = expression1;
            if (!SizeExpression0.Constant || !SizeExpression1.Constant) return;

            double? max = expression0.Value;
            double? min = expression1.Value;
            if (max == null || min == null) return;

            if (min > max)
            {
                double? temp = max;
                max = min;
                min = temp;
            }
            MaxIndex = (int)max;
            MinIndex = (int)min;
            Size = (int)max - (int)min + 1;
        }

        public PackedArray Clone()
        {
            PackedArray pArray = new(SizeExpression0, SizeExpression1);
            return pArray;
        }

        //public PackedArray(int value0,int value1) :this(
        //    Expressions.Expression.CreateTempExpression(value0.ToString()),
        //    Expressions.Expression.CreateTempExpression(value1.ToString())
        //    )
        //{
        //}
        //public PackedArray(string value0, string value1) : this(
        //    Expressions.Expression.CreateTempExpression(value0),
        //    Expressions.Expression.CreateTempExpression(value1)
        //    )
        //{
        //}
        public int? MaxIndex = null;
        public int? MinIndex = null;
        public Expressions.Expression? SizeExpression0 { get; protected set; }
        public Expressions.Expression? SizeExpression1 { get; protected set; }
        public int? Size { get; protected set; } = null;

        public string CreateString()
        {
            if (SizeExpression0 == null) return "";

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(SizeExpression0.CreateString());
            if (SizeExpression1 != null)
            {
                sb.Append(":");
                sb.Append(SizeExpression1.CreateString());
            }
            sb.Append("]");
            return sb.ToString();
        }

        public void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            if (SizeExpression0 == null) return;
            label.AppendText("[");
            label.AppendLabel(SizeExpression0.GetLabel());
            if (SizeExpression1 != null)
            {
                label.AppendText(":");
                label.AppendLabel(SizeExpression1.GetLabel());
            }
            label.AppendText("]");
        }
        public AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Contorls.ColorLabel label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            AppendLabel(label);
            return label;
        }
        public bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            if (!indexExpression.Constant) return false;
            if (indexExpression.Value == null) return false;
            int index = (int)indexExpression.Value;

            if (MaxIndex != null && index > MaxIndex) return true;
            if (MinIndex != null && index < MinIndex) return true;
            return false;
        }

        public static PackedArray? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            word.MoveNext(); // [

            Expressions.Expression? expression = Expressions.Expression.parseCreate(word, nameSpace, false);

            if (expression == null) return null;
            if (word.Text != ":")
            {
                word.AddError(": expected");
                return null;
            }
            word.MoveNext();

            Expressions.Expression? expression1 = Expressions.Expression.parseCreate(word, nameSpace, false);
            if (expression1 == null)
            {
                word.AddError("expression expected");
                return null;
            }
            if (word.Text == "]")
            {
                word.MoveNext();
                return new PackedArray(expression, expression1);
            }
            return null;
        }

    }
}
