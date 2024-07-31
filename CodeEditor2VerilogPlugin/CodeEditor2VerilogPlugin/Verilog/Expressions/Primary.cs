using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class Primary : Expression
    {
        protected Primary() {
            Constant = false;
        }
        
//        public virtual bool Constant { get; protected set; }
//        public virtual double? Value { get; protected set; }
//        public virtual int? BitWidth { get; protected set; }
        //        public bool Signed { get; protected set; }
//        public WordReference Reference { get; protected set; }

        public static Primary Create(bool constant, double? value, int? bitWidth)
        {
            Primary primary = new Primary();
            primary.Constant = constant;
            primary.Value = value;
            primary.BitWidth = bitWidth;
            return primary;
        }

        public virtual AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Contorls.ColorLabel label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            AppendLabel(label);
            return label;
        }

        public override string CreateString()
        {
            return "";
        }

        /*
         * 
         * 
         A.8.4 Primaries
        constant_primary    ::= constant_concatenation
                                | constant_function_call
                                | ( constant_mintypmax_expression )
                                | constant_multiple_concatenation
                                | genvar_identifier
                                | number
                                | parameter_identifier
                                | specparam_identifier  
        module_path_primary ::= number
                                | identifier
                                | module_path_concatenation
                                | module_path_multiple_concatenation
                                | function_call          
                                | system_function_call          
                                | constant_function_call          
                                | ( module_path_mintypmax_expression )  
        primary             ::= number
                                | concatenation          
                                | multiple_concatenation
                                | function_call 
                                | system_function_call
                                | constant_function_call
                                | ( mintypmax_expression )
                                | hierarchical_identifier
                                | hierarchical_identifier [ expression ] { [ expression ] }
                                | hierarchical_identifier [ expression ] { [ expression ] }  [ range_expression ]
                                | hierarchical_identifier [ range_expression ]

        ## SystemVerilog2017
        primary     ::=   primary_literal 
                        | [ class_qualifier | package_scope ] hierarchical_identifier select 
                        | empty_queue 
                        | concatenation [ [ range_expression ] ] 
                        | multiple_concatenation [ [ range_expression ] ] 
                        | function_subroutine_call 
                        | let_expression 
                        | ( mintypmax_expression )
                        | cast 
                        | assignment_pattern_expression 
                        | streaming_concatenation
                        | sequence_method_call 
                        | "this"
                        | "$"
                        | "null"

        cast            ::=  casting_type "`" "(" expression ")"
        casting_type    ::=  simple_type | constant_primary | signing | "string" | "const"
        simple_type     ::= integer_type | non_integer_type | ps_type_identifier | ps_parameter_identifier 

        */
        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return parseCreate(word, nameSpace, false);
        }
        public static Primary? ParseCreateLValue(WordScanner word, NameSpace nameSpace)
        {
            return parseCreate(word, nameSpace, true);
        }
        private static Primary? parseCreate(WordScanner word, NameSpace nameSpace,bool lValue)
        {
            //if (word.Text == "srif") System.Diagnostics.Debugger.Break();

            switch (word.WordType)
            {
                case WordPointer.WordTypeEnum.Number:
                    return Number.ParseCreate(word);
                case WordPointer.WordTypeEnum.Symbol:
                    if (word.GetCharAt(0) == '{')
                    {
                        return Concatenation.ParseCreateConcatenationOrMultipleConcatenation(word, nameSpace, lValue);
                    }else if(word.GetCharAt(0) == '(')
                    {
                        return Bracket.ParseCreateBracketOrMinTypMax(word, nameSpace);
                    }
                    return null;
                case WordPointer.WordTypeEnum.String:
                    return ConstantString.ParseCreate(word);
                case WordPointer.WordTypeEnum.Text:
                    {
                        var variable = parseVariable(word, nameSpace, lValue);
                        if (variable != null) return variable;

                        var parameter = ParameterReference.ParseCreate(word, nameSpace);
                        if (parameter != null) return parameter;

                        if (word.Text.StartsWith("$") && word.RootParsedDocument.ProjectProperty.SystemFunctions.Keys.Contains(word.Text))
                        {
                            return FunctionCall.ParseCreate(word, nameSpace);
                        }

                        if (word.NextText == "(" & !lValue)
                        {
                            return FunctionCall.ParseCreate(word, nameSpace);
                        }

                        if (word.NextText == "'") // cast
                        {
                            return Cast.ParseCreate(word, nameSpace);
                        }

                        {
                            Primary? primary = parseHierarchyVariable(word, nameSpace, lValue);
                            if (primary != null) return primary; 
                        }
                        break;
                    }
            }
            return null;
        }

        public static Primary? parseHierarchyVariable(WordScanner word, NameSpace nameSpace, bool lValue)
        {
            NameSpace space = nameSpace;
            Primary? primary = null;
            parseHierarchyNameSpace(word, nameSpace, ref space, ref primary, lValue);

            if (primary == null || space == null)// || space == nameSpace || space is Class)
            {
                if (word.Eof) return null;
                if (General.ListOfKeywords.Contains(word.Text)) return null;

                if (General.IsIdentifier(word.Text) && !nameSpace.DataObjects.ContainsKey(word.Text) && !word.Prototype)
                {   // undefined net
                    if (!word.CellDefine) word.AddWarning("undefined");
                    Net net = new DataObjects.Nets.Net();
                    net.Name = word.Text;
                    net.Signed = false;
                    if (word.Active)
                    {
                        nameSpace.DataObjects.Add(net.Name, net);
                    }
                    var variable = VariableReference.ParseCreate(word, nameSpace, lValue);

                    if (variable != null)
                    {
                        return variable;
                    }

                }
            }
            else if (primary is TaskReference)
            {
                return primary;
            }
            else if (space != null)
            {
                if (space.DataObjects.ContainsKey(word.Text))
                {
                    return VariableReference.ParseCreate(word, space, lValue);
                }
                if (lValue && space.BuildingBlock.Tasks.ContainsKey(word.Text))
                {
                    return TaskReference.ParseCreate(word, space);
                }
                return primary;
            }
            return null;
        }

        public static Primary? parseVariable(WordScanner word, NameSpace nameSpace, bool lValue)
        {
            var variable = VariableReference.ParseCreate(word, nameSpace, lValue);
            if (variable == null) return null;

            if (variable.Variable is DataObjects.Variables.Object && word.Text == ".")
            {
                word.MoveNext();
                DataObjects.Variables.Object? obj = variable.Variable as DataObjects.Variables.Object;
                if (obj != null) return Primary.parseCreate(word, obj.Class, lValue);
                else throw new Exception();
            }

            if (variable.Variable is DataObjects.InterfaceInstantiation && word.Text == ".")
            {
                word.MoveNext();
                DataObjects.InterfaceInstantiation? interfaceInstantiation = variable.Variable as DataObjects.InterfaceInstantiation;
                if (interfaceInstantiation == null) throw new Exception();
                Interface? interface_ = nameSpace.ProjectProperty.GetBuildingBlock(interfaceInstantiation.SourceName) as Interface;
                if (interface_ != null)
                {
                    if (interface_.ModPorts.ContainsKey(word.Text))
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        interfaceInstantiation.ModPortName = word.Text;
                        ModPort modPort = interface_.ModPorts[interfaceInstantiation.ModPortName];
                        word.MoveNext();
                        if (word.Text == ".")
                        {
                            word.MoveNext();
                            Primary.parseCreate(word, modPort, lValue);
                        }
                    }
                    else
                    {
                        Primary.parseCreate(word, interface_, lValue);
                    }
                }
            }

            return variable;
        }

        public static void parseHierarchyNameSpace(WordScanner word, NameSpace rootNameSpace, ref NameSpace nameSpace,ref Primary? primary,bool assigned)
        {
            if(parseKnownHierarchyNameSpace(word,rootNameSpace,ref nameSpace, ref primary, assigned))
            {
                // parsed
                return;
            }else if (word.NextText == "(")
            {
                // task reference : for left side only
                // function call : for right side only
                if (assigned)
                { // left value
                    TaskReference taskReference = TaskReference.ParseCreate(word, rootNameSpace, nameSpace);
                    primary = taskReference;
                    return;
                }
                else
                {
                    primary = FunctionCall.ParseCreate(word,rootNameSpace, nameSpace);
                }
            }
            else if (nameSpace.NameSpaces.ContainsKey(word.Text))
            {
                nameSpace = nameSpace.NameSpaces[word.Text];
                if(assigned && word.NextText == ";" && nameSpace is Task)
                {
                    TaskReference taskReference = TaskReference.ParseCreate(word, rootNameSpace, nameSpace);
                    primary = taskReference;
                    return;
                }

                word.Color(CodeDrawStyle.ColorType.Identifier);
                NameSpaceReference nameSpaceReference = new NameSpaceReference(nameSpace);
                primary = nameSpaceReference;
                primary.Reference = word.GetReference();
                word.MoveNext();


                if (word.Text == ".")
                {
                    word.MoveNext();    // .
                    parseHierarchyNameSpace(word, rootNameSpace, ref nameSpace, ref primary,assigned);
                }
                else
                {
                    if(nameSpace != null && rootNameSpace != nameSpace)
                    {
                        Primary newPrimary = new NameSpaceReference(nameSpace);
                        newPrimary.Reference = primary.Reference;
                        primary = newPrimary;
                    }
                    return;
                }
            }
            else
            {
                var variable = VariableReference.ParseCreate(word, nameSpace,assigned);
                if (variable != null)
                {
                    primary = variable;
                    return;
                }

                var parameter = ParameterReference.ParseCreate(word, nameSpace);
                if (parameter != null)
                {
                    primary = parameter;
                    return;
                }

                return;
            }
        }

        public static bool parseKnownHierarchyNameSpace(WordScanner word, NameSpace rootNameSpace, ref NameSpace nameSpace, ref Primary? primary, bool assigned)
        {
            BuildingBlock buildingBlock = nameSpace.BuildingBlock;
            if (!buildingBlock.Instantiations.ContainsKey(word.Text)) return false;

            IInstantiation instantiation = buildingBlock.Instantiations[word.Text];
            if(instantiation is ModuleInstantiation)
            {
                ModuleInstantiation? mInst = instantiation as ModuleInstantiation;
                if (mInst == null) throw new Exception();

                ModuleInstanceReference moduleInstanceReference = new ModuleInstanceReference(mInst);
                primary = moduleInstanceReference;
                nameSpace = mInst.GetInstancedBuildingBlock();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (nameSpace == null) return true;

                if (word.Text == ".")
                {
                    word.MoveNext();    // .
                    parseHierarchyNameSpace(word, rootNameSpace, ref nameSpace, ref primary, assigned);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else if(instantiation is InterfaceInstantiation)
            {
                InterfaceInstantiation? iInst = instantiation as InterfaceInstantiation;
                if (iInst == null) throw new Exception();
                InterfaceReference interfaceInstanceReference = new InterfaceReference(iInst);
                interfaceInstanceReference.Reference = word.GetReference();

                primary = interfaceInstanceReference;
                BuildingBlock bBlock = iInst.GetInstancedBuildingBlock();
                nameSpace = bBlock;

                if (iInst.ModPortName != null)
                {
                    Interface? interface_ = bBlock as Interface;
                    if(interface_ != null && interface_.ModPorts.ContainsKey(iInst.ModPortName))
                    {
                        nameSpace = interface_.ModPorts[iInst.ModPortName];
                    }
                }

                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();
                if (nameSpace == null) return true;

                if (word.Text == ".")
                {
                    word.MoveNext();    // .
                    parseHierarchyNameSpace(word, rootNameSpace, ref nameSpace, ref primary, assigned);
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }

        }



        private static Primary? subParseCreate(WordScanner word, NameSpace nameSpace,bool lValue)
        {
            if (nameSpace == null) System.Diagnostics.Debugger.Break();
            switch (word.WordType)
            {
                case WordPointer.WordTypeEnum.Number:
                    return null;
                case WordPointer.WordTypeEnum.Symbol:
                    return null;
                case WordPointer.WordTypeEnum.String:
                    return null;
                case WordPointer.WordTypeEnum.Text:
                    {
                        var variable = VariableReference.ParseCreate(word, nameSpace,lValue);
                        if (variable != null) return variable;

                        var parameter = ParameterReference.ParseCreate(word, nameSpace);
                        if (parameter != null) return parameter;

                        if (!lValue && word.NextText == "(")
                        {
                            return FunctionCall.ParseCreate(word, nameSpace);
                        }

                        if (word.NextText == ".")
                        {
                            if(nameSpace.BuildingBlock is IBuildingBlockWithModuleInstance &&
                               (nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance).Instantiations.ContainsKey(word.Text) )
                            { // module instance

                                IBuildingBlockWithModuleInstance buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;
                                
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                string moduleName = buildingBlock.Instantiations[word.Text].SourceName;
                                BuildingBlock module = word.RootParsedDocument.ProjectProperty.GetBuildingBlock(moduleName);
                                if (module == null) return null;
                                word.MoveNext();
                                word.MoveNext(); // .

                                Primary primary = subParseCreate(word, module,lValue);
                                if (primary == null)
                                {
                                    word.AddError("illegal variable");
                                }
                                return primary;
                            } else if (nameSpace.NameSpaces.ContainsKey(word.Text))
                            { // namespaces
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                NameSpace space = nameSpace.NameSpaces[word.Text];
                                if (space == null) return null;
                                word.MoveNext();
                                word.MoveNext(); // .

                                Primary primary = subParseCreate(word, space, lValue);
                                if (primary == null)
                                {
                                    word.AddError("illegal variable");
                                }
                                return primary;
                            }
                        }
                        else
                        {
                            if (nameSpace.BuildingBlock is IBuildingBlockWithModuleInstance &&
                               (nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance).Instantiations.ContainsKey(word.Text))
                            { // module instance
                                IBuildingBlockWithModuleInstance buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;

                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                string moduleName = buildingBlock.Instantiations[word.Text].SourceName;
                                BuildingBlock module = word.RootParsedDocument.ProjectProperty.GetBuildingBlock(moduleName);
                                if (module == null) return null;
                                word.MoveNext();

                                if(word.Text == ".")
                                {
                                    word.MoveNext();
                                    Primary primary = subParseCreate(word, module, lValue);
                                    if (primary == null)
                                    {
                                        word.AddError("illegal variable");
                                    }
                                    return new NameSpaceReference(module);
                                }
                                else
                                {
                                    return new NameSpaceReference(module);
                                }

                            }else if(nameSpace is BuildingBlock && nameSpace.BuildingBlock.Tasks.ContainsKey(word.Text))
                            {
                                return TaskReference.ParseCreate(word, nameSpace.BuildingBlock);
                            }
                            else if (nameSpace.NameSpaces.ContainsKey(word.Text))
                            {
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                NameSpace space = nameSpace.NameSpaces[word.Text];
                                if (space == null) return null;
                                word.MoveNext();
                                return new NameSpaceReference(space);

                            }
                        }

                        if (word.Eof || General.ListOfKeywords.Contains(word.Text))
                        {
                            return new NameSpaceReference(nameSpace);
                        }
                    }
                    break;
            }
            return null;
        }


    }






}



