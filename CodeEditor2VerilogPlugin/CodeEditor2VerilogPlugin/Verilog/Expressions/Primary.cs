using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.OpenGL;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog.Expressions
{
    public abstract class Primary : Expression
    {
        protected Primary() {
            Constant = false;
        }
        
//        public virtual bool Constant { get; protected set; }
//        public virtual double? Value { get; protected set; }
//        public virtual int? BitWidth { get; protected set; }
        //        public bool Signed { get; protected set; }
//        public WordReference Reference { get; protected set; }

        //public static Primary Create(bool constant, double? value, int? bitWidth)
        //{
        //    Primary primary = new Primary();
        //    primary.Constant = constant;
        //    primary.Value = value;
        //    primary.BitWidth = bitWidth;
        //    return primary;
        //}

        public virtual AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();
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

41) implicit_class_handle shall only appear within the scope of a class_declaration or out-of-block method declaration.
42) The $ primary shall be legal only in a select for a queue variable, in an open_value_range, covergroup_value_
range, integer_covergroup_expression, or as an entire sequence_actual_arg or property_actual_arg.


constant_primary ::=
primary_literal
| ps_parameter_identifier constant_select
| specparam_identifier [ [ constant_range_expression ] ]
| genvar_identifier39
| formal_port_identifier constant_select
| [ package_scope | class_scope ] enum_identifier
| constant_concatenation [ [ constant_range_expression ] ]
| constant_multiple_concatenation [ [ constant_range_expression ] ]
| constant_function_call
| constant_let_expression
| ( constant_mintypmax_expression )
| constant_cast
| constant_assignment_pattern_expression
| type_reference40
| "null"

