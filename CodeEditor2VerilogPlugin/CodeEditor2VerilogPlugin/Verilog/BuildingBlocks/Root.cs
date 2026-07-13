using System;
using System.Collections.Generic;
using pluginVerilog.Verilog.Items;

namespace pluginVerilog.Verilog.BuildingBlocks
{

    /* Verilog 2001
        source_text ::= { description }
        description ::= module_declaration
                        | udp_declaration
                        | checker_declaration
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

    /*
    description ::=
          module_declaration
        | udp_declaration
        | interface_declaration
        | program_declaration 
        | package_declaration
        | checker_declaration
        | { attribute_instance } package_item
        | { attribute_instance } bind_directive
        | config_declaration

    package_item ::=
          package_or_generate_item_declaration
        | anonymous_program
        | package_export_declaration
        | timeunits_declaration

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
        | assertion_item_declaration
        | ;

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

        public static async System.Threading.Tasks.Task<Root> ParseCreate(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                    case "primitive":
                        await parsePrimitive(word, parsedDocument, file);
                        break;
                    // interface_declaration
                    case "interface":
                        await parseInterface(word, parsedDocument, file);
                        break;
                    // program_declaration
                    case "program":
                        await parseProgram(word, parsedDocument, file);
                        break;
                    // bind_directive
                    case "bind":
                        await parseBindDirective(word, parsedDocument, file);
                        break;
                    // checker_declaration
                    case "checker":
                        await parseChecker(word, parsedDocument, file);
                        break;
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

            switch (parsedDocument.ParseMode)
            {
                case CodeEditor2.CodeEditor.Parser.DocumentParser.ParseModeEnum.LoadParse:
                    parsedDocument.ReparseRequested = true;
                    break;
                default:
                    parsedDocument.ReparseRequested = false;
                    break;
            }
            return root;
        }


        private static async System.Threading.Tasks.Task parseModule(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "module" && word.Text != "macromodule") throw new Exception();

            // skip block
            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "module", "endmodule");
                    return;
                }
            }


            Module module;

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                module = await Module.ParseCreate(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                module = await Module.ParseCreate(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            // Note: ReparseRequested is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parsePackage(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "package") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "package", "endpackage");
                    return;
                }
            }

            Package package;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                package = await Package.ParseCreate(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                package = await Package.ParseCreate(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            // Note: ReparseRequested is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parseProgram(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "program") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "program", "endprogram");
                    return;
                }
            }


            Program program;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                program = await Program.Parse(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                program = await Program.Parse(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }

            bool added = parsedDocument.Root.AddOrUpdateBuildingBlock(program.Name, program);
            if (added)
            {
                // Only set ReparseRequested if building block was actually added for the first time
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                word.AddError("duplicated module name");
            }
            // Note: ReparseRequested for ParseMode is now set only once at the end of ParseCreate based on ParseMode
        }

        private static void skipBlock(WordScanner word, string startWord, string endWord)
        {
            if (word.Text != startWord) return;
            word.MoveNext();
            IndexReference beginReference = word.CreateIndexReference();

            string identifier = word.Text;

            List<string> stopWords = new List<string> { "endmodule" };
            while (!word.Eof)
            {
                if (stopWords.Contains(word.Text)) break;   // TODO support hier module definition
                word.Color(CodeDrawStyle.ColorType.Inactivated);
                word.MoveNext();
            }
            if (word.Text == endWord) word.MoveNext();
            if (word.Text == ":")
            {
                word.MoveNext();
                if (word.Text == identifier) word.MoveNext();
            }
            IndexReference lastReference = word.CreateIndexReference();

            word.AppendBlock(beginReference, lastReference, identifier, true);

            return;

        }

        private static async System.Threading.Tasks.Task parseInterface(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "interface") System.Diagnostics.Debugger.Break();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "interface", "endinterface");
                    return;
                }
            }

            Interface module;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                module = await Interface.Create(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                module = await Interface.Create(word, parsedDocument.Root, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }

            bool added = parsedDocument.Root.AddOrUpdateBuildingBlock(module.Name, module);
            if (added)
            {
                // Only set ReparseRequested if building block was actually added for the first time
                parsedDocument.ReparseRequested = true;
            }
            else
            {
                if (word.Prototype)
                {
                    word.AddPrototypeError("duplicated buldingblock name");
                }
                // If not prototype, the update is already done by AddOrUpdateBuildingBlock
            }
            // Note: ReparseRequested for ParseMode is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parseBindDirective(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "bind") throw new Exception();

            BindDirective? bindDirective;
            if (Items.BindDirective.Parse(word, null, out bindDirective))
            {
                // Bind directive parsed successfully
                // Note: Bind directive is a compiler directive-like construct that doesn't create a building block
                // It binds instances to modules/interfaces/checkers that need to be referenced elsewhere
            }
        }

        private static async System.Threading.Tasks.Task parsePrimitive(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "primitive") throw new Exception();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "primitive", "endprimitive");
                    return;
                }
            }

            Primitive? primitive;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                primitive = await Primitive.ParseCreate(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                primitive = await Primitive.ParseCreate(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            // Note: ReparseRequested is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parseChecker(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "checker") throw new Exception();

            if (parsedDocument.TargetBuildingBlockName != null)
            {
                if (word.NextText != parsedDocument.TargetBuildingBlockName)
                {
                    skipBlock(word, "checker", "endchecker");
                    return;
                }
            }

            Checker? checker;
            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            if (parsedDocument.ParameterOverrides == null)
            {
                checker = await Checker.ParseCreate(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {   // TODO :  parameter override parse implementation for checker, currently just pass null
                checker = await Checker.ParseCreate(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }

            if (checker != null)
            {
                bool added = parsedDocument.Root.AddOrUpdateBuildingBlock(checker.Name, checker);
                if (added)
                {
                    // Only set ReparseRequested if building block was actually added for the first time
                    parsedDocument.ReparseRequested = true;
                }
                else
                {
                    if (word.Prototype)
                    {
                        word.AddPrototypeError("duplicated checker name");
                    }
                }
            }
        }

    }
}
