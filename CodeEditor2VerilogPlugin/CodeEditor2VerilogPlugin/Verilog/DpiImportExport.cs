using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.Verilog
{
    public class DpiImportExport
    {
        /*
         dpi_import_export::=
                  "import" dpi_spec_string[dpi_function_import_property] [c_identifier "=" ] dpi_function_proto ;
                | "import" dpi_spec_string[dpi_task_import_property] [c_identifier "=" ] dpi_task_proto ;
                | "export" dpi_spec_string[c_identifier = ] "function" function_identifier;
                | "export" dpi_spec_string[c_identifier = ] "task" task_identifier;
        dpi_spec_string::=
                \"DPI-C\" | \"DPI\"
        dpi_function_import_property    ::= context | pure
        dpi_task_import_property        ::= context
        dpi_function_proto              ::= function_prototype
        dpi_task_proto                  ::= task_prototype
        function_prototype              ::= function data_type_or_void function_identifier[([tf_port_list])]
        task_prototype                  ::= task task_identifier [ ([tf_port_list])]
        */
        public static void Parse(WordScanner word, NameSpace nameSpace)
        {
            bool import = false;
            if (word.Text == "import")
            {
                import = true;
            }else if(word.Text == "export")
            {
                import = false;
            }
            else
            {
                throw new Exception();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.Text =="\""+ "DPI-C" + "\"")
            {

            }
            else if (word.Text == "\"" + "DPI-C" + "\"")
            {

            }
            else
            {
                word.AddError("illegal dpi_spec_string");
                word.MoveNext();
                return;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (import)
            {
                parseImport(word, nameSpace);
            }
            else
            {
                parseExport(word, nameSpace);
            }
        }
        public static void parseImport(WordScanner word, NameSpace nameSpace)
        {
            bool shouldBeFunction = false;
            if (word.Text == "context")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (word.Text == "pure")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                shouldBeFunction = true;
            }

            if (word.Text != "function" && word.Text != "task")
            {
                if (word.NextText == "=")
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    word.MoveNext(); // =
                }
                else
                {
                    word.AddError("illegal Dpi Import");
                    return;
                }

            }
            if(word.Text == "task")
            {
                if (shouldBeFunction) word.AddError("cannot use task with pure keyword");
                Task.ParsePrototype(word, nameSpace);
                return;
            }
            if(word.Text == "function")
            {
                Function.ParsePrototype(word, nameSpace);
                return;
            }
            word.AddError("illegal Dpi Import");
        }


        public static void parseExport(WordScanner word, NameSpace nameSpace)
        {

        }

    }
}
