using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class InsideOperator : Operator
    {
        protected InsideOperator(WordScanner word, string text, byte precedence, NameSpace nameSpace) : base(text, precedence)
        {
            if (word.Text != "inside") throw new Exception();
            Reference = word.GetReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.Eof || word.Text != "{")
            {
                word.AddError("{ expected");
                return;
            }
            word.MoveNext();

            while (!word.Eof)
            {
                Expression? exp = Expression.ParseCreate(word, nameSpace);
                if (exp == null)
                {
                    DataObjects.Arrays.PackedArray ? range = DataObjects.Arrays.PackedArray.ParseCreate(word, nameSpace);
                    if (range == null) break;
                    Ranges.Add(range);
                }
                else
                {
                    Expressions.Add(exp);
                }
                if (word.Text != ",") break;
                word.MoveNext();
            }


            if (word.Eof || word.Text != "}")
            {
                word.AddError("} expected");
                return;
            }
            word.MoveNext();
        }

        public List<Expression> Expressions = new List<Expression>();
        public List<DataObjects.Arrays.PackedArray> Ranges = new List<DataObjects.Arrays.PackedArray>();
        /*
        unary_operator  ::=   + 
                            | - 
                            | ! 
                            | ~ 
                            | & 
                            | | 
                            | ^ 
                            | ~^ 
                            | ~& 
                            | ~| 
                            | ^~
        */

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Primary.DisposeSubReference(false);
        }

        public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            label.AppendText(Text);
            Primary.AppendLabel(label);
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(Text);
            Primary.AppendString(stringBuilder);
        }

        public static new InsideOperator? ParseCreate(WordScanner word,NameSpace nameSpace)
        {
            if (word.Text != "inside") return null;
            return new InsideOperator(word, "inside", 8, nameSpace);
        }

        public delegate void OperatedAction(InsideOperator unaryOperator);
        public static OperatedAction? Operated;

        public InsideOperator Operate(Primary primary)
        {
            Primary = primary;
            bool constant = false;
            double? value = null;
            int? bitWidth = null;

            if (primary.Constant) constant = true;
            if (primary.Value != null) value = getValue(Text, (double)primary.Value);
            if (primary.BitWidth != null) bitWidth = getBitWidth(Text, (int)primary.BitWidth);

            Constant = constant;
            Value = value;
            BitWidth = BitWidth;
            if (Primary.Reference != null)
            {
                Reference = Primary.Reference;
            }
            if (Operated != null) Operated(this);
            return this;
        }

        private double? getValue(string text, double value)
        {
            return null;
        }

        private int getBitWidth(string text, int bitWidth)
        {
            return 1;
        }

    }
}
