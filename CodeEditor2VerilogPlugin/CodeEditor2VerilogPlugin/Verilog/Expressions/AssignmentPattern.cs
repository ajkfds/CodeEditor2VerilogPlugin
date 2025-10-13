using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class AssignmentPattern : Primary
    {
        /*
        assignment_pattern ::=
              '{ expression { , expression } }
            | '{ structure_pattern_key : expression { , structure_pattern_key : expression } }
            | '{ array_pattern_key : expression { , array_pattern_key : expression } }
            | '{ constant_expression { expression { , expression } } }
        */
        public static Primary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Text != "{") throw new Exception();
            word.MoveNext();

            while(word.Text != "}" & !word.Eof)
            {
                word.MoveNext();
            }
            if (word.Text != "}")
            {
                word.AddError("illegal assignment pattern");
            }
            else
            {
                word.MoveNext();
            }

                return null;
        }
    }
}
