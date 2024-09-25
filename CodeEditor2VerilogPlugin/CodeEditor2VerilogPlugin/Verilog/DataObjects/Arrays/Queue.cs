using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class Queue : VariableArray
    {
        public Queue(Expressions.Expression? maxSizeExpression)
        {
            this.MaxSizeExpression = maxSizeExpression;
        }
        public Expressions.Expression? MaxSizeExpression { get; protected set; }

        public override bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            return false;
        }
        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ $");
            if (MaxSizeExpression != null)
            {
                sb.Append(":");
                sb.Append(MaxSizeExpression.CreateString());
            }
            sb.Append(" ]");
            return sb.ToString();
        }

        public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            label.AppendText("[ $");
            if (MaxSizeExpression != null)
            {
                label.AppendText(": ");
                label.AppendLabel(MaxSizeExpression.GetLabel());
            }
            label.AppendText(" ]");
        }
        public override AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Contorls.ColorLabel label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            AppendLabel(label);
            return label;
        }

        public static new Queue? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // queue_dimension          ::= [ $ [ : constant_expression] ]

            if (word.Text != "$") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // $

            if (word.Text == "]")
            {
                word.MoveNext(); // ]
                return new Queue(null);
            }

            if (word.Text == ":")
            {
                word.MoveNext();
                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (word.Text == "]")
                {
                    word.MoveNext(); // ]
                    return new Queue(expression);
                }
                else
                {
                    word.AddError("] expected");
                    return new Queue(null);
                }
            }
            word.AddError("] expected");
            return null;
        }

    }


}
