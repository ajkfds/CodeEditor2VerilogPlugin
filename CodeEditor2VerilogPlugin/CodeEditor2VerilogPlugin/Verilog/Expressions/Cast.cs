using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class Cast : Primary
    {
        protected Cast() { }

        public Expression Expression { get; protected set; }

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Expression.DisposeSubReference(false);
        }

        public override AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Contorls.ColorLabel label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            label.AppendText("(");
            if (Expression != null) label.AppendLabel(Expression.GetLabel());
            label.AppendText(")");
            return label;
        }

// constant_cast    ::= casting_type ' ( constant_expression )
// cast             ::=  casting_type ' ( expression )

// casting_type     ::= simple_type | constant_primary | signing | "string" | "const"
// simple_type      ::= integer_type | non_integer_type | ps_type_identifier | ps_parameter_identifier
        public static Primary? ParseCreate(WordScanner word, NameSpace nameSpace,Number number)
        {
            Cast cast = new Cast();
            cast.Reference = number.Reference;
            word.MoveNext();
            if (word.Eof || word.Text != "(")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            Expression? exp1 = Expression.ParseCreate(word, nameSpace);
            if (word.Eof || exp1 == null || word.Text != ")")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            cast.Reference = WordReference.CreateReferenceRange(cast.Reference, word.GetReference());
            cast.Expression = exp1;
            cast.Constant = exp1.Constant;
            cast.BitWidth = (int?)number.Value;
            cast.Value = exp1.Value;

            if(cast.BitWidth != null && cast.Value != null)
            {
                if (cast.Value > Math.Pow(2, (double)cast.BitWidth)-1) cast.Reference.AddError("value overflow");
            }
            return cast;
        }

        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Cast cast = new Cast();
            cast.Reference = word.CrateWordReference();

            Verilog.INamedElement? namedElement = nameSpace.NamedElements.GetDataObject(word.Text);
            if(namedElement == null)
            {
                word.AddError("illegal cast");
                return null;
            }
            word.Color(namedElement.ColorType);
            word.MoveNext();
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Eof || word.Text!="(")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            Expression? exp1 = Expression.ParseCreate(word, nameSpace);
            if (word.Eof || exp1 == null || word.Text!=")")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            cast.Reference = WordReference.CreateReferenceRange(cast.Reference, word.GetReference());
            cast.Expression = exp1;
            cast.Constant = exp1.Constant;
            cast.BitWidth = exp1.BitWidth;
            cast.Value = exp1.Value;
            return cast;
        }
    }
}
