using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class SubroutineCall
    {
        /* SystemVerilog IEEE1800-2017
        
            subroutine_call ::=               tf_call
                                            | system_tf_call
                                            | method_call
                                            | [ std :: ] randomize_call

            list_of_arguments ::=             [ expression ] { , [ expression ] } { , . identifier ( [ expression ] ) }
                                            | .identifier ( [ expression ] ) { , . identifier ( [ expression ] ) }
            ps_or_hierarchical_tf_identifier ::=      [ package_scope ] tf_identifier
                                                    | hierarchical_tf_identifier
            hierarchical_tf_identifier ::= hierarchical_identifier
            hierarchical_identifier ::= [ $root . ] { identifier constant_bit_select . } identifier
            tf_identifier ::= identifier


            tf_call ::=                   ps_or_hierarchical_tf_identifier { attribute_instance } [ ( list_of_arguments ) ]
            method_call_body ::=          method_identifier { attribute_instance } [ ( list_of_arguments ) ]


            method_call ::=               method_call_root . method_call_body
                                        | built_in_method_call
            built_in_method_call ::=      array_manipulation_call
                                        | randomize_call
            method_call_root ::=          primary
                                        | implicit_class_handle

            package_scope ::=     package_identifier "::"
                                | "$unit" "::"

            class_scope ::= class_type "::"
            class_type ::=  ps_class_identifier [ parameter_value_assignment ] { "::" class_identifier [ parameter_value_assignment ] }
            ps_class_identifier ::= [ package_scope ] class_identifier

            array_manipulation_call ::= array_method_name { attribute_instance }    [ "(" list_of_arguments ")" ]　[ "with" "(" expression ")" ]


            randomize_call ::=  "randomize" { attribute_instance }
                                [ "(" [ variable_identifier_list | "null" ] ")" ]
                                [ "with" [ "(" [ identifier_list ] ")" ] constraint_block ]

         */

        /*
            subroutine_call ::=             + <[ package_scope ] tf_identifier | hierarchical_tf_identifier>    { attribute_instance } [ ( list_of_arguments ) ]
                                            + <primary | implicit_class_handle>.method_identifier               { attribute_instance } [ ( list_of_arguments ) ]
                                            + built_in_method_call
                                            + system_tf_call
                                            + [ std :: ] randomize_call

         
         
         */
        //public static IStatement? ParseCreateStatement(WordScanner word, NameSpace nameSpace)
        //{
        //    /*
        //    subroutine_call ::=               tf_call
        //                                    | system_tf_call
        //                                    | method_call
        //                                    | [ std :: ] randomize_call
        //    */


        //}
    }
}
