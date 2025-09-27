using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextMateSharp.Grammars;

namespace pluginVerilog.Verilog.Expressions
{
    public class IncOrDecExpression : IStatement
    {
        protected IncOrDecExpression() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        void IStatement.DisposeSubReference()
        {
        }
        public DataObjectReference? DataObjectReference { get; set; }
        public bool Increment = false;
        public required WordReference WordReference { get; init; }
        public static IncOrDecExpression? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            if (!word.SystemVerilog) return null;
            if (word.Text != "++" && word.Text != "--" && word.NextText != "++" && word.NextText != "--") return null;
            // inc_or_dec_expression::=   inc_or_dec_operator { attribute_instance } variable_lvalue
            //                          | variable_lvalue { attribute_instance } inc_or_dec_operator
            // inc_or_dec_operator ::= ++ | --

            DataObjectReference? dataObjectReference = null;
            bool increment = false;
            WordReference wref;

            if (word.Text =="++" || word.Text == "--")
            {
                wref = word.GetReference();
                if (word.Text == "++")
                {
                    increment = true;
                }
                else if (word.Text == "--")
                {
                    increment = false;
                }
                else
                {
                    return null;
                }
                word.MoveNext();
                Primary? primary = Primary.ParseCreate(word, nameSpace, acceptImplicitNet);
                if (primary != null) wref = WordReference.CreateReferenceRange(wref, primary.Reference);

                if (primary is DataObjectReference)
                {
                    dataObjectReference = (DataObjectReference)primary;
                }
                else if(primary==null)
                {
                    word.MoveNext();
                    if(!word.Prototype) word.AddError("illegal inc_or_dec_expression");
                }
                else
                {
                    word.AddError("illegal inc_or_dec_expression");
                }
            }
            else if(word.NextText=="++" || word.NextText == "--")
            {
                wref = word.GetReference();
                Primary? primary = Primary.ParseCreate(word, nameSpace, acceptImplicitNet);
                if (primary is DataObjectReference)
                {
                    dataObjectReference = (DataObjectReference)primary;
                }
                else if (primary == null)
                {
                    word.MoveNext();
                    if (!word.Prototype) word.AddError("illegal inc_or_dec_expression");
                }
                else
                {
                    word.AddError("illegal inc_or_dec_expression");
                }

                if (word.Text == "++")
                {
                    increment = true;
                }else if (word.Text == "--")
                {
                    increment = false;
                }
                else
                {
                    return null;
                }
                if (primary != null) wref = WordReference.CreateReferenceRange(wref, word.GetReference());
                word.MoveNext();
            }
            else
            {
                return null;
            }
            return new IncOrDecExpression() { WordReference = wref, Increment = increment, DataObjectReference = dataObjectReference };
        }

    }
}
