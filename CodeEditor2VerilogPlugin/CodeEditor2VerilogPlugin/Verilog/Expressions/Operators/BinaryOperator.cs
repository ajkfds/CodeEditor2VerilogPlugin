using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class BinaryOperator : Operator
    {
        protected BinaryOperator(WordScanner word, string text, byte precedence) : base(text, precedence)
        {
            Reference = word.GetReference();
            word.MoveNext();
        }
        /*
        binary_operator ::=   + 
                            | - 
                            | * 
                            | / 
                            | %
                            | < 
                            | > 
                            | & 
                            | | 
                            | ^ 

                            | == 
                            | != 
                            | <= 
                            | >= 

                            | && 
                            | || 
                            | **
                            | >> 
                            | << 
                            | ^~ 
                            | ~^ 
                            | === 
                            | !== 
                            | >>> 
                            | <<< 
        */

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Primary1.DisposeSubReference(false);
            Primary2.DisposeSubReference(false);
        }
        public static BinaryOperator ParseCreate(WordScanner word)
        {

            switch (word.Length)
            {
                case 1:
                    if (word.GetCharAt(0) == '+') { return new BinaryOperator(word, "+", 6); }
                    if (word.GetCharAt(0) == '-') { return new BinaryOperator(word, "-", 6); }
                    if (word.GetCharAt(0) == '*') { return new BinaryOperator(word, "*", 5); }
                    if (word.GetCharAt(0) == '/') { return new BinaryOperator(word, "/", 5); }
                    if (word.GetCharAt(0) == '%') { return new BinaryOperator(word, "%", 5); }
                    if (word.GetCharAt(0) == '<') { return new BinaryOperator(word, "<", 8); }
                    if (word.GetCharAt(0) == '>') { return new BinaryOperator(word, ">", 8); }
                    if (word.GetCharAt(0) == '&') { return new BinaryOperator(word, "&", 10); }
                    if (word.GetCharAt(0) == '|') { return new BinaryOperator(word, "|", 12); }
                    if (word.GetCharAt(0) == '^') { return new BinaryOperator(word, "^", 11); }
                    if (word.GetCharAt(0) == '\'') { return new BinaryOperator(word, "\'", 2); }
                    return null;
                case 2:
                    if (word.GetCharAt(1) == '=')
                    {
                        if (word.GetCharAt(0) == '=') { return new BinaryOperator(word, "==", 9); }
                        if (word.GetCharAt(0) == '!') { return new BinaryOperator(word, "!=", 9); }
                        if (word.GetCharAt(0) == '<') { return new BinaryOperator(word, "<=", 8); }
                        if (word.GetCharAt(0) == '>') { return new BinaryOperator(word, ">=", 8); }
                        return null;
                    }
                    else if (word.GetCharAt(0) == word.GetCharAt(1))
                    {
                        if (word.GetCharAt(0) == '&') { return new BinaryOperator(word, "&&", 13); }
                        if (word.GetCharAt(0) == '|') { return new BinaryOperator(word, "||", 14); }
                        if (word.GetCharAt(0) == '*') { return new BinaryOperator(word, "**", 4); }
                        if (word.GetCharAt(0) == '>') { return new BinaryOperator(word, ">>", 7); }
                        if (word.GetCharAt(0) == '<') { return new BinaryOperator(word, "<<", 7); }
                        if (word.GetCharAt(0) == '^' && word.GetCharAt(1) == '~') { return new BinaryOperator(word, "^~", 11); }
                        if (word.GetCharAt(0) == '~' && word.GetCharAt(1) == '^') { return new BinaryOperator(word, "~^", 11); }
                        return null;
                    }
                    if (word.GetCharAt(1) == '>')
                    {
                        if (word.GetCharAt(0) == '-') { return new BinaryOperator(word, "->", 16); }
                        return null;
                    }
                    else
                    {
                        return null;
                    }
                case 3:
                    if (word.GetCharAt(1) != word.GetCharAt(2))
                    {
                        if (word.GetCharAt(0) == '=' | word.GetCharAt(1) == '=' | word.GetCharAt(2) == '?') { return new BinaryOperator(word, "==?", 9); }
                        if (word.GetCharAt(0) == '!' | word.GetCharAt(1) == '=' | word.GetCharAt(2) == '?') { return new BinaryOperator(word, "!=?", 9); }
                        if (word.GetCharAt(0) == '<' | word.GetCharAt(1) == '-' | word.GetCharAt(2) == '>') { return new BinaryOperator(word, "<->", 16); }
                        return null;
                    }
                    if (word.GetCharAt(1) == '=')
                    {
                        if (word.GetCharAt(0) == '=') { return new BinaryOperator(word, "===", 9); }
                        if (word.GetCharAt(0) == '!') { return new BinaryOperator(word, "!==", 9); }
                        return null;
                    }
                    else if (word.GetCharAt(0) == word.GetCharAt(1))
                    {
                        if (word.GetCharAt(0) == '>') { return new BinaryOperator(word, ">>>", 7); }
                        if (word.GetCharAt(0) == '<') { return new BinaryOperator(word, "<<<", 7); }
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

        public override void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            Primary1.AppendLabel(label);
            label.AppendText(Text);
            Primary2.AppendLabel(label);
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            Primary1.AppendString(stringBuilder);
            stringBuilder.Append(Text);
            Primary2.AppendString(stringBuilder);
        }


        Primary Primary1;
        Primary Primary2;

        public delegate void OperatedAction(BinaryOperator binaryOperator);
        public static OperatedAction Operated;

        public BinaryOperator Operate(Primary primary1, Primary primary2)
        {
            Primary1 = primary1;
            Primary2 = primary2;

            bool constant = false;
            double? value = null;
            int? bitWidth = null;

            if (primary1.Constant && primary2.Constant) constant = true;
            if (primary1.Value != null && primary2.Value != null) value = getValue(Text, (double)primary1.Value, (double)primary2.Value);
            if (primary1.BitWidth != null && primary2.BitWidth != null) bitWidth = getBitWidth(Text, (int)primary1.BitWidth, (int)primary2.BitWidth);

            Constant = constant;
            Value = value;
            BitWidth = bitWidth;
            Primary ret = this;
            if (Primary1 != null && Primary1.Reference != null && Primary2 != null && Primary2.Reference != null)
            {
                Reference = WordReference.CreateReferenceRange(Primary1.Reference, Primary2.Reference);
            }
            //            Primary ret = Primary.Create(constant, value, bitWidth);
            if (Operated != null) Operated(this);
            return this;
        }


        private int? getBitWidth(string operatorText, int bitWidth1, int bitWidth2)
        {
            int maxWidth = bitWidth1;
            if (bitWidth2 > bitWidth1) maxWidth = bitWidth2;

            switch (operatorText)
            {
                // arithmetic operator
                case "+":
                    return maxWidth + 1;
                case "-":
                    return maxWidth;
                case "*":
                    return bitWidth1 + bitWidth2;
                case "/":
                    return maxWidth;
                case "**":
                    return bitWidth1 * bitWidth2;

                // modulus operator
                case "%":
                    return bitWidth2;

                // relational operators
                case "<":
                case ">":
                case "<=":
                case ">=":
                    return 1;
                // equality operators
                case "==":
                case "!=":
                case "===":
                case "!==":
                    return 1;
                // logical operators
                case "&&":
                case "||":
                    return 1;

                // bit-wise binary operators
                case "&":
                case "|":
                case "^":
                case "^~":
                case "~^":
                    return maxWidth;

                // logical shift
                case ">>":
                case "<<":
                    return bitWidth1;
                // arithmetic shift
                case ">>>":
                case "<<<":
                    return bitWidth1;

                // cast
                case "\'":
                    return bitWidth1;

                default:
                    return null;
            }

        }

        private double? getValue(string operatorText, double value1, double value2)
        {
            switch (Text)
            {
                // arithmetic operator
                case "+":
                    return value1 + value2;
                case "-":
                    return value1 - value2;
                case "*":
                    return value1 * value2;
                case "/":
                    return value1 / value2;
                case "**":
                    return Math.Pow(value1, value2);

                // modulus operator
                case "%":
                    return value1 % value2;

                // relational operators
                case "<":
                    return value1 < value2 ? 1 : 0;
                case ">":
                    return value1 > value2 ? 1 : 0;
                case "<=":
                    return value1 <= value2 ? 1 : 0;
                case ">=":
                    return value1 >= value2 ? 1 : 0;

                // equality operators
                case "==":
                    return value1 == value2 ? 1 : 0;
                case "!=":
                    return value1 != value2 ? 1 : 0;
                case "===":
                    return value1 == value2 ? 1 : 0;
                case "!==":
                    return value1 != value2 ? 1 : 0;

                // logical operators
                case "&&":
                    return value1 != 0 & value2 != 0 ? 1 : 0;
                case "||":
                    return value1 != 0 | value2 != 0 ? 1 : 0;

                // bit-wise binary operators
                case "&":
                    {
                        ulong lng1 = (ulong)value1;
                        ulong lng2 = (ulong)value2;
                        return lng1 & lng2;
                    }
                case "|":
                    {
                        ulong lng1 = (ulong)value1;
                        ulong lng2 = (ulong)value2;
                        return lng1 | lng2;
                    }
                case "^":
                case "^~":
                case "~^":

                // logical shift
                case ">>":
                case "<<":
                // arithmetic shift
                case ">>>":
                case "<<<":
                    return null;

                // cast
                case "\'":
                    return value1;
                default:
                    return null;
            }
        }


    }
}
