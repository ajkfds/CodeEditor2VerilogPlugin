using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class UnaryOperator : Operator
    {
        protected UnaryOperator(WordScanner word, string text, byte precedence) : base(text, precedence)
        {
            Reference = word.GetReference();
            word.MoveNext();
        }
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

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(Text);
            Primary.AppendLabel(label);
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append(Text);
            Primary.AppendString(stringBuilder);
        }

        public static UnaryOperator? ParseCreate(WordScanner word)
        {
            switch (word.Length)
            {
                case 1:
                    if (word.GetCharAt(0) == '+') { return new UnaryOperator(word, "+", 3); }
                    if (word.GetCharAt(0) == '-') { return new UnaryOperator(word, "-", 3); }
                    if (word.GetCharAt(0) == '!') { return new UnaryOperator(word, "!", 3); }
                    if (word.GetCharAt(0) == '~') { return new UnaryOperator(word, "~", 3); }
                    if (word.GetCharAt(0) == '&') { return new UnaryOperator(word, "&", 3); }
                    if (word.GetCharAt(0) == '|') { return new UnaryOperator(word, "|", 3); }
                    if (word.GetCharAt(0) == '^') { return new UnaryOperator(word, "^", 3); }
                    return null;
                case 2:
                    if (word.GetCharAt(0) == '~')
                    {
                        if (word.GetCharAt(1) == '^') { return new UnaryOperator(word, "~^", 3); }
                        if (word.GetCharAt(1) == '&') { return new UnaryOperator(word, "~&", 3); }
                        if (word.GetCharAt(1) == '|') { return new UnaryOperator(word, "~|", 3); }
                        return null;
                    }
                    else if (word.GetCharAt(0) == '^')
                    {
                        if (word.GetCharAt(1) == '~') { return new UnaryOperator(word, "^~", 3); }
                        return null;
                    }
                    else
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        public delegate void OperatedAction(UnaryOperator unaryOperator);
        public static OperatedAction? Operated;

        public UnaryOperator Operate(Primary primary)
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
            switch (text)
            {
                // arithmetic operators
                case "+":
                    return value;
                case "-":
                    return -value;

                // logical negation
                case "!":
                    return null;

                // reduction operators
                case "&":
                case "|":
                case "^":
                case "~^":
                case "~&":
                case "~|":
                case "^~":
                    return null;

                default:
                    return null;
            }
        }

        private int? getBitWidth(string text, int bitWidth)
        {
            switch (text)
            {
                // arithmetic operators
                case "+":
                case "-":
                    return bitWidth + 1;

                // logical negation
                case "!":
                    return bitWidth;

                // reduction operators
                case "&":
                case "|":
                case "^":
                case "~^":
                case "~&":
                case "~|":
                case "^~":
                    return 1;
                default:
                    return null;
            }
        }

    }
}
