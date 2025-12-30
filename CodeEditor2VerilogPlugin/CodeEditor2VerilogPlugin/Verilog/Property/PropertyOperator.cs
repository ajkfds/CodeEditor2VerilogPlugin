using pluginVerilog.Verilog.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Property
{
    public class PropertyOperator : PropertyPrimary
    {
        public static new PropertyOperator? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // precedence
            // 6            not, nexttime, s_nexttime     
            // 5            and
            // 4            or
            // 3            iff
            // 2            until, s_until, until_with, s_until_with, implies
            // 1            |->, |=>, #-#, #=#
            // 0            always, s_always, eventually, s_eventually, if-else, case, accept_on, reject_on, sync_accept_on, sync_reject_on

            switch (word.Text)
            {
                case "not":
                case "nexttime":
                case "s_nexttime":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 6);
                case "and":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 5);
                case "or":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 4);
                case "iff":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 3);
                case "until":
                case "s_until":
                case "until_with":
                case "s_until_with":
                case "implies":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 2);
                case "|->":
                case "|=>":
                case "#-#":
                case "#=#":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 1);
                case "always":
                case "s_always":
                case "eventually":
                case "s_eventually":
                case "if-else":
                case "case":
                case "accept_on":
                case "reject_on":
                case "sync_accept_on":
                case "sync_reject_on":
                    word.MoveNext();
                    return new PropertyOperator(word.Text, 0);
            }
            return null;
        }
        protected PropertyOperator(string text, byte precedence)
        {
            Text = text;
            Precedence = precedence;
        }

        public readonly string Text = "";
        public readonly byte Precedence;
    }
}
