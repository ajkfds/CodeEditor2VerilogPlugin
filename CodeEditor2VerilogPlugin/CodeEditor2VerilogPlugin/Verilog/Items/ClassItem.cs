using System;
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
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            return await Parse(word, nameSpace, null);
        }

        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace,Attribute? attribute)
        {
            // { attribute_instance } class_property
            {
                // { property_qualifier } data_declaration  
                if (DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace)) return true;

                // TODO
                // "const" { class_item_qualifier } data_type const_identifier [ = constant_expression ] ; 
                //class_item_qualifier ::= 
                //      "static"  
                //    | "protected"  
                //    | "local"   
                if (word.Text == "const")
                {
                    return parseConst(word, nameSpace);
                }
            }

                switch (word.Text)
            {
                case "endclass":
                    return false;

                // { attribute_instance }
                case "(*":
                    Attribute attr = Attribute.ParseCreate(word, nameSpace);
                    await Parse(word, nameSpace, attr);
                    return true;
                // ;
                case ";":
                    word.MoveNext();
                    return true;
                // local_parameter_declaration;
                // parameter_declaration;
                case "parameter":
                case "localparam":
                    if(attribute != null)
                    {
                        attribute.Reference.AddError("attribute instance is not accepted");
                    }
                    DataObjects.Constants.Constants.ParseCreateDeclaration(word, nameSpace, attribute);
                    return true;
                // { attribute_instance } class_method
                //        { method_qualifier } task_declaration  
                case "task":
                    await Task.Parse(word, nameSpace);
                    return true;

                //      | { method_qualifier } function_declaration  
                case "function":
                    await Function.ParseFunctionOrConstructor(word, nameSpace);
                    return true;
                //      | "pure" "virtual" { class_item_qualifier } method_prototype ;  
                case "pure":
                    return await MethodPrototype.ParseCreateWithPureVirtual(word, nameSpace);
                //      | "extern" { method_qualifier } method_prototype ;
                case "extern":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return await MethodPrototype.ParseCreate(word, nameSpace);

                //      | { method_qualifier } class_constructor_declaration  
                //      | "extern" { method_qualifier } class_constructor_prototype


                // { attribute_instance } class_constraint
                case "constraint":
                    {
                        var constraint = Coverage.ConstraintDeclaration.ParseCreate(word, nameSpace);
                        if (constraint != null)
                        {
                            // Register constraint in namespace
                            if (!word.Prototype && !string.IsNullOrEmpty(constraint.Name))
                            {
                                if (!nameSpace.NamedElements.ContainsKey(constraint.Name))
                                {
                                    nameSpace.NamedElements.Add(constraint.Name, constraint);
                                }
                            }
                            return true;
                        }
                        return false;
                    }

                // { attribute_instance } class_declaration
                case "class":
                    await BuildingBlocks.Class.ParseCreate(word, nameSpace, attribute, nameSpace.BuildingBlock, nameSpace.BuildingBlock.File);
                    return true;

                // { attribute_instance } covergroup_declaration
                case "covergroup":
                    {
                        var covergroup = await Coverage.CovergroupDeclaration.ParseCreate(word, nameSpace);
                        if (covergroup != null)
                        {
                            // Register covergroup in namespace
                            if (!word.Prototype && !string.IsNullOrEmpty(covergroup.Name))
                            {
                                if (!nameSpace.NamedElements.ContainsKey(covergroup.Name))
                                {
                                    nameSpace.NamedElements.Add(covergroup.Name, covergroup);
                                }
                            }
                            return true;
                        }
                        return false;
                    }

                case "virtual":
                case "rand":
                case "randc":
                case "static":
                case "protected":
                case "local":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    await Parse(word, nameSpace);
                    return true;
                default:
                    break;
            }
            return false;




            //switch (word.Text)
            //{
            //    // temporary TODO implemet

            //    default:
            //        return await NonPortModuleItem.Parse(word, nameSpace);
            //}
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
