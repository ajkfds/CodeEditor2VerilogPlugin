using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Items;
using pluginVerilog.Verilog.ModuleItems;
using Splat.ModeDetection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Module : BuildingBlock, IModuleOrInterface, IPortNameSpace , IBuildingBlockWithModuleInstance, IModuleOrInterfaceOrCheckerOrClass
    {
        protected Module() : base(null, null)
        {

        }

        // Port
        public Dictionary<string, DataObjects.Port> Ports { get; } = new Dictionary<string, DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get; } = new List<DataObjects.Port>();

        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public required override Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile? ret;
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



        public static async Task<Module> ParseCreate(WordScanner word, Attribute attribute, BuildingBlock parent, Data.IVerilogRelatedFile file, bool protoType)
        {
            return await ParseCreate(word, null, attribute,parent, file, protoType);
        }

        public static async Task<Module> ParseCreate(
            WordScanner word,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute attribute,
            BuildingBlock parent,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
            module_declaration  ::= { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                        [ list_of_ports ] ; { module_item }
                                        endmodule
                                    | { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                        [ list_of_port_declarations ] ; { non_port_module_item }
                                        endmodule
            module_keyword      ::= module | macromodule  
            module_identifier   ::= identifier

            module_parameter_port_list  ::= # ( parameter_declaration { , parameter_declaration } ) 
            list_of_ports ::= ( port { , port } )
            */

            // module_keyword ( module | macromodule )
            if (word.Text != "module" && word.Text != "macromodule") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();


            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvPair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvPair.Key, kvPair.Value);
            }

            // module_identifier
            if (word.RootParsedDocument.Root == null) throw new Exception();

            Module module = new Module()
            {
                BeginIndexReference = beginReference,
                Name = word.Text,
                Parent = parent,
                Project = word.Project,
                File = file,
                DefinitionReference = word.CrateWordReference()
            };

            module.BuildingBlock = module;

            if (word.CellDefine) module.cellDefine = true;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal module name");
            }
            else
            {
                module.NameReference = word.GetReference();
            }
            word.MoveNext();
            module.BlockBeginIndexReference = word.CreateIndexReference();

            //if (word.RootParsedDocument.ParseMode == CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.LoadParse)
            //{
            //    while (!word.Eof)
            //    {
            //        if (word.Text == "endmodule") break;
            //        word.MoveNext();
            //    }
            //} else
            if (!word.CellDefine && !protoType)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                await parseModule(prototypeWord, parameterOverrides, null, module);
                prototypeWord.Dispose();
                word.CheckCancelToken();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                await parseModule(word, parameterOverrides, null, module);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                await parseModule(word, parameterOverrides, null, module);
                word.Prototype = false;
            }

            // endmodule keyword
            if (word.Text != "endmodule")
            {
                word.AddError("endmodule expected");
                module.LastIndexReference = word.CreateIndexReference();
                return module;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            module.LastIndexReference = word.CreateIndexReference();

            if (module.BlockBeginIndexReference != null)
            {
                word.AppendBlock(module.BlockBeginIndexReference, module.LastIndexReference,module.Name,false);
            }
            word.MoveNext();

            if (word.Text == ":")
            {
                word.MoveNext();
                if (word.Text == module.Name)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    word.AddError("module name mismatch");
                    word.MoveNext();
                }
            }

            // register with the parent module
            if (!parent.BuildingBlocks.ContainsKey(module.Name))
            {
                parent.BuildingBlocks.Add(module.Name, module);
            }
            else
            {
                if (protoType)
                {
                    module.NameReference.AddError("duplicated module name");
                }
                else
                {
                    module.BuildingBlocks[module.Name] = module;
                }
            }

            return module;
        }

        /*
        module_declaration  ::= { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]override
                                    [ list_of_ports ] ; { module_item }
                                    endmodule
                                | { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                    [ list_of_port_declarations ] ; { non_port_module_item }
                                    endmodule
        module_keyword      ::= module | macromodule  
        module_identifier   ::= identifier

        module_parameter_port_list  ::= # ( parameter_declaration { , parameter_declaration } ) 
        list_of_ports ::= ( port { , port } )
        */
        protected static async System.Threading.Tasks.Task parseModule(
            WordScanner word,
            //            string parameterOverrideModuleName,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute,
            Module module
            )
        {

            while (true)
            {
                if (word.Eof || word.Text == "endmodule")
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
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, module, null);
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
                        if (module.NamedElements.ContainsKey(vkp.Key) && module.NamedElements[vkp.Key] is DataObjects.Constants.Constants)
                        {
                            DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)module.NamedElements[vkp.Key];
                            if (constants.DefinedReference != null)
                            {
                                constants.DefinedReference.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            module.NamedElements.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter() { Name = vkp.Key, DefinedReference = vkp.Value.Reference, Expression = vkp.Value };
                            module.NamedElements.Add(param.Name, param);
                        }
                        else
                        {
                            //System.Diagnostics.Debug.Print("undefined params "+module.File.Name +":" + vkp.Key );
                        }
                    }
                }

                if (word.Eof || word.Text == "endmodule") break;
                if (word.Text == "(")
                {
                    parseListOfPorts_ListOfPortsDeclarations(word, module);
                } // list_of_ports or list_of_port_declarations

                if (word.Eof || word.Text == "endmodule") break;

                if (word.RootParsedDocument.ParseMode == CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.LoadParse)
                {
                    while (!word.Eof)
                    {
                        if (word.Text == "endmodule") break;
                        word.MoveNext();
                    }
                    return;
                }

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
                    if (module.AnsiStylePortDefinition)
                    {
                        if (!await Items.NonPortModuleItem.Parse(word, module))
                        {
                            word.CheckCancelToken();
                            if (word.Text == "endmodule") break;
                            word.AddError("illegal module item");
                            if (!word.SkipToKeyword(";"))
                            {
                                word.MoveNext();
                            }
                        }
                    }
                    else
                    {
                        if (!await Items.ModuleItem.Parse(word, module))
                        {
                            word.CheckCancelToken();
                            if (word.Text == "endmodule") break;
                            word.AddError("illegal module item");
                            if (!word.SkipToKeyword(";"))
                            {
                                word.MoveNext();
                            }
                            else
                            {
                                if (word.Text == ";") word.MoveNext();
                            }
                        }
                    }
                    CommentAnnotationItem.Parse(word, module);
                    word.CheckCancelToken();
                }
                //parseModuleItems(word, module);
                break;
            }



            if (!word.Prototype)
            {
                CheckVariablesUseAndDriven(word, module);
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

            List<INamedElement> instantiations = NamedElements.Values.FindAll(x => x is IBuildingBlockInstantiation);
            foreach (IBuildingBlockInstantiation instantiation in instantiations)
            {
                if (instantiation.Name == null) throw new Exception();
                items.Add(newItem(instantiation.Name, CodeDrawStyle.ColorType.Identifier));
            }
        }


        protected static void parseListOfPorts_ListOfPortsDeclarations(WordScanner word, Module module)
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
            | { attribute_instance } specify_block 
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

        public override List<string> GetExitKeywords()
        {
            return new List<string> {
//                "module","endmodule",
//                "function","endfunction",
//                "task","endtask",
//                "always","initial",
//                "assign","specify","endspecify",
//                "generate","endgenerate"
            };
        }


    }
}