module_path_primary ::=
number
| identifier
| module_path_concatenation
| module_path_multiple_concatenation
| function_subroutine_call
| ( module_path_mintypmax_expression )


        */
        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            return parseCreate(word, nameSpace, false, acceptImplicitNet);
        }
        public static Primary? ParseCreateLValue(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            return parseCreate(word, nameSpace, true, acceptImplicitNet);
        }
        private static Primary? parseCreate(WordScanner word, NameSpace nameSpace,bool lValue,bool acceptImplicitNet)
        {
            //if (word.Text == "srif") System.Diagnostics.Debugger.Break();

            switch (word.WordType)
            {
                case WordPointer.WordTypeEnum.Number:
                    return Number.ParseCreateNumberOrCast(word, nameSpace, lValue);
                case WordPointer.WordTypeEnum.Symbol:
                    if (word.GetCharAt(0) == '{')
                    {
                        return Concatenation.ParseCreateConcatenationOrMultipleConcatenation(word, nameSpace, lValue, acceptImplicitNet);
                    }else if(word.GetCharAt(0) == '(')
                    {
                        return Bracket.ParseCreateBracketOrMinTypMax(word, nameSpace);
                    }
                    return null;
                case WordPointer.WordTypeEnum.String:
                    return ConstantString.ParseCreate(word,nameSpace);
                case WordPointer.WordTypeEnum.Text:
                    // null
                    if(word.Text == "null")
                    {
                        return Null.ParseCreate(word, nameSpace);
                    }
                    // dollar primitive
                    if(word.Text == "$")
                    {
                        return DollarMark.ParseCreate(word, nameSpace);
                    }
                    // system function call
                    if (word.Text.StartsWith("$"))// && word.ProjectProperty.SystemFunctions.Keys.Contains(word.Text))
                    {
                        return FunctionCall.ParseCreate(word, nameSpace,nameSpace);
                    }

                    if(word.Text == "MyObject")
                    {
                        string a = "";
                    }

                    // assignment pattern
                    if(word.Text =="'" && word.NextText == "{")
                    {
                        return AssignmentPattern.ParseCreate(word, nameSpace);
                    }

                    // cast
                    if (word.NextText == "'") // cast
                    {
                        return Cast.ParseCreate(word, nameSpace);
                    }

                    // keyword
                    if (General.ListOfKeywords.Contains(word.Text))
                    {
                        return null;
                    }

                    // abosrt if not ideftifier
                    if (!General.IsIdentifier(word.Text))
                    {
                        return null;
                    }


                    // function call (function recarsive call)
                    if (word.NextText=="(" && word.Text == nameSpace.Name)
                    {
                        // It shall be illegal to omit the parentheses in a tf_call unless the subroutine is a task, void function,
                        // or class method. If the subroutine is a nonvoid class function method, it shall be illegal to omit the parentheses if the call is directly recursive.
                        return FunctionCall.ParseCreate(word, nameSpace, nameSpace);
                    }

                    string nameSpaceText = "";
                    NameSpace? targetNameSpace = nameSpace;

                    // Class scope resolution operator
                    if (word.NextText == "::")
                    {
                        string a = "";
                    }

                    if (word.Text == "this" && word.NextText == ".")
                    {

                        // The this keyword shall only be used within
                        // non -static class methods,
                        // constraints,
                        // inlined constraint methods, or covergroups embedded within classes(see 19.4);
                        // otherwise, an error shall be issued.

                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        word.MoveNext();
                        targetNameSpace = nameSpace.BuildingBlock;
                        nameSpaceText = "this.";
                    }
                    else
                    {
                        { // search upward
                            NameSpace? searchUpwardNameSpace = null;
                            INamedElement? upwardElement = nameSpace.GetNamedElementUpward(word.Text, out searchUpwardNameSpace);
                            if (upwardElement != null)
                            {
                                acceptImplicitNet = false;
                                targetNameSpace = searchUpwardNameSpace;
                            }
                            if (targetNameSpace == null) targetNameSpace = nameSpace;
                        }

                        // search downward
                        NameSpace? searchDownwardNameSpace = searchNameSpace(word, targetNameSpace, ref nameSpaceText);
                        if (searchDownwardNameSpace != null)
                        {
                            if (nameSpaceText != "") acceptImplicitNet = false;
                            targetNameSpace = searchDownwardNameSpace;
                        }
                        else
                        {
                            if (targetNameSpace == null)
                            {
                                targetNameSpace = nameSpace;
                            }
                        }
                        if(targetNameSpace is BuildingBlocks.Class)
                        {
                            if(word.Text == "::")
                            {
                                word.MoveNext();
                            }
                        }else if(targetNameSpace is BuildingBlocks.Module)
                        {

                        }

                    }

                    INamedElement? element = null;
                    if (targetNameSpace.NamedElements.ContainsKey(word.Text))
                    {
                        element = targetNameSpace.NamedElements[word.Text];
                    }
                    else
                    {
                        element = targetNameSpace.GetNamedElementUpward(word.Text);
                    }
                    
                    // variable reference
                    if (element is DataObject)
                    {
                        return parseDataObject(word, nameSpace, targetNameSpace, lValue, nameSpaceText);
                    }

                    // Since Task and Function are also namespaces, they need to be processed before namespaces.
                    // task reference : for left side only
                    if (lValue && element is Task)
                    {
                        return TaskReference.ParseCreate(word, targetNameSpace.BuildingBlock, nameSpace);
                    }

                    // function call : for right side only
                    if (!lValue && element is Function)
                    {
                        return FunctionCall.ParseCreate(word, targetNameSpace, nameSpace);
                    }

                    if (element is DataObjects.Constants.Constants)
                    {
                        return ParameterReference.ParseCreate(word, targetNameSpace);
                    }

                    if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
                    {
                        return null;
                    }

                    if (word.NextText == "(")
                    {
                        return parseUndefinedFunction(word);
                    }
                    //else if (word.NextText == ";")
                    //{
                    //    return parseUndefinedFunction(word);
                    //}



                    // implicit net declaration
                    if (acceptImplicitNet)
                    {
                        Net net = DataObjects.Nets.Net.Create(word.Text, DataObjects.Nets.Net.NetTypeEnum.Wire, null);
                        net.DefinedReference = word.GetReference();

                        if (!word.Prototype)
                        {
                            nameSpace.NamedElements.Add(net.Name, net);
                            word.ApplyRule(word.ProjectProperty.RuleSet.ImplicitNetDeclaretion);
                        }

                        return parseDataObject(word, nameSpace, targetNameSpace, lValue, nameSpaceText);
                    }

                    return null;
            }
            return null;
        }

        public static NameSpace? searchNameSpace(WordScanner word, NameSpace nameSpace,ref string nameSpaceText)
        {
            if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
            {
                return nameSpace;
            }
            if(word.NextText=="(" || word.NextText == ";")
            {
                return nameSpace;
            }

            if (!nameSpace.NamedElements.ContainsKey(word.Text))
            {   // namespace not found
                if (word.NextText == ".")
                { // unfound heirarchy
                    return searchUnfoundNameSpace(word, ref nameSpaceText);
                }
                else
                {
                    return nameSpace;    // implicit net declaration?
                }
            }

            INamedElement element = nameSpace.NamedElements[word.Text];

            if (element is NameSpace)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                NameSpace newNameSpace = (NameSpace)element;
                if(nameSpaceText =="")
                {
                    nameSpaceText = newNameSpace.Name;
                }
                else
                {
                    nameSpaceText = nameSpaceText + "." + newNameSpace.Name;
                }
                return searchNameSpace(word,newNameSpace, ref nameSpaceText);
            }

            if(element is IBuildingBlockInstantiation)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                IBuildingBlockInstantiation buildingBlockInstantiation = (IBuildingBlockInstantiation)element;
                BuildingBlock? buildingBlock = buildingBlockInstantiation.GetInstancedBuildingBlock();
                if (buildingBlock == null) return null;
                if (word.Text == ".")
                {
                    word.MoveNext();
                    if (nameSpaceText == "")
                    {
                        nameSpaceText = buildingBlockInstantiation.Name;
                    }
                    else
                    {
                        nameSpaceText = nameSpaceText + "." + buildingBlockInstantiation.Name;
                    }
                    return searchNameSpace(word, buildingBlock, ref nameSpaceText);
                }
                return null;
            }
            return nameSpace;
        }
        public static NameSpace? searchUnfoundNameSpace(WordScanner word, ref string nameSpaceText)
        {
            if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
            {
                return null;
            }

            if (word.NextText != ".")
            {
                return null;
            }

            do
            {
                nameSpaceText += word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text == ".")
                {
                    nameSpaceText += ".";
                    word.MoveNext();
                    continue;
                }
                break;
            } while (!word.Eof);

            return null;
        }

        public static Primary? parseDataObject(WordScanner word, NameSpace nameSpace, INamedElement owner, bool lValue,string nameSpaceText)
        {
            DataObjectReference? dataObjectReference = DataObjectReference.ParseCreate(word, nameSpace, owner, lValue);
            if(dataObjectReference != null) dataObjectReference.NameSpaceText = nameSpaceText;

            if (dataObjectReference == null) return null;
            if (dataObjectReference.DataObject == null) return null;

            DataObjects.Variables.Object? obj = null;
            if (dataObjectReference.DataObject is DataObjects.Variables.Object)
            {
                obj = (DataObjects.Variables.Object)dataObjectReference.DataObject;
            }

            if (word.Text != ".") return dataObjectReference;
            word.MoveNext();

            if (!dataObjectReference.DataObject.NamedElements.ContainsKey(word.Text))
            {
                if(word.NextText=="(" || word.NextText == ";")
                {
                    return parseUndefinedFunction(word);
                }
                return dataObjectReference;
            }

            //if (!variable.DataObject.NamedElements.ContainsKey(word.Text))
            //{ // undefined primitive
            //    return parseUndefinedDataObject(word, nameSpace, owner, lValue, nameSpaceText);
            //}
            INamedElement? element = dataObjectReference.DataObject.NamedElements[word.Text];


            // Since ModPort are also namespaces, they need to be processed before namespaces.
            if (element is DataObject)
            {
                if (nameSpaceText != "") nameSpaceText = nameSpaceText + ".";
                nameSpaceText = nameSpaceText + dataObjectReference.VariableName + ".";
                return parseDataObject(word, nameSpace, dataObjectReference.DataObject, lValue,nameSpaceText);
            }

            // Since Task and Function are also namespaces, they need to be processed before namespaces.

            if (element is BuiltInMethod)
            {
                BuiltinMethodCall? builtinMethodCall = BuiltinMethodCall.ParseCreate(word, nameSpace,dataObjectReference.DataObject);
                return builtinMethodCall;
            }

            // task reference : for left side only
            if (lValue && element is Task && obj != null)
            {
                TaskReference task =TaskReference.ParseCreate(word, nameSpace.BuildingBlock,obj.Class);
                return task;
            }

            // void function call : for left side only
            if (lValue && element is Function && obj != null)
            {
                FunctionCall? func = FunctionCall.ParseCreate(word, nameSpace, obj.Class);
                Function? function = func?.Function;
                if(function!= null && function.ReturnVariable == null) return func;
            }

            // function call : for right side only
            if (!lValue && element is Function && obj != null)
            {
                FunctionCall? func = FunctionCall.ParseCreate(word, nameSpace, obj.Class);
                return func;
            }

            word.AddError("illegal primitive");
            return null;
        }

        //private static Primary? parseUndefinedDataObject(WordScanner word)
        //{
        //    word.Color(CodeDrawStyle.ColorType.Identifier);
        //    word.MoveNext();

        //    if(word.Text == "(")
        //    {
        //        word.MoveNext();
        //        word.SkipToKeywords(new List<string> { ";",")" });
        //        if (word.Text == ")") word.MoveNext();
        //    }
        //    return null;
        //}
        private static Primary? parseUndefinedFunction(WordScanner word)
        {
            word.AddError("undefined function");
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                word.SkipToKeywords(new List<string> { ";", ")" });
                if (word.Text == ")") word.MoveNext();
            }
            return null;
        }


        private static Primary? subParseCreate(WordScanner word, NameSpace nameSpace,bool lValue, bool acceptImplicitNet)
        {
            if (nameSpace == null) throw new Exception();

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
                        var variable = DataObjectReference.ParseCreate(word, nameSpace, nameSpace, lValue);
                        if (variable != null) return variable;

                        var parameter = ParameterReference.ParseCreate(word, nameSpace);
                        if (parameter != null) return parameter;

                        if (!lValue && word.NextText == "(")
                        {
                            return FunctionCall.ParseCreate(word, nameSpace);
                        }

                        if (word.NextText == ".")
                        {
                            if(
                                nameSpace.BuildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(word.Text)
                            ){ // module instance

                                IBuildingBlockWithModuleInstance? buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;
                                if (buildingBlock == null) throw new Exception();

                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)nameSpace.BuildingBlock.NamedElements[word.Text];

                                string moduleName = instantiation.SourceName;
                                if (word.RootParsedDocument.ProjectProperty == null) return null;
                                BuildingBlock? module = word.RootParsedDocument.ProjectProperty.GetBuildingBlock(moduleName);
                                if (module == null) return null;
                                word.MoveNext();
                                word.MoveNext(); // .

                                Primary? primary = subParseCreate(word, module, lValue, acceptImplicitNet);
                                if (primary == null)
                                {
                                    word.AddError("illegal variable");
                                }
                                return primary;
                            } else if (nameSpace.NamedElements.ContainsKey(word.Text) && nameSpace.NamedElements[word.Text] is NameSpace)
                            { // namespaces
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                NameSpace space = (NameSpace)nameSpace.NamedElements[word.Text];
                                if (space == null) return null;
                                word.MoveNext();
                                word.MoveNext(); // .

                                Primary? primary = subParseCreate(word, space, lValue, acceptImplicitNet);
                                if (primary == null)
                                {
                                    word.AddError("illegal variable");
                                }
                                return primary;
                            }
                        }
                        else
                        {
                            if (
                                nameSpace.BuildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(word.Text)
                            ){ // module instance
                                IBuildingBlockWithModuleInstance? buildingBlock = nameSpace.BuildingBlock as IBuildingBlockWithModuleInstance;
                                if (buildingBlock == null) throw new Exception();

                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)nameSpace.BuildingBlock.NamedElements[word.Text];
                                string moduleName = instantiation.SourceName;

                                if(word.RootParsedDocument.ProjectProperty == null) return null;
                                BuildingBlock? module = word.RootParsedDocument.ProjectProperty.GetBuildingBlock(moduleName);
                                if (module == null) return null;
                                word.MoveNext();

                                if(word.Text == ".")
                                {
                                    word.MoveNext();
                                    Primary? primary = subParseCreate(word, module, lValue, acceptImplicitNet);
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

                            }else if(nameSpace is BuildingBlock && nameSpace.BuildingBlock.NamedElements.ContainsKey(word.Text) && nameSpace.BuildingBlock.NamedElements[word.Text] is Task)
                            {
                                return TaskReference.ParseCreate(word, nameSpace.BuildingBlock, acceptImplicitNet);
                            }
                            else if (nameSpace.NamedElements.ContainsKey(word.Text) && nameSpace.NamedElements[word.Text] is NameSpace)
                            {
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                NameSpace space = (NameSpace) nameSpace.NamedElements[word.Text];
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



