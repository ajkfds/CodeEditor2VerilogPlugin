using pluginVerilog.Verilog.DataObjects.Nets;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class PackageOrGenerateItemDeclaration
    {
        /*
        package_or_generate_item_declaration ::= 
              net_declaration 
            | data_declaration 
            | task_declaration 
            | function_declaration 
            | checker_declaration 
            | dpi_import_export 
            | extern_constraint_declaration 
            | class_declaration 
            | class_constructor_declaration 
            | local_parameter_declaration ;
            | parameter_declaration ;
            | covergroup_declaration 
            | overload_declaration 
            | assertion_item_declaration 
            | ;
         */
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            // data_declaration
            if (DataDeclaration.Parse(word, nameSpace)) return true;

            //            if (DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace)) return true;
            /*
            data_declaration ::=      [ const ] [ var ] [ lifetime ] data_type_or_implicit list_of_variable_decl_assignments ;10
                                    | type_declaration
                                    | package_import_declaration
                                    | net_type_declaration

            package_import_declaration ::=
                "import" package_import_item { , package_import_item } ;
             */
            //if (word.Text == "typedef")
            //{
            //    return DataObjects.Typedef.ParseDeclaration(word, nameSpace);
            //}


            switch (word.Text)
            {
                // net_declaration
                case "supply0":
                case "supply1":
                case "tri":
                case "triand":
                case "trior":
                case "trireg":
                case "tri0":
                case "tri1":
                case "uwire":
                case "wire":
                case "wand":
                case "wor":
                    Net.ParseDeclaration(word, nameSpace);
                    break;
                //              struct_union["packed"[signing]] { struct_union_member { struct_union_member } }{ packed_dimension }
                case "event":
                    DataObjects.Variables.Event.ParseCreateFromDeclaration(word, nameSpace);
                    break;
                //              ps_covergroup_identifier
                //              type_reference
                //          implicit_data_type
                //      type_declaration

                //      package_import_declaration

                //      net_type_declaration

                // task_declaration
                case "task":
                    await Task.Parse(word, nameSpace);
                    break;

                // function_declaration
                case "function":
                    await Function.Parse(word, nameSpace);
                    break;

                // checker_declaration
                // TODO

                // dpi_import_export
                case "import":
                case "export":
                    await DpiImportExport.Parse(word, nameSpace);
                    break;
                // extern_constraint_declaration
                // TODO

                // class_declaration
                case "virtual":
                case "class":
                    await BuildingBlocks.Class.ParseDeclaration(word, nameSpace);
                    break;

                // interface_class_declaration
                case "interface":
                    // Check if it's interface class
                    if (word.NextText == "class")
                    {
                        await BuildingBlocks.InterfaceClass.ParseDeclaration(word, nameSpace);
                        break;
                    }
                    // Fall through to module/interface handling if needed
                    return false;

                // class_constructor_declaration

                // local_parameter_declaration;
                // parameter_declaration;
                case "parameter":
                case "localparam":
                    Verilog.DataObjects.Constants.Parameter.ParseCreateDeclaration(word, nameSpace, null);
                    break;

                // covergroup_declaration
                case "covergroup":
                    // TODO: implement covergroup declaration
                    word.AddError("covergroup declaration not implemented");
                    word.SkipToKeyword("endgroup");
                    if (word.Text == "endgroup") word.MoveNext();
                    if (word.Text == ";") word.MoveNext();
                    break;

                // overload_declaration

                // assertion_item_declaration
                case "property":
                    // property_declaration ::= property property_identifier [ ( [ property_port_list ] ) ] ; { assertion_variable_declaration } property_spec [ ; ] endproperty [ : property_identifier ]
                    await ParsePropertyDeclaration(word, nameSpace);
                    return true;

                case "sequence":
                    // sequence_declaration ::= sequence sequence_identifier [ ( [ sequence_port_list ] ) ] ; { assertion_variable_declaration } sequence_expr [ ; ] endsequence [ : sequence_identifier ]
                    await ParseSequenceDeclaration(word, nameSpace);
                    return true;

                // ;
                case ";":
                    word.AddSystemVerilogError();
                    word.MoveNext();
                    break;

                // etc
                case "(*":
                    Attribute attribute = Attribute.ParseCreate(word, nameSpace);
                    break;
                // errpr trap
                case "endgenerate":
                    return false;
                case "end":
                    return false;
                case "endmodule":
                    return false;

                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Parse property declaration
        /// property_declaration ::=
        ///     "property" property_identifier [ "(" [ property_port_list ] ")" ] ";"
        ///     { assertion_variable_declaration }
        ///     property_spec [ ";" ]
        ///     "endproperty" [ ":" property_identifier ]
        /// </summary>
        private static async System.Threading.Tasks.Task ParsePropertyDeclaration(WordScanner word, NameSpace nameSpace)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // property

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("property identifier expected");
                word.SkipToKeyword("endproperty");
                if (word.Text == "endproperty") word.MoveNext();
                return;
            }

            string propertyName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse optional port list
            if (word.Text == "(")
            {
                word.MoveNext();
                // TODO: Parse property_port_list
                // property_port_item ::= { attribute_instance } [ "local" [ property_lvar_port_direction ] ] property_formal_type formal_port_identifier {variable_dimension} [ "=" property_actual_arg ]
                while (!word.Eof && word.Text != ")")
                {
                    if (word.Text == ")")
                    {
                        break;
                    }
                    word.MoveNext();
                }
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // Parse assertion_variable_declarations
            // assertion_variable_declaration ::= var_data_type list_of_variable_decl_assignments ;
            while (!word.Eof && word.Text != "endproperty")
            {
                // Skip assertion variable declarations
                // This is a simplified implementation - a full implementation would parse the actual declarations
                word.MoveNext();
            }

            // Skip to endproperty
            if (word.Text == "endproperty")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Check for optional : property_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == propertyName)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }
            }
        }

        /// <summary>
        /// Parse sequence declaration
        /// sequence_declaration ::=
        ///     "sequence" sequence_identifier [ "(" [ sequence_port_list ] ")" ] ";"
        ///     { assertion_variable_declaration }
        ///     sequence_expr [ ";" ]
        ///     "endsequence" [ ":" sequence_identifier ]
        /// </summary>
        private static async System.Threading.Tasks.Task ParseSequenceDeclaration(WordScanner word, NameSpace nameSpace)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // sequence

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("sequence identifier expected");
                word.SkipToKeyword("endsequence");
                if (word.Text == "endsequence") word.MoveNext();
                return;
            }

            string sequenceName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse optional port list
            if (word.Text == "(")
            {
                word.MoveNext();
                // TODO: Parse sequence_port_list
                // sequence_port_item ::= { attribute_instance } [ "local" [ sequence_lvar_port_direction ] ] sequence_formal_type formal_port_identifier {variable_dimension} [ = sequence_actual_arg ]
                // sequence_lvar_port_direction ::= input | inout | output
                while (!word.Eof && word.Text != ")")
                {
                    if (word.Text == ")")
                    {
                        break;
                    }
                    word.MoveNext();
                }
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // Parse assertion_variable_declarations
            // assertion_variable_declaration ::= var_data_type list_of_variable_decl_assignments ;
            while (!word.Eof && word.Text != "endsequence")
            {
                // Skip assertion variable declarations
                // This is a simplified implementation - a full implementation would parse the actual declarations
                word.MoveNext();
            }

            // Skip to endsequence
            if (word.Text == "endsequence")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Check for optional : sequence_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == sequenceName)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }
            }
        }
    }
}
