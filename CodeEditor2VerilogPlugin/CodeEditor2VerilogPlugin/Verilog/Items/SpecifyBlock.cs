using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class SpecifyBlock
    {
        protected SpecifyBlock() { }

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "specify") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (word.Text == "endspecify") break;
                word.MoveNext();
            }

            if (word.Text== "endspecify")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            /*
            pecify_block ::= "specify" { specify_item } "endspecify"
            specify_item ::=    specparam_declaration
                                | pulsestyle_declaration
                                | showcancelled_declaration
                                | path_declaration
                                | system_timing_check
            pulsestyle_declaration ::=    "pulsestyle_onevent" list_of_path_outputs ";"
                                        | "pulsestyle_ondetect" list_of_path_outputs ";"
            showcancelled_declaration ::=     "showcancelled" list_of_path_outputs ";"
                                            | "noshowcancelled" list_of_path_outputs ";" 
             */
            return true;
        }

    }
}
