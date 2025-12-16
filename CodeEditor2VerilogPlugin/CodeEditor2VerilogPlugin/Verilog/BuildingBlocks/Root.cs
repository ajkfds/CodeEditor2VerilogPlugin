using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{

    /* Verilog 2001
        source_text ::= { description }
        description ::= module_declaration
                        | udp_declaration
        module_declaration ::=      { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                    [ list_of_ports ] ; { module_item }
                                    endmodule

                                    | { attribute_instance } module_keyword module_identifier [ module_parameter_port_list ]
                                    [ list_of_port_declarations ] ; { non_port_module_item }
                                    endmodule
        module_keyword ::= module | macromodule
    */

    /* System Verilog 2012
    source_text ::= [ timeunits_declaration ] { description } 
    description ::= 
          module_declaration 
        | udp_declaration 
        | interface_declaration 
        | program_declaration         
        | package_declaration 
        | { attribute_instance } package_item 
        | { attribute_instance } bind_directive 
        | config_declaration 

    module_nonansi_header ::= 
        { attribute_instance } module_keyword [ lifetime ] module_identifier 
        { package_import_declaration } [ parameter_port_list ] list_of_ports ;

    module_ansi_header ::= 
        { attribute_instance } module_keyword [ lifetime ] module_identifier 
        { package_import_declaration }1 [ parameter_port_list ] [ list_of_port_declarations ] ;

    module_declaration ::= 
            module_nonansi_header [ timeunits_declaration ] { module_item } 
            endmodule [ : module_identifier ] 

            | module_ansi_header [ timeunits_declaration ] { non_port_module_item } 
            endmodule [ : module_identifier ] 

            | { attribute_instance } module_keyword [ lifetime ] module_identifier ( .* ) ;
            [ timeunits_declaration ] { module_item } endmodule [ : module_identifier ] 

            | extern module_nonansi_header 
            | extern module_ansi_header 
    module_keyword ::= module | macromodule

    */
    public class Root : BuildingBlocks.BuildingBlock
    {
        protected Root() : base(null, null)
        {

        }

//        public Dictionary<string, BuildingBlock> BuldingBlocks = new Dictionary<string, BuildingBlock>();


        public BuildingBlock? GetBuildingBlock(IndexReference indexReference)
        {
            foreach (BuildingBlock buildingBlock in BuildingBlocks.Values)
            {
                if (buildingBlock.BeginIndexReference == null) continue;
                if (buildingBlock.LastIndexReference == null) continue;

                if (indexReference.IsSmallerThan(buildingBlock.BeginIndexReference)) continue;
                if (indexReference.IsGreaterThan(buildingBlock.LastIndexReference)) continue;
                return buildingBlock;
            }
            return null;
        }
        public static async System.Threading.Tasks.Task<Root> ParseCreate(WordScanner word, ParsedDocument parsedDocument,Data.VerilogFile file)
        {
            Root root = new Root()
            {
                BeginIndexReference = word.CreateIndexReference(),
                DefinitionReference = word.CrateWordReference(),
                File = file,
                Name = "$root",
                Parent = null,
                Project = word.Project
            };
            root.BuildingBlock = root;
            parsedDocument.Root = root;

            while (!word.Eof)
            {
                switch (word.Text)
                {
                    // module_declaration
                    case "module":
                    case "macromodule":
                        await parseModule(word, parsedDocument, file);
                        break;
                    // udp_declaration
                    // interface_declaration
                    case "interface":
                        await parseInterface(word, parsedDocument, file);
                        break;
                    // program_declaration
                    case "program":
                        await parseProgram(word, parsedDocument, file);
                        break;
                    // bind_directive
                    // config_declaration
                    // package_declaration
                    case "package":
                        await parsePackage(word, parsedDocument, file);
                        break;
                    default:
                        // package_item
                        if (!await Items.PackageItem.Parse(word, root))
                        {
                            word.MoveNext();
                        }
                        break;

                }
            }

            return root;
        }


        private static async System.Threading.Tasks.Task parseModule(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "module" && word.Text != "macromodule") throw new Exception();

            // skip block
            if (parsedDocument.TargetBuildingBlockName != null)
            {
                string moduleName = word.NextText;
                if (moduleName != parsedDocument.TargetBuildingBlockName)
                {
                    word.MoveNext();
                    IndexReference beginReference = word.CreateIndexReference();

                    List<string> stopWords = new List<string> { "endmodule" };
                    while (!word.Eof)
                    {
                        if (stopWords.Contains(word.Text)) break;   // TODO support hier module definition
                        word.Color(CodeDrawStyle.ColorType.Inactivated);
                        word.MoveNext();
                    }

                    IndexReference lastReference = word.CreateIndexReference();

                    word.AppendBlock(beginReference, lastReference, moduleName, true);

                    return;
                }
            }


            Module module;

            if (parsedDocument.ParseMode == Parser.VerilogInstanceParser.ParseModeEnum.LoadParse)
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    module = await Module.ParseCreate(word, null, parsedDocument.Root, file, true);
                }
                else
                {
                    module = await Module.ParseCreate(word, parsedDocument.ParameterOverrides, null , parsedDocument.Root, file, true);
                }
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    module = await Module.ParseCreate(word, null, parsedDocument.Root, file, false);
                }
                else
                {
                    module = await Module.ParseCreate(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, false);
                }
            }

        }

        private static async System.Threading.Tasks.Task parsePackage(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "package") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                string moduleName = word.NextText;
                if (moduleName != parsedDocument.TargetBuildingBlockName)
                {
                    word.SkipToKeyword("endpackage");
                    word.MoveNext();
                    return;
                }
            }


            Package package;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            if (parsedDocument.ParseMode == Parser.VerilogInstanceParser.ParseModeEnum.LoadParse)
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    package = await Package.Create(word, null, parsedDocument.Root, file, true);
                }
                else
                {
                    package = await Package.Create(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, true);
                }
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    package = await Package.Create(word, null, parsedDocument.Root, file, false);
                }
                else
                {
                    package = await Package.Create(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, false);
                }
            }
        }

        private static async System.Threading.Tasks.Task parseProgram(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "program") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                string moduleName = word.NextText;
                if (moduleName != parsedDocument.TargetBuildingBlockName)
                {
                    word.SkipToKeyword("endprogram");
                    word.MoveNext();
                    return;
                }
            }


            Program program;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            if (parsedDocument.ParseMode == Parser.VerilogInstanceParser.ParseModeEnum.LoadParse)
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    program = await Program.Parse(word, null, parsedDocument.Root, file, true);
                }
                else
                {
                    program = await Program.Parse(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, true);
                }
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    program = await Program.Parse(word, null, parsedDocument.Root, file,  false);
                }
                else
                {
                    program = await Program.Parse(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, false);
                }
            }

            if (!parsedDocument.Root.BuildingBlocks.ContainsKey(program.Name))
            {
                parsedDocument.Root.BuildingBlocks.Add(program.Name, program);
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                word.AddError("duplicated module name");
            }
        }

        private static async System.Threading.Tasks.Task parseInterface(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "interface") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                string moduleName = word.NextText;
                if (moduleName != parsedDocument.TargetBuildingBlockName)
                {
                    word.SkipToKeyword("endinterface");
                    word.MoveNext();
                    return;
                }
            }

            Interface module;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            if (parsedDocument.ParseMode == Parser.VerilogInstanceParser.ParseModeEnum.LoadParse)
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    module = await Interface.Create(word, parsedDocument.Root, null, parsedDocument.Root, file, true);
                }
                else
                {
                    module = await Interface.Create(word, parsedDocument.Root, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, true);
                }
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (parsedDocument.ParameterOverrides == null)
                {
                    module = await Interface.Create(word, parsedDocument.Root, null, parsedDocument.Root, file, false);
                }
                else
                {
                    module = await Interface.Create(word, parsedDocument.Root, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, false);
                }
            }

            if (!parsedDocument.Root.BuildingBlocks.ContainsKey(module.Name))
            {
                parsedDocument.Root.BuildingBlocks.Add(module.Name, module);
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (word.Prototype)
                {
                    word.AddPrototypeError("duplicated buldingblock name");
                }
                else
                {
                    parsedDocument.Root.BuildingBlocks[module.Name] = module;
                }
            }
        }

    }
}
