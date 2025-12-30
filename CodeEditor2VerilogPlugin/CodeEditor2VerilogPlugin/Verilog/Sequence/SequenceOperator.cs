using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Sequence
{
    public class SequenceOperator : SequencePrimary
    {
        public static new SequenceOperator? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            //                  precedence
            // [*],[=],[->]     6
            // ##               5
            // throughout       4
            // within           3
            // intersect        2
            // and              1
            // or               0
            switch (word.Text)
            {
                case "[*]":
                    word.MoveNext();
                    return new SequenceOperator("[*]", 6);
                case "[=]":
                    word.MoveNext();
                    return new SequenceOperator("[=]", 6);
                case "[->]":
                    word.MoveNext();
                    return new SequenceOperator("[->]", 6);
                case "##":
                    word.MoveNext();
                    return new SequenceOperator("##", 5);
                case "throughout":
                    word.MoveNext();
                    return new SequenceOperator("throughout", 4);
                case "within":
                    word.MoveNext();
                    return new SequenceOperator("within", 3);
                case "intersect":
                    word.MoveNext();
                    return new SequenceOperator("intersect", 2);
                case "and":
                    word.MoveNext();
                    return new SequenceOperator("and", 1);
                case "or":
                    word.MoveNext();
                    return new SequenceOperator("or", 0);
            }
            return null;
        }
        protected SequenceOperator(string text, byte precedence)
        {
            Text = text;
            Precedence = precedence;
        }

        public readonly string Text = "";
        public readonly byte Precedence;

    }
}

