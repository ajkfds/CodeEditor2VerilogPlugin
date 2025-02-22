﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class Bracket : Primary
    {
        protected Bracket() { }

        public Expression Expression { get; protected set; }

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Expression.DisposeSubReference(false);
        }

        public override AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
            label.AppendText("(");
            if (Expression != null) label.AppendLabel(Expression.GetLabel());
            label.AppendText(")");
            return label;
        }

        public static Primary? ParseCreateBracketOrMinTypMax(WordScanner word, NameSpace nameSpace)
        {
            Bracket bracket = new Bracket();
            bracket.Reference = word.GetReference();
            word.MoveNext();
            if (word.Eof)
            {
                word.AddError("illegal bracket");
                return null;
            }
            Expression? exp1 = Expression.ParseCreateInBracket(word, nameSpace);
            if (exp1 == null)
            {
                word.AddError("illegal bracket");
                return null;
            }
            if (word.Eof)
            {
                word.AddError("illegal bracket");
                return null;
            }
            if (word.GetCharAt(0) == ':')
            {
                return MinTypMax.ParseCreate(word, nameSpace, exp1);
            }
            if (word.Eof | word.GetCharAt(0) != ')')
            {
                word.AddError("illegal bracket");
                return null;
            }
            bracket.Reference = WordReference.CreateReferenceRange(bracket.Reference, word.GetReference());
            word.MoveNext();
            bracket.Expression = exp1;
            bracket.Constant = exp1.Constant;
            bracket.BitWidth = exp1.BitWidth;
            bracket.Value = exp1.Value;
            return bracket;
        }
    }
}
