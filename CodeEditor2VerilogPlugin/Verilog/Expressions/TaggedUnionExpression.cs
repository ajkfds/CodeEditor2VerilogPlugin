using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    /// <summary>
    /// SystemVerilog A.8.4 tagged_union_expression
    /// tagged_union_expression ::= tagged [ unique ] union_member_identifier [ ( expression ) ] { . union_member_identifier [ ( expression ) ] }
    /// e.g. tagged Invalid / tagged Valid(42)
    /// </summary>
    public class TaggedUnionExpression : Primary
    {
        internal TaggedUnionExpression() { }

        public Expression? MemberExpression { get; protected set; }
        public string? MemberIdentifier { get; protected set; }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("tagged", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (MemberIdentifier != null) label.AppendText(" " + MemberIdentifier, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            if (MemberExpression != null)
            {
                label.AppendText("(");
                label.AppendLabel(MemberExpression.GetLabel());
                label.AppendText(")");
            }
        }

        public override string CreateString()
        {
            string ret = "tagged";
            if (MemberIdentifier != null) ret += " " + MemberIdentifier;
            if (MemberExpression != null) ret += "(" + MemberExpression.CreateString() + ")";
            return ret;
        }

        /// <summary>
        /// Parse "tagged [ unique ] union_member_identifier [ ( expression ) ] { . union_member_identifier [ ( expression ) ] }"
        /// </summary>
        public static TaggedUnionExpression? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // "tagged"

            TaggedUnionExpression tagged = new TaggedUnionExpression();

            if (word.Text == "unique")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("union member identifier expected");
                return null;
            }

            tagged.MemberIdentifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            if (word.GetCharAt(0) == '(')
            {
                word.MoveNext(); // (
                Expression? exp = Expression.ParseCreate(word, nameSpace);
                if (exp == null)
                {
                    word.AddError("illegal tagged union member expression");
                    if (word.Eof) return null;
                }
                tagged.MemberExpression = exp;
                if (word.GetCharAt(0) != ')')
                {
                    word.AddError(") expected");
                    while (!word.Eof && word.GetCharAt(0) != ')' && word.Text != ";") word.MoveNext();
                }
                if (word.GetCharAt(0) == ')') word.MoveNext();
            }

            // { . union_member_identifier [ ( expression ) ] }
            while (word.GetCharAt(0) == '.')
            {
                word.MoveNext();
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("union member identifier expected");
                    break;
                }
                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();
                if (word.GetCharAt(0) == '(')
                {
                    word.MoveNext();
                    Expression? exp = Expression.ParseCreate(word, nameSpace);
                    if (exp == null)
                    {
                        word.AddError("illegal tagged union member expression");
                        if (word.Eof) return null;
                    }
                    if (word.GetCharAt(0) != ')')
                    {
                        word.AddError(") expected");
                        while (!word.Eof && word.GetCharAt(0) != ')' && word.Text != ";") word.MoveNext();
                    }
                    if (word.GetCharAt(0) == ')') word.MoveNext();
                }
            }

            tagged.Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference());
            return tagged;
        }
    }
}
