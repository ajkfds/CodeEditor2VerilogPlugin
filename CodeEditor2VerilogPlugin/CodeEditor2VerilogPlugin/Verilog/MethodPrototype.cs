using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public static class MethodPrototype
    {
        public static async System.Threading.Tasks.Task<bool> ParseCreateWithPureVirtual(WordScanner word, NameSpace nameSpace)
        {
            /*
            interface_class_method ::=
                "pure" "virtual" method_prototype ;

            method_prototype ::=
                  task_prototype
                | function_prototype

            task_prototype ::= task task_identifier [ ( [ tf_port_list ] ) ]
            function_prototype ::= function data_type_or_void function_identifier [ ( [ tf_port_list ] ) ]
             */
            if (word.Eof | word.Text != "pure") return false;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Eof | word.Text != "virtual") return true;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (await ParseCreate(word, nameSpace))
            {
                return true;
            }
            else
            {
                return true;
            }
        }

        public static async System.Threading.Tasks.Task<bool> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            /*
            method_prototype ::=
                  task_prototype
                | function_prototype

            task_prototype ::= task task_identifier [ ( [ tf_port_list ] ) ]
            function_prototype ::= function data_type_or_void function_identifier [ ( [ tf_port_list ] ) ]
             */
            switch (word.Text)
            {
                case "task":
                    await Task.ParsePrototype(word, nameSpace);
                    return true;
                case "function":
                    await Function.ParsePrototype(word, nameSpace);
                    return true;
                default:
                    word.AddError("illegal method prototype");
                    return false;
            }

        }

    }
}
