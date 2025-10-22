using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class AssignmentOperator : Operator
    {
        protected AssignmentOperator(WordScanner word, string text, byte precedence) : base(text, precedence)
        {
            Reference = word.GetReference();
            word.MoveNext();
        }
        /*
        assignment_operator ::= 
                =
                +=
                -=
                *= 
                /= 
                %= 
                &= 
                |= 
                ^= 
                <<= 
                >>= 
                <<<= 
                >>>=

        Operator                token Name                                      Operand data types
        =                       Binary assignment operator                      Any
        += -= /= *=             Binary arithmetic assignment operators          Integral, real, shortreal
        %=                      Binary arithmetic modulus assignment operator   Integral
        &= |= ^=                Binary bitwise assignment operators             Integral
        >>= <<=                 Binary logical shift assignment operators       Integral
        >>>= <<<=               Binary arithmetic shift assignment operators    Integral
        */


        /*
                <<= 
                >>= 

                <<<= 
                >>>=
         
         */

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Primary1.DisposeSubReference(false);
            Primary2.DisposeSubReference(false);
        }
        public static AssignmentOperator? ParseCreate(WordScanner word)
        {

            switch (word.Length)
            {
                case 1:
                    if (word.GetCharAt(0) == '=') { return new AssignmentOperator(word, "=", 17); }
                    return null;
                case 2:
                    if (word.GetCharAt(1) == '=')
                    {
                        if (word.GetCharAt(0) == '+') { return new AssignmentOperator(word, "+=", 17); }
                        if (word.GetCharAt(0) == '-') { return new AssignmentOperator(word, "-=", 17); }
                        if (word.GetCharAt(0) == '*') { return new AssignmentOperator(word, "*=", 17); }
                        if (word.GetCharAt(0) == '%') { return new AssignmentOperator(word, "%=", 17); }
                        if (word.GetCharAt(0) == '&') { return new AssignmentOperator(word, "&=", 17); }
                        if (word.GetCharAt(0) == '|') { return new AssignmentOperator(word, "|=", 17); }
                        if (word.GetCharAt(0) == '^') { return new AssignmentOperator(word, "^=", 17); }
                        return null;
                    }
                    else
                    {
                        return null;
                    }
                case 3:
                    if (word.GetCharAt(2) == '=')
                    {
                        if (word.GetCharAt(0) == '<' | word.GetCharAt(1) == '<') { return new AssignmentOperator(word, "<<=", 17); }
                        if (word.GetCharAt(0) == '>' | word.GetCharAt(1) == '>') { return new AssignmentOperator(word, ">>=", 17); }
                        return null;
                    }
                    else
                    {
                        return null;
                    }
                case 4:
                    if (word.GetCharAt(3) == '=')
                    {
                        if (word.GetCharAt(0) == '<' | word.GetCharAt(1) == '<' | word.GetCharAt(2) == '<') { return new AssignmentOperator(word, "<<<=", 17); }
                        if (word.GetCharAt(0) == '>' | word.GetCharAt(1) == '>' | word.GetCharAt(2) == '>') { return new AssignmentOperator(word, ">>>=", 17); }
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

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
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

        public delegate void OperatedAction(AssignmentOperator binaryOperator);
        public static OperatedAction Operated;

        public AssignmentOperator Operate(Primary primary1, Primary primary2,bool prototype)
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
            if (Primary1 != null && Primary2.Reference != null)
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
                //Binary assignment operator (Any)
                case "=":
                    return bitWidth1;

                // Binary arithmetic assignment operators (Integral, real, shortrea)
                case "+=":
                    return maxWidth + 1;
                case "-=":
                    return maxWidth;
                case "*=":
                    return bitWidth1 + bitWidth2;
                case "/=":
                    return maxWidth;

                // Binary arithmetic modulus assignment operator (Integral)
                case "%=":
                    return bitWidth2;

                // Binary bitwise assignment operators (Integral)
                case "&=":
                case "|=":
                case "^=":
                    return maxWidth;

                //Binary logical shift assignment operators (Integral)
                case "<<=":
                case ">>=":
                    return bitWidth1;

                // Binary arithmetic shift assignment operators (Integral)
                case "<<<=":
                case ">>>= ":
                    return bitWidth1;

                default:
                    return null;
            }

        }

        private double? getValue(string operatorText, double value1, double value2)
        {
            switch (Text)
            {
                //Binary assignment operator (Any)
                case "=":
                    return value2;
                // Binary arithmetic assignment operators (Integral, real, shortrea)
                case "+=":
                    return value1 + value2;
                case "-=":
                    return value1 - value2;
                case "*=":
                    return value1 * value2;
                case "/=":
                    return value1 / value2;

                // Binary arithmetic modulus assignment operator (Integral)
                case "%=":
                    return value1 % value2;

                // Binary bitwise assignment operators (Integral)
                case "&=":
                    {
                        ulong lng1 = (ulong)value1;
                        ulong lng2 = (ulong)value2;
                        return lng1 & lng2;
                    }
                case "|=":
                    {
                        ulong lng1 = (ulong)value1;
                        ulong lng2 = (ulong)value2;
                        return lng1 | lng2;
                    }
                case "^=":
                    {
                        ulong lng1 = (ulong)value1;
                        ulong lng2 = (ulong)value2;
                        return lng1 ^ lng2;
                    }

                //Binary logical shift assignment operators (Integral)
                case "<<=":
                case ">>=":
                    return null;

                // Binary arithmetic shift assignment operators (Integral)
                case "<<<=":
                case ">>>= ":
                    return null;

                default:
                    return null;
            }
        }


    }
}

