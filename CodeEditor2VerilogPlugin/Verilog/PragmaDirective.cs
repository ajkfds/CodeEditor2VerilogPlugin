using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class PragmaDirective
    {
        /*
        pragma ::=
                    `pragma pragma_name [ pragma_expression { , pragma_expression } ]
        pragma_name ::= simple_identifier
        pragma_expression ::=
                pragma_keyword
                | pragma_keyword = pragma_value
                | pragma_value
        pragma_value ::=
            ( pragma_expression { , pragma_expression } )
            | number
            | string
            | identifier
        pragma_keyword ::= simple_identifier
         */
        public static void Parse(WordPointer wordPointer)
        {
            if (wordPointer.Text != "`pragma") throw new Exception();
            wordPointer.Color(CodeDrawStyle.ColorType.Keyword);
            wordPointer.MoveNext();

            if (General.IsIdentifier(wordPointer.Text))
            {   // pragma_name
                wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                wordPointer.MoveNext();
            }
            else
            {
                wordPointer.AddError("illegal pragma_name");
                return;
            }

            // The pragma directive argument (pragma_expression list) is a single-line
            // construct. The tokenizer does not track newlines, so parsing pragma
            // expressions as bare identifiers would leak the first token of the
            // following module item (e.g. "wire" in
            //   `pragma protect
            //   wire protected_wire;
            // ) into the directive. Consume the rest of the line instead.
            wordPointer.MoveNextUntilEol();
            // MoveNextUntilEol stops on the directive's argument text (and leaves the
            // scanner on it); advance to the next token so the caller does not treat
            // the argument line as a module item ("illegal module item").
            if (!wordPointer.Eof) wordPointer.MoveNext();
        }

        private static bool parsePragmaExpression(WordPointer wordPointer)
        {
            if (wordPointer.Eof) return false;

            if (General.IsIdentifier(wordPointer.Text))
            {   // pragma_keyword
                wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
                wordPointer.MoveNext();

                if (wordPointer.Text != "=") return true;
                // pragma_keyword = pragma_value : consume "=" then the value
                wordPointer.MoveNext();
                return parsePragmaValue(wordPointer);
            }

            return parsePragmaValue(wordPointer);
        }

        private static bool parsePragmaValue(WordPointer wordPointer)
        {
            if (wordPointer.Eof) return false;
            if (wordPointer.Text == "(") 
            {
                wordPointer.MoveNext();
                if (!parsePragmaExpression(wordPointer))
                {
                    wordPointer.AddError("illegal parsePragmaValue");
                    return true;
                }
                while (!wordPointer.Eof)
                {
                    if (wordPointer.Text != ",") break;
                    wordPointer.MoveNext();

                    if (!parsePragmaExpression(wordPointer))
                    {
                        wordPointer.AddError("illegal parsePragmaValue");
                        break;
                    }
                }
                if(wordPointer.Text == ")")
                {
                    wordPointer.MoveNext();
                    return true;
                }
                return true;
            }
            wordPointer.Color(CodeDrawStyle.ColorType.Identifier);
            wordPointer.MoveNext();
            return true;
        }
        

    }
}
