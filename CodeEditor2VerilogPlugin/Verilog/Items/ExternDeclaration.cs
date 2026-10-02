using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    /// <summary>
    /// extern declarations (IEEE 1800-2017)
    ///
    /// module_declaration ::= "extern" module_nonansi_header | "extern" module_ansi_header
    /// interface_declaration ::= "extern" interface_nonansi_header | "extern" interface_ansi_header
    /// checker_declaration ::= "extern" checker_declaration
    /// extern_tf_declaration ::=
    ///       "extern" method_prototype ;
    ///     | "extern" "forkjoin" task_prototype { "," task_prototype } ;
    /// extern_constraint_declaration ::= [ "static" ] "constraint" constraint_identifier "(" [ constraint_port_list ] ")" ;
    ///
    /// These declarations declare a prototype for an elaboration-time / compiler-external entity.
    /// The parser only registers the identifier so that references can resolve; the body is not parsed.
    /// </summary>
    public class ExternDeclaration
    {
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "extern") return false;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            switch (word.Text)
            {
                // extern module_declaration / extern interface_declaration / extern checker_declaration
                case "module":
                case "macromodule":
                case "interface":
                case "checker":
                    return parseExternBuildingBlock(word, nameSpace);

                // extern_tf_declaration ::= "extern" "forkjoin" task_prototype { "," task_prototype } ;
                case "forkjoin":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    while (!word.Eof)
                    {
                        if (word.Text == "task")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            Task_.ParsePrototype(word, nameSpace);
                        }
                        else
                        {
                            word.AddError("task prototype expected");
                            word.SkipToKeyword(";");
                            break;
                        }
                        if (word.Text == ",")
                        {
                            word.MoveNext();
                            continue;
                        }
                        break;
                    }
                    consumeSemicolon(word);
                    return true;

                // extern method_prototype ; | extern constraint_declaration
                default:
                    break;
            }

            // extern_constraint_declaration ::= [ "static" ] "constraint" constraint_identifier ( [ constraint_port_list ] ) ;
            if (word.Text == "static" && word.NextText == "constraint")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            if (word.Text == "constraint")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("constraint identifier expected");
                    word.SkipToKeyword(";");
                    consumeSemicolon(word);
                    return true;
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                // skip optional port list
                if (word.Text == "(")
                {
                    word.SkipToKeyword(")");
                    if (word.Text == ")") word.MoveNext();
                }
                consumeSemicolon(word);
                return true;
            }

            // extern method_prototype ;
            if (word.Text == "pure")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            if (word.Text == "function" || word.Text == "task")
            {
                MethodPrototype.ParseCreate(word, nameSpace);
                return true;
            }

            word.AddError("illegal extern declaration");
            word.SkipToKeyword(";");
            consumeSemicolon(word);
            return true;
        }

        /// <summary>
        /// extern module_nonansi_header / module_ansi_header / interface_*_header / checker_declaration
        /// Consume the header up to and including the terminating semicolon and register the identifier.
        /// </summary>
        private static bool parseExternBuildingBlock(WordScanner word, NameSpace nameSpace)
        {
            // building block keyword (module / interface / checker)
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // [ lifetime ]
            if (word.Text == "static" || word.Text == "automatic")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            // "interface class" case
            if (word.Text == "class")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            // identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("identifier expected");
                word.SkipToKeyword(";");
                consumeSemicolon(word);
                return true;
            }
            string externName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // consume the rest of the header (parameters, ports) up to ";"
            word.SkipToKeyword(";");
            consumeSemicolon(word);
            return true;
        }

        private static void consumeSemicolon(WordScanner word)
        {
            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
            }
        }
    }
}
