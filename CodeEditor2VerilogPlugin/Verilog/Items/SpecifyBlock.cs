using System;

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

                // specparam_declaration
                if (word.Text == "specparam")
                {
                    DataObjects.Constants.Constants.ParseCreateDeclaration(word, nameSpace, null);
                    continue;
                }

                // pulsestyle_declaration / showcancelled_declaration: keyword + list_of_path_outputs + ";"
                if (
                    word.Text == "pulsestyle_onevent" || word.Text == "pulsestyle_ondetect" ||
                    word.Text == "showcancelled" || word.Text == "noshowcancelled")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    while (!word.Eof && word.Text != ";")
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    if (word.Text == ";") word.MoveNext();
                    continue;
                }

                // system_timing_check: $setup / $hold / $setuphold / $recovery / $removal /
                // $recrem / $skew / $timeskew / $fullskew / $width / $period / $nochange
                if (word.Text.StartsWith("$"))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        int depth = 1;
                        while (!word.Eof && depth > 0)
                        {
                            if (word.Text == "(") depth++;
                            else if (word.Text == ")") depth--;
                            word.MoveNext();
                        }
                    }
                    if (word.Text == ";") word.MoveNext();
                    continue;
                }

                // path_declaration: simple/parallel/full/if/connect modules path
                //   path_declaration ::= simple_path_declaration ; | ...
                //   simple_path_declaration ::= path_description ; ...
                //   path_description ::= ( input_terminal [ polarity_operator ] => output_terminal )
                //                       | ( [ input_terminal ] [*] > output_terminal )
                if (word.Text == "(")
                {
                    // consume path description up to ';'
                    int depth = 0;
                    while (!word.Eof)
                    {
                        if (word.Text == "(") depth++;
                        else if (word.Text == ")") depth--;
                        else if (word.Text == ";" && depth == 0) break;
                        word.MoveNext();
                    }
                    if (word.Text == ";") word.MoveNext();
                    continue;
                }

                // unknown specify item
                word.MoveNext();
            }

            if (word.Text == "endspecify")
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
