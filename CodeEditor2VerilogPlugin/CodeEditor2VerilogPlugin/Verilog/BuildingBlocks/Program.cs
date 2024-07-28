using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Program : BuildingBlock, IModuleOrInterfaceOrProgram
    {
        protected Program() : base(null, null)
        {

        }

        // IModuleOrInterfaceOrProgram
        // Port
        private Dictionary<string, DataObjects.Port> ports = new Dictionary<string, DataObjects.Port>();
        public Dictionary<string, DataObjects.Port> Ports { get { return ports; } }
        private List<DataObjects.Port> portsList = new List<DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get { return portsList; } }
        public WordReference NameReference;
        private List<string> portParameterNameList = new List<string>();
        public List<string> PortParameterNameList { get { return portParameterNameList; } }

        public bool AnsiPortStyle = false;
        public bool Static = true;

        public static Program Create(WordScanner word, Attribute attribute, Data.IVerilogRelatedFile file, bool protoType)
        {
            return Create(word, null, attribute, file, protoType);
        }
        public static Program Create(
            WordScanner word,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute attribute,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
            program_declaration ::= 
                  program_nonansi_header [ timeunits_declaration ] { program_item } "endprogram" [ : program_identifier ] 

                | program_ansi_header [ timeunits_declaration ] { non_port_program_item } endprogram" [ : program_identifier ] 

                | { attribute_instance } "program" program_identifier ( .* ) ; [ timeunits_declaration ] { program_item } "endprogram" [ : program_identifier ] 

                | extern program_nonansi_header 
                | extern program_ansi_header
            
            program_nonansi_header ::= 
                { attribute_instance } "program" [ lifetime ] program_identifier 
                { package_import_declaration } [ parameter_port_list ] list_of_ports ; 
            
            program_ansi_header ::= 
                { attribute_instance } "program" [ lifetime ] program_identifier 
                { package_import_declaration } [ parameter_port_list ] [ list_of_port_declarations ] ;

            
            program_item ::= 
                  port_declaration ; 
                | non_port_program_item

            non_port_program_item ::= 
                  { attribute_instance } continuous_assign 
                | { attribute_instance } module_or_generate_item_declaration 
                | { attribute_instance } initial_construct 
                | { attribute_instance } final_construct 
                | { attribute_instance } concurrent_assertion_item 
                | timeunits_declaration 
                | program_generate_item
            
            program_generate_item ::= 
                  loop_generate_construct 
                | conditional_generate_construct 
                | generate_region
             */


            // "program"
            if (word.Text != "program") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            Program program = new Program();
            program.Parent = word.RootParsedDocument.Root;
            program.Project = word.Project;

            program.BuildingBlock = program;
            program.File = file;
            program.BeginIndexReference = word.CreateIndexReference();
            word.MoveNext();


            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvPair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvPair.Key, kvPair.Value);
            }


            if (!word.CellDefine && !protoType)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parseProgramItems(prototypeWord, parameterOverrides, null, program);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parseProgramItems(word, parameterOverrides, null, program);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                parseProgramItems(word, parameterOverrides, null, program);
                word.Prototype = false;
            }

            // endprogram keyword
            if (word.Text == "endprogram")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                program.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(program.BeginIndexReference, program.LastIndexReference);
                word.MoveNext();
                return program;
            }

            {
                word.AddError("endprogram expected");
            }

            return program;
        }

        /*
        program_declaration ::= 
              program_nonansi_header [ timeunits_declaration ] { program_item } "endprogram" [ : program_identifier ] 

            | program_ansi_header [ timeunits_declaration ] { non_port_program_item } endprogram" [ : program_identifier ] 

            | { attribute_instance } "program" program_identifier ( .* ) ; [ timeunits_declaration ] { program_item } "endprogram" [ : program_identifier ] 

            | extern program_nonansi_header 
            | extern program_ansi_header

        program_nonansi_header ::= 
            { attribute_instance } "program" [ lifetime ] program_identifier 
            { package_import_declaration } [ parameter_port_list ] list_of_ports ; 

        program_ansi_header ::= 
            { attribute_instance } "program" [ lifetime ] program_identifier 
            { package_import_declaration } [ parameter_port_list ] [ list_of_port_declarations ] ;


        program_item ::= 
              port_declaration ; 
            | non_port_program_item

        non_port_program_item ::= 
              { attribute_instance } continuous_assign 
            | { attribute_instance } module_or_generate_item_declaration 
            | { attribute_instance } initial_construct 
            | { attribute_instance } final_construct 
            | { attribute_instance } concurrent_assertion_item 
            | timeunits_declaration 
            | program_generate_item

        program_generate_item ::= 
              loop_generate_construct 
            | conditional_generate_construct 
            | generate_region
         */
        protected static void parseProgramItems(
            WordScanner word,
            //            string parameterOverrideModuleName,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute, Program program)
        {
            switch (word.Text)
            {
                case "static":
                    program.Static = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "automatic":
                    program.Static = false;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
            }

            // program_identifier
            program.Name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal program name");
            }
            else
            {
                program.NameReference = word.GetReference();
            }
            word.MoveNext();

            while (true)
            {
                if (word.Eof || word.Text == "endprogram")
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
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, program, null);
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
                        if (program.Constants.ContainsKey(vkp.Key))
                        {
                            if (program.Constants[vkp.Key].DefinitionRefrecnce != null)
                            {
                                //                                module.Parameters[vkp.Key].DefinitionReference.AddNotice("override " + vkp.Value.Value.ToString());
                                program.Constants[vkp.Key].DefinitionRefrecnce.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            program.Constants.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter();
                            param.Name = vkp.Key;
                            param.Expression = vkp.Value;
                            program.Constants.Add(param.Name, param);
                        }
                        else
                        {
                            //System.Diagnostics.Debug.Print("undefed params "+module.File.Name +":" + vkp.Key );
                        }
                    }
                }

                if (word.Eof || word.Text == "endprogram") break;
                if (word.Text == "(")
                {
                    parseListOfPorts_ListOfPortsDeclarations(word, program);
                } // list_of_ports or list_of_port_declarations

                if (word.Eof || word.Text == "endprogram") break;

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
                    if (!Items.ProgramItem.Parse(word, program))
                    {
                        if (word.Text == "endprogram") break;
                        word.AddError("illegal program item");
                        word.MoveNext();
                    }
                }
                //parseModuleItems(word, module);
                break;
            }

            if (!word.Prototype)
            {
                CheckVariablesUseAndDriven(word, program);
            }

            //foreach (var variable in module.Variables.Values)
            //{
            //    if (variable.DefinedReference == null) continue;
            //    variable.UsedReferences.Clear();
            //}
            return;
        }

        private CodeEditor2.CodeEditor.AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (ModuleItems.IInstantiation inst in Instantiations.Values)
            {
                if (inst.Name == null) throw new Exception();
                items.Add(newItem(inst.Name, CodeDrawStyle.ColorType.Identifier));
            }
        }


        protected static void parseListOfPorts_ListOfPortsDeclarations(WordScanner word, Program module)
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


        public override List<string> GetExitKeywords()
        {
            return new List<string>
            {
                //                "module","endmodule",
                //                "function","endfunction",
                //                "task","endtask",
                //                "always","initial",
                //                "assign","specify","endspecify",
                //                "generate","endgenerate"
            };
        }        //

    }
}
