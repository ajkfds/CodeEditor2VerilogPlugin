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

        public static Primary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Cast cast = new Cast();
            cast.Reference = word.CrateWordReference();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Eof || word.Text!="(")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            Expression exp1 = Expression.ParseCreate(word, nameSpace);
            if (word.Eof || exp1 == null || word.Text!=")")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            cast.Reference = WordReference.CreateReferenceRange(cast.Reference, word.GetReference());
            word.MoveNext();
            cast.Expression = exp1;
            cast.Constant = exp1.Constant;
            cast.BitWidth = exp1.BitWidth;
            cast.Value = exp1.Value;
            return cast;
        }
    }
}
