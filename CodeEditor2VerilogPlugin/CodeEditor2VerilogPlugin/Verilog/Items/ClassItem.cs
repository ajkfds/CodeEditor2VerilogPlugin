using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ClassItem
    {
        /*
        class_item ::=  
              { attribute_instance } class_property  
            | { attribute_instance } class_method  
            | { attribute_instance } class_constraint  
            | { attribute_instance } class_declaration  
            | { attribute_instance } covergroup_declaration  
            | local_parameter_declaration ;  
            | parameter_declaration7 ;  
            | ;
        
        class_property ::=  
              { property_qualifier } data_declaration  
            | "const" { class_item_qualifier } data_type const_identifier [ = constant_expression ] ; 

        class_method ::=  
              { method_qualifier } task_declaration  
            | { method_qualifier } function_declaration  
            | "pure" "virtual" { class_item_qualifier } method_prototype ;  
            | "extern" { method_qualifier } method_prototype ;  
            | { method_qualifier } class_constructor_declaration  
            | "extern" { method_qualifier } class_constructor_prototype


        class_item_qualifier ::= 
              "static"  
            | "protected"  
            | "local"  

        property_qualifier ::= 
              random_qualifier  
            | class_item_qualifier

        random_qualifier ::= 
              "rand"  
            | "randc"  

        method_qualifier ::=  
            [ "pure" ] "virtual"  
            | class_item_qualifier 

        data_declaration ::=  
              [ const ] [ var ] [ lifetime ] data_type_or_implicit list_of_variable_decl_assignments ;  
            | type_declaration  
            | package_import_declaration11 net_type_declaration 

        class_constructor_declaration ::= 
            function [ class_scope ] new [ ( [ tf_port_list ] ) ] ;  
            { block_item_declaration }  
            [ super . new [ ( list_of_arguments ) ] ; ]  
            { function_statement_or_null } 
            endfunction [ : new ] 

        class_scope ::= class_type ::  
       */

        /*
         
        random_qualifier data_declaration
        
        class_item_qualifier task_declaration/function_declaration/class_constructor_declaration/data_declaration

        [ "pure" ] "virtual" task_declaration/function_declaration/{ class_item_qualifier } method_prototype ;/class_constructor_declaration
         
        "extern" ({ method_qualifier } method_prototype ;)/({ method_qualifier } class_constructor_prototype)
         
        "const" { class_item_qualifier } data_type const_identifier [ = constant_expression ] ; 
         */

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // data_declaration
            if (DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace)) return true;

            switch (word.Text)
            {
                // task_declaration
                case "task":
                    Task.Parse(word, nameSpace);
                    return true;

                // function_declaration
                case "function":
                    Function.ParseFunctionOrConstructor(word, nameSpace);
                    return true;

                case "const":
                    return parseConst(word, nameSpace);
                case "endclass":
                    return false;
                default:
                    return NonPortModuleItem.Parse(word, nameSpace);
            }
        }

        private static bool parseConst(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "const") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            //class_item_qualifier
            switch (word.Text)
            {
                case "static":
                case "protected":
                case "local":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
            }

            // data_declaration
            if (DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace)) return true;

            return true;
        }

    }
}