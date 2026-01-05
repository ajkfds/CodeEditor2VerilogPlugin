using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class UnPackedArray : VariableArray, IArray
    {
        protected UnPackedArray() { }
        public static UnPackedArray? ParseCreate(WordScanner word, NameSpace nameSpace, Expressions.Expression expression)
        {
            // unpacked_dimension   ::= [constant_range] | [constant_expression]
            // constant_range       ::= constant_expression : constant_expression

            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression);
            }

            if (word.Text != ":")
            {
                word.AddError(": expected");
                return null;
            }
            word.MoveNext();

            Expressions.Expression? expression1 = Expressions.Expression.ParseCreate(word, nameSpace, false);
            if (expression1 == null)
            {
                word.AddError("expression expected");
                return null;
            }
            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression, expression1);
            }

            return null;
        }
        public static UnPackedArray? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // unpacked_dimension   ::= [constant_range] | [constant_expression]
            // constant_range       ::= constant_expression : constant_expression

            if (word.Text != "[") return null;
            word.MoveNext();

            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if (expression == null)
            {
                word.AddError("illegal unpacked array");
                return null;
            }

            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression);
            }

            if (word.Text != ":")
            {
                word.AddError(": expected");
                return null;
            }
            word.MoveNext();

            Expressions.Expression? expression1 = Expressions.Expression.ParseCreate(word, nameSpace, false);
            if (expression1 == null)
            {
                word.AddError("expression expected");
                return null;
            }
            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression, expression1);
            }

            return null;
        }
        public UnPackedArray(Expressions.Expression widthExpression) 
        {
            if (widthExpression == null) return;

            SizeExpression0 = widthExpression;
            if (!SizeExpression0.Constant) return;

            double? width = widthExpression.Value;
            if(width != null)
            {
                Size = (int)width;
                MinIndex = 0;
                MaxIndex = Size - 1;
            }
        }
        public UnPackedArray(Expressions.Expression expression0, Expressions.Expression expression1) 
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

        public int? MaxIndex = null;
        public int? MinIndex = null;
        public Expressions.Expression? SizeExpression0 { get; protected set; }
        public Expressions.Expression? SizeExpression1 { get; protected set; }

        public override string CreateString()
        {
            if (SizeExpression0 == null) return "";

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(SizeExpression0.CreateString());
            if(SizeExpression1 != null)
            {
                sb.Append(":");
                sb.Append(SizeExpression1.CreateString());
            }
            sb.Append("]");
            return sb.ToString();
        }

        public UnPackedArray Clone()
        {
            UnPackedArray unPackedArray = new UnPackedArray() { SizeExpression0 = SizeExpression0, SizeExpression1 = SizeExpression1, MinIndex = MinIndex, MaxIndex = MaxIndex,Size = Size };
            return unPackedArray;
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
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
        public override AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            AppendLabel(label);
            return label;
        }
        public override bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            if (!indexExpression.Constant) return false;
            if (indexExpression.Value == null) return false;
            int index = (int)indexExpression.Value;

            if ( MaxIndex != null && index > MaxIndex) return true;
            if (MinIndex != null && index < MinIndex) return true;
            return false;
        }

    }
}
