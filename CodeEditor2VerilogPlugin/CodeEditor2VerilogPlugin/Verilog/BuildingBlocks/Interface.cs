using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Interface : BuildingBlock, IModuleOrInterface
    {
        protected Interface() : base(null, null)
        {

        }

        // IModuleOrInterfaceOrProgram

        // Port
        public Dictionary<string, DataObjects.Port> Ports { get; } = new Dictionary<string, DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get; } = new List<DataObjects.Port>();

//        public Dictionary<string, ModPort> ModPorts { get; } = new Dictionary<string, ModPort>();
        //        public WordReference NameReference;
        //        public List<string> PortParameterNameList { get; } = new List<string>();

        // Module
        //        public Dictionary<string, ModuleItems.IInstantiation> Instantiations { get; } = new Dictionary<string, ModuleItems.IInstantiation>();


        private WeakReference<Data.IVerilogRelatedFile>? fileRef;
        public required override Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            init
            {
                fileRef = new WeakReference<Data.IVerilogRelatedFile>(value);
            }
        }

        public override string FileId { get; protected set; }
        private bool cellDefine = false;
        public bool CellDefine
        {
            get { return cellDefine; }
        }



        public static Interface Create(WordScanner word, NameSpace nameSpace, Attribute? attribute, Data.IVerilogRelatedFile file, bool protoType)
        {
            return Create(word,nameSpace, null, attribute, file, protoType);
        }
        public static Interface Create(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
            interface_declaration   ::=   interface_nonansi_header  [ timeunits_declaration ] { interface_item }            "endinterface" [ : interface_identifier ] 
                                        | interface_ansi_header     [ timeunits_declaration ] { non_port_interface_item }   "endinterface" [ : interface_identifier ] 
                                        | { attribute_instance } "interface" interface_identifier "( .* )" ; [ timeunits_declaration ] { interface_item } "endinterface" [ : interface_identifier ] 
                                        | "extern" interface_nonansi_header 
                                        | "extern" interface_ansi_header

            interface_nonansi_header    ::= { attribute_instance } "interface" [ lifetime ] interface_identifier { package_import_declaration } [ parameter_port_list ] list_of_ports ;
            interface_ansi_header       ::= { attribute_instance } "interface" [ lifetime ] interface_identifier { package_import_declaration } [ parameter_port_list ] [ list_of_port_declarations ] ;


            module_parameter_port_list  ::= # ( parameter_declaration { , parameter_declaration } ) 
            list_of_ports ::= ( port { , port } )
            */

            if (word.Text != "interface") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();

            // indentifier
            Interface interface_ = new Interface() {
                BeginIndexReference = beginReference,
                Name = word.Text,
                Parent = nameSpace,
                Project = word.Project,
                File = file,
                DefinitionReference = word.CrateWordReference()
            };
            if (word.CellDefine) interface_.cellDefine = true;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal module name");
            }
            else
            {
                interface_.NameReference = word.GetReference();
            }
            interface_.BuildingBlock = interface_;
            word.MoveNext();

            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvpair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvpair.Key, kvpair.Value);
            }

            if (!word.CellDefine && !protoType)
            {
                // protptype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parseInterfaceItems(prototypeWord, parameterOverrides, null, interface_);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parseInterfaceItems(word, parameterOverrides, null, interface_);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                parseInterfaceItems(word, parameterOverrides, null, interface_);
                word.Prototype = false;
            }

            // endmodule keyword
            if (word.Text == "endinterface")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                interface_.LastIndexReference = word.CreateIndexReference();

                if (interface_.Parent != null)
                {
                    if (interface_.Parent.NamedElements.ContainsKey(interface_.Name))
                    {
                        if(protoType)
                        {
                            word.AddError("duplicate interface");
                        }
                        else
                        {
                            interface_.Parent.NamedElements .Replace(interface_.Name,interface_);
                        }

                    }
                    else
                    {
                        interface_.Parent.NamedElements.Add(interface_.Name, interface_);
                    }
                }

                word.AppendBlock(interface_.BeginIndexReference, interface_.LastIndexReference);
                word.MoveNext();
                return interface_;
            }

            {
                word.AddError("endmodule expected");
            }

            return interface_;
        }

        /*
        module_declaration  ::= { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]Foverride
                                    [ list_of_ports ] ; { module_item }
                                    endmodule
                                | { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                    [ list_of_port_declarations ] ; { non_port_module_item }
                                    endmodule
        module_keyword      ::= module | macromodule  
        module_identifier   ::= identifier

        module_parameter_port_list  ::= # ( parameter_declaration { , parameter_declaration } ) 
        list_of_ports ::= ( port { , port } )


        interface_item  ::=   port_declaration ;
                            | non_port_interface_item 
        non_port_interface_item ::=  generate_region 
                                    | interface_or_generate_item 
                                    | program_declaration 
                                    | modport_declaration
                                    | interface_declaration 
                                    | timeunits_declaration3
        */
        protected static void parseInterfaceItems(
            WordScanner word,
            //            string parameterOverrideModueName,
            Dictionary<string, Expressions.Expression> parameterOverrides,
            Attribute attribute, Interface interface_)
        {

            while (true)
            {
                if (word.Eof || word.Text == "endinterface")
                {
                    break;
                }
                if (word.Text == "#")
                { // module_parameter_port_list
                    word.MoveNext();
                    do
                    {
                        if (word.GetCharAt(0) != '(')
                        {
                            word.AddError("( expected");
                            break;
                        }
                        word.MoveNext();
                        while (!word.Eof)
                        {
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, interface_, null);
                            if (word.Text != ",")
                            {
                                if (word.Text == ")") break;
                                if (word.Text == ",") continue;

                                if (word.Prototype) word.AddPrototypeError("illegal separator");
                                // illegal
                                word.SkipToKeyword(",");
                                if (word.Text == "parameter") continue;
                                break;
                            }
                            word.MoveNext();
                        }

                        if (word.GetCharAt(0) != ')')
                        {
                            word.AddError(") expected");
                            break;
                        }
                        word.MoveNext();
                    } while (false);
                }

                if (parameterOverrides != null)
                {
                    foreach (var vkp in parameterOverrides)
                    {
                        if (interface_.NamedElements.ContainsKey(vkp.Key) && interface_.NamedElements[vkp.Key] is DataObjects.Constants.Constants)
                        {
                            DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)interface_.NamedElements[vkp.Key];
                            if (constants.DefinitionRefrecnce != null)
                            {
                                //                                module.Parameters[vkp.Key].DefinitionRefrecnce.AddNotice("override " + vkp.Value.Value.ToString());
                                constants.DefinitionRefrecnce.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            interface_.NamedElements.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter() { Name = vkp.Key, DefinitionRefrecnce= vkp.Value.Reference, Expression = vkp.Value };
                            interface_.NamedElements.Add(param.Name, param);
                        }
                        else
                        {
                            //System.Diagnostics.Debug.Print("undefed params "+module.File.Name +":" + vkp.Key );
                        }
                    }
                }

                if (word.Eof || word.Text == "endinterface") break;
                if (word.Text == "(")
                {
                    parseListOfPorts_ListOfPortsDeclarations(word, interface_);
                } // list_of_ports or list_of_posrt_declarations

                if (word.Eof || word.Text == "endinterface") break;

                if (word.GetCharAt(0) == ';')
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                }

                while (!word.Eof)
                {
                    if (!Items.InterfaceItem.Parse(word, interface_))
                    {
                        if (word.Text == "endinterface") break;
                        word.AddError("illegal module item");
                        word.MoveNext();
                    }
                }
                //parseModuleItems(word, module);
                break;
            }


            if (!word.Prototype)
            {
                CheckVariablesUseAndDriven(word, interface_);
            }

            //foreach (var variable in module.Variables.Values)
            //{
            //    if (variable.DefinedReference == null) continue;
            //    variable.UsedReferences.Clear();
            //}
            return;
        }

        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (IBuildingBlockInstantiation instantiation in NamedElements.Values.OfType<IBuildingBlockInstantiation>())
            {
                if (instantiation.Name == null) continue;
                items.Add(newItem(instantiation.Name, CodeDrawStyle.ColorType.Identifier));
            }
        }

        //protected static void checkVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        //{
        //    foreach (var variable in nameSpace.DataObjects.Values)
        //    {
        //        if (variable.DefinedReference == null) continue;

        //        DataObjects.Variables.ValueVariable? valueVar = variable as DataObjects.Variables.ValueVariable;
        //        if (valueVar == null) continue;

        //        if (valueVar.AssignedReferences.Count == 0)
        //        {
        //            if (valueVar.UsedReferences.Count == 0)
        //            {
        //                word.AddNotice(variable.DefinedReference, "undriven & unused");
        //            }
        //            else
        //            {
        //                word.AddNotice(variable.DefinedReference, "undriven");
        //            }
        //        }
        //        else
        //        {
        //            if (valueVar.UsedReferences.Count == 0)
        //            {
        //                word.AddNotice(variable.DefinedReference, "unused");
        //            }
        //        }
        //    }
        //}

        protected static void parseListOfPorts_ListOfPortsDeclarations(WordScanner word, Interface module)
        {
            // list_of_ports::= (port { , port } )
            // list_of_port_declarations::= (port_declaration { , port_declaration } ) | ( )

            // port::= [port_expression] | .port_identifier( [port_expression] )
            // port_expression::= port_reference | { port_reference { , port_reference } }
            // port_reference::= port_identifier | port_identifier[constant_expression] | port_identifier[range_expression]

            // port_declaration::= { attribute_instance} inout_declaration | { attribute_instance} input_declaration | { attribute_instance} output_declaration  

            // inout_declaration::= inout[net_type][signed][range] list_of_port_identifiers
            // input_declaration ::= input[net_type][signed][range] list_of_port_identifiers
            // output_declaration ::= output[net_type][signed][range]      
            // list_of_port_identifiers | output[reg][signed][range]     
            // list_of_port_identifiers | output reg[signed][range]      
            // list_of_variable_port_identifiers | output[output_variable_type]      
            // list_of_port_identifiers | output output_variable_type list_of_variable_port_identifiers 
            // list_of_port_identifiers::= (From Annex A -A.2.3) port_identifier { , port_identifier }

            // list_of_variable_port_identifiers ::= port_identifier [ = constant_expression ]                               { , port_identifier [ = constant_expression ] }  

            if (word.Text != "(") return;
            word.MoveNext();
            if (word.Text == ")")
            {
                word.MoveNext();
                return;
            }

            Verilog.DataObjects.Port.ParsePortDeclarations(word, module);

            if (word.Text == ")")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError(") expected");
            }
        }

        /*
        ##Verilog 2001
        module_item ::= module_or_generate_item
            | port_declaration ;
            | { attribute_instance } generated_instantiation 
            | { attribute_instance } local_parameter_declaration
            | { attribute_instance } parameter_declaration
            | { attribute_instance } 
        _block 
            | { attribute_instance } specparam_declaration  
        module_or_generate_item ::=   { attribute_instance } module_or_generate_item_declaration 
            | { attribute_instance } parameter_override 
            | { attribute_instance } continuous_assign
            | { attribute_instance } gate_instantiation
            | { attribute_instance } udp_instantiation 
            | { attribute_instance } module_instantiation 
            | { attribute_instance } initial_construct 
            | { attribute_instance } always_construct  
        module_or_generate_item_declaration ::=   net_declaration 
            | reg_declaration
            | integer_declaration 
            | real_declaration 
            | time_declaration 
            | realtime_declaration 
            | event_declaration 
            | genvar_declaration 
            | task_declaration 
            | function_declaration          
        parameter_override ::= defparam list_of_param_assignments ;  
        */

        /*
         ## SystemVerilog 2012

        module_common_item::=
                      module_or_generate_item_declaration 
                    | interface_instantiation 
                    | program_instantiation 
                    | assertion_item 
                    | bind_directive 
                    | continuous_assign 
                    | net_alias 
                    | initial_construct 
                    | final_construct 
                    | always_construct 
                    | loop_generate_construct 
                    | conditional_generate_construct

        module_item ::= 
                      port_declaration ;
                    | non_port_module_item

        module_or_generate_item ::= 
                      { attribute_instance } parameter_override 
                    | { attribute_instance } gate_instantiation 
                    | { attribute_instance } udp_instantiation
                    | { attribute_instance } module_instantiation
                    | { attribute_instance } module_common_item

        module_or_generate_item_declaration ::= 
                      package_or_generate_item_declaration
                    | genvar_declaration
                    | clocking_declaration
                    | default clocking clocking_identifier ;

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


        /*
        generated_instantiation ::= (From Annex A -A.4.2) generate { generate_item } endgenerate
        generate_item_or_null ::= generate_item | ;  
        generate_item ::=   generate_conditional_statement | generate_case_statement | generate_loop_statement | generate_block | module_or_generate_item  

         */


        public static List<string> UniqueKeywords = new List<string> {
            "module","endmodule",
            "function","endfunction",
            "task","endtask",
            "always","initial",
            "assign","specify","endspecify",
            "generate","endgenerate"
        };


    }
}
