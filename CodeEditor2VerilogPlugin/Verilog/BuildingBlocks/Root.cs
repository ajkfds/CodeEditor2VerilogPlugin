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

        /// <summary>
        /// Verifies that each forward typedef declaration resolved to a data
        /// type in the same local scope (IEEE 1800-2017 6.18).
        /// </summary>
        private static void checkForwardTypedefs(WordScanner word, ParsedDocument parsedDocument)
        {
            if (parsedDocument.ForwardTypedefs.Count == 0) return;
            foreach (ParsedDocument.ForwardTypedefDeclaration forwardTypedef in parsedDocument.ForwardTypedefs)
            {
                NameSpace nameSpace = forwardTypedef.DeclaringNameSpace;
                if (nameSpace == null) continue;
                INamedElement? element = nameSpace.GetNamedElementUpward(forwardTypedef.Name);
                // a Typedef / Class / InterfaceClass is a valid data type
                if (element is DataObjects.Typedef) continue;
                if (element is BuildingBlocks.Class) continue;
                if (element is BuildingBlocks.InterfaceClass) continue;
                forwardTypedef.Reference.AddError(
                    "forward typedef '" + forwardTypedef.Name + "' does not resolve to a data type");
            }
        }

        /// <summary>
        /// Verifies that each "extern constraint c;" declared in a class body is
        /// defined by an out-of-block "constraint class::c { ... }" declaration
        /// (IEEE 1800-2017 18.5.1). Reports the class-body prototype when its
        /// definition is missing.
        /// </summary>
        private static void checkExternConstraintsImplemented(WordScanner word, ParsedDocument parsedDocument)
        {
            if (word.Prototype) return;
            if (word.CompletionContext != null) return;
            checkExternConstraintsInNameSpace(parsedDocument.Root);
        }

        private static void checkExternConstraintsInNameSpace(NameSpace nameSpace)
        {
            foreach (INamedElement element in nameSpace.NamedElements.Values)
            {
                if (element is Coverage.ConstraintDeclaration constraint &&
                    constraint.IsExtern && !constraint.IsExternImplemented)
                {
                    constraint.AddExternNotDefinedError();
                }
                if (element is BuildingBlocks.Class class_)
                {
                    checkExternConstraintsInNameSpace(class_);
                }
            }
        }

        public static async System.Threading.Tasks.Task<Root> ParseCreateAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                IndexReference iref = word.CreateIndexReference();
                switch (word.Text)
                {
                    // module_declaration
                    case "module":
                    case "macromodule":
                        await parseModuleAsync(word, parsedDocument, file);
                        break;
                    // udp_declaration
                    case "primitive":
                        await parsePrimitiveAsync(word, parsedDocument, file);
                        break;
                    // interface_declaration
                    case "interface":
                        if (word.NextText == "class")
                        {
                            // interface_class_declaration at root level
                            // (interface_class_declaration ::= "interface" "class" ...)
                            InterfaceClass.ParseDeclaration(word, parsedDocument.Root);
                            break;
                        }
                        await parseInterfaceAsync(word, parsedDocument, file);
                        break;
                    // program_declaration
                    case "program":
                        await parseProgramAsync(word, parsedDocument, file);
                        break;
                    // bind_directive
                    case "bind":
                        await parseBindDirectiveAsync(word, parsedDocument, file);
                        break;
                    // checker_declaration
                    case "checker":
                        await parseCheckerAsync(word, parsedDocument, file);
                        break;
                    // config_declaration
                    // package_declaration
                    case "package":
                        await parsePackageAsync(word, parsedDocument, file);
                        break;
                    // extern_constraint_declaration
                    //   (constraint class_identifier :: constraint_identifier { ... })
                    case "constraint":
                        Coverage.ConstraintDeclaration.ParseCreate(word, root);
                        break;
                    default:
                        // package_item
                        Verilog.Items.PackageItem.Parse(word, root);
                        break;
                }
                if (word.CreateIndexReference().IsSameAs(iref))
                {
                    word.MoveNext();
                }
            }

            // A forward typedef declaration ("typedef identifier ;") must have a
            // matching data type definition in the same local scope; otherwise
            // the type_identifier does not resolve to a data type (IEEE
            // 1800-2017 6.18). Checked here so that forward declarations that
            // precede their definition still resolve.
            checkForwardTypedefs(word, parsedDocument);

            // An explicit external constraint ("extern constraint c;") declared
            // in a class body shall be defined outside the class by an
            // out-of-block "constraint class::c { ... }" declaration; otherwise
            // it is an error (IEEE 1800-2017 18.5.1). Checked here so that the
            // out-of-block definitions (which follow the class body) have been
            // parsed already.
            checkExternConstraintsImplemented(word, parsedDocument);

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


        private static async System.Threading.Tasks.Task parseModuleAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                module = await Module.ParseCreateAsync(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                module = await Module.ParseCreateAsync(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
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
                    word.AddPrototypeError("duplicated module name");
                }
                // If not prototype, the update is already done by AddOrUpdateBuildingBlock
            }
            // Note: ReparseRequested for ParseMode is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parsePackageAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                package = await Package.ParseCreateAsync(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                package = await Package.ParseCreateAsync(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            // Note: ReparseRequested is now set only once at the end of ParseCreate based on ParseMode
        }

        private static async System.Threading.Tasks.Task parseProgramAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                program = await Program.ParseAsync(word, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {
                program = await Program.ParseAsync(word, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
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

        private static async System.Threading.Tasks.Task parseInterfaceAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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

            Interface interface_;
            //IndexReference iref = IndexReference.Create(parsedDocument);

            // Parse mode is now handled at the end of ParseCreate, not per-block
            // This prevents cascading re-parses that cause instability
            ////if (parsedDocument.ParameterOverrides == null)
            ////{
            ////    module = await Interface.Create(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            ////}
            ////else
            {
                interface_ = await Interface.Create(word, parsedDocument.Root, parsedDocument.ParameterOverrides, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }

            bool added = parsedDocument.Root.AddOrUpdateBuildingBlock(interface_.Name, interface_);
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

        private static async System.Threading.Tasks.Task parseBindDirectiveAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
        {
            if (word.Text != "bind") throw new Exception();

            BindDirective? bindDirective;
            if (Verilog.Items.BindDirective.Parse(word, null, out bindDirective))
            {
                // Bind directive parsed successfully
                // Note: Bind directive is a compiler directive-like construct that doesn't create a building block
                // It binds instances to modules/interfaces/checkers that need to be referenced elsewhere
            }
        }

        private static async System.Threading.Tasks.Task parsePrimitiveAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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

        private static async System.Threading.Tasks.Task parseCheckerAsync(WordScanner word, ParsedDocument parsedDocument, Data.VerilogFile file)
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
                checker = Checker.ParseCreate(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
            }
            else
            {   // TODO :  parameter override parse implementation for checker, currently just pass null
                checker = Checker.ParseCreate(word, parsedDocument.Root, null, parsedDocument.Root, file, parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse);
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
