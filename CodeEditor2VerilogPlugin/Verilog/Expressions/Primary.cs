using CSharpMath.Atom.Atoms;
using Microsoft.Agents.AI;
using OpenAI.Realtime;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    public abstract class Primary : Expression
    {
        protected Primary()
        {
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

        public new virtual AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
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

        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, false);
        }
        public static new Primary? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            return parseCreate(word, nameSpace, false, acceptImplicitNet,true);
        }
        public static Primary? ParseCreateLValue(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            return parseCreate(word, nameSpace, true, acceptImplicitNet,true);
        }
        public static Primary? ParseCreateWoRange(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet)
        {
            return parseCreate(word, nameSpace, false, acceptImplicitNet, false);
        }
        private static Primary? parseCreate(WordScanner word, NameSpace nameSpace, bool lValue, bool acceptImplicitNet, bool acceptRange = true)
        {
            //if (word.Text == "srif") System.Diagnostics.Debugger.Break();
            // acceptRange = false is used for foreach(data[i])

            switch (word.WordType)
            {
                case WordPointer.WordTypeEnum.Number:
                    return Number.ParseCreateNumberOrCast(word, nameSpace, lValue);
                case WordPointer.WordTypeEnum.Symbol:
                    if (word.GetCharAt(0) == '{')
                    {
                        return Concatenation.ParseCreateConcatenationOrMultipleConcatenation(word, nameSpace, lValue, acceptImplicitNet);
                    }
                    else if (word.GetCharAt(0) == '(')
                    {
                        return Bracket.ParseCreateBracketOrMinTypMax(word, nameSpace);
                    }
                    else if (word.GetCharAt(0) == '\'')
                    {
                        // assignment pattern '{
                        if (word.NextText == "{")
                        {
                            return AssignmentPattern.ParseCreate(word, nameSpace, lValue);
                        }
                    }
                    return null;
                case WordPointer.WordTypeEnum.String:
                    return ConstantString.ParseCreate(word, nameSpace);
                case WordPointer.WordTypeEnum.Text:
                    // null
                    if (word.Text == "null")
                    {
                        return Null.ParseCreate(word, nameSpace);
                    }
                    // dollar primitive
                    if (word.Text == "$")
                    {
                        return DollarMark.ParseCreate(word, nameSpace);
                    }

                    // system function call
                    if (word.Text.StartsWith("$"))// && word.ProjectProperty.SystemFunctions.Keys.Contains(word.Text))
                    {
                        return FunctionCall.ParseCreate(word, nameSpace);
                    }

                    // assignment pattern
                    if (word.Text == "'" && word.NextText == "{")
                    {
                        return AssignmentPattern.ParseCreate(word, nameSpace, lValue);
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
                    if (word.NextText == "(" && word.Text == nameSpace.Name)
                    {
                        // It shall be illegal to omit the parentheses in a tf_call unless the subroutine is a task, void function,
                        // or class method. If the subroutine is a nonvoid class function method, it shall be illegal to omit the parentheses if the call is directly recursive.
                        return FunctionCall.ParseCreate(word, nameSpace);
                    }




                    NameReference? nameReference = NameReference.ParseCreate(word, nameSpace,acceptRange);
                    if (nameReference == null)
                    {
                        return null;
                    }

                    INamedElement? element;
                    INamedElement? targetElement;
                    NameSpace? targetNameSpace;
                    (element, targetElement) = nameReference.GetElement(nameSpace);
                    targetNameSpace = targetElement as NameSpace;

                    // implicit net declaration
                    if (acceptImplicitNet && element == null && nameReference.CanBeImplecitNet )
                    {
                        Net net = DataObjects.Nets.Net.Create(word.Text, DataObjects.Nets.Net.NetTypeEnum.Wire, null);
                        net.DefinedReference = word.GetReference();
                        net.Defined = true;

                        if (word.Prototype)
                        {
                            nameSpace.NamedElements.Add(net.Name, net);
                        }
                        else
                        {
                            if (nameSpace.NamedElements.ContainsKey(net.Name))
                            {
                                nameSpace.NamedElements.RemoveKey(net.Name);
                                nameSpace.NamedElements.Add(net.Name, net);
                            }
                        }

                        // define @ protptype
                        if (word.Prototype)
                        {
                            word.ApplyPrototypeRule(word.ProjectProperty.RuleSet.ImplicitNetDeclaretion);
                        }

                        return parseDataObject(word, nameSpace, nameSpace, lValue, acceptRange, "");
                    }

                    {
                        if (element is VirtualScopeNameSpace)
                        {
                            VirtualScopeNameSpace virtualScopeNameSpace = (VirtualScopeNameSpace)element;
                            element =  virtualScopeNameSpace.VirtualScopeTarget;
                        }
                    }

                    if (element == null)
                    {
                        WordReference beginRef = word.GetReference();
                        word.AddError("unfound object");
                        word.MoveNext();
                        return new UnfoundObjectReference() { Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference()) };
                    }

                    string nameSpaceText = nameReference.GetNameSpaceText();

                    // variable reference
                    if (element is DataObject && targetElement != null)
                    {
                        return parseDataObject(word, nameSpace, targetElement, lValue, acceptRange, nameSpaceText);
                    }

                    // Since Task and Function are also namespaces, they need to be processed before namespaces.
                    // task reference : for left side only
                    if (lValue && element is Task_ && targetNameSpace != null)
                    {
                        return TaskReference.ParseCreate(word, nameSpace, targetNameSpace);
                    }

                    // task reference on a class object : e.g. obj.myTask(...)
                    // (Variables.Object is not a NameSpace, so the branch above cannot handle it)
                    if (element is Task_ && targetElement is DataObjects.Variables.Object taskObjectVariable)
                    {
                        BuildingBlocks.Class? taskSourceClass = taskObjectVariable.GetSourceClass();
                        if (taskSourceClass != null)
                        {
                            return TaskReference.ParseCreate(word, nameSpace, taskSourceClass);
                        }
                    }

                    // function call : for right side only
                    if (!lValue && (element is Function || element is LetDeclaration) && targetNameSpace != null)
                    {
                        Primary? functionCall = FunctionCall.ParseCreate(word, nameSpace,targetNameSpace);
                        if (functionCall == null) return null;
                        return parseChainedMethodCalls(word, nameSpace, functionCall);
                    }

                    // function call on a class object : e.g. obj.myFunc(...)
                    // (Variables.Object is not a NameSpace, so the branch above cannot handle it)
                    // Note: statement-level calls (e.g. obj.myTask(); obj.myFunc(); ) reach here with
                    // lValue == true via ParseCreateVariableLValue, so the lValue restriction must not
                    // apply to class-object function calls. A function call can never be an lValue,
                    // so accepting it here is safe.
                    if (element is Function && targetElement is DataObjects.Variables.Object objectVariable)
                    {
                        BuildingBlocks.Class? sourceClass = objectVariable.GetSourceClass();
                        if (sourceClass != null)
                        {
                            Primary? functionCall = FunctionCall.ParseCreate(word, nameSpace, sourceClass);
                            if (functionCall == null) return null;
                            return parseChainedMethodCalls(word, nameSpace, functionCall);
                        }
                    }

                    if ( (element is Verilog.BuiltInMethod) && targetElement is DataObjects.Variables.Object)
                    {
                        // built-in method call (e.g. handle.randomize(), obj.srandom(seed))
                        BuiltinMethodCall? methodCall = BuiltinMethodCall.ParseCreate(word, nameSpace, (DataObjects.DataObject)targetElement);
                        return methodCall;
                    }

                    if (element is DataObjects.Constants.Constants && targetNameSpace != null)
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


                    IDataType? dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
                    if (dataType != null)
                    {
                        DataTypeReference dataTypeReference = new DataTypeReference { IDataType = dataType };
                        return dataTypeReference;
                    }

                    {
                        if (element is ModuleInstantiation)
                        {
                            WordReference beginRef = word.GetReference();
                            ModuleInstantiation? moduleInstantiation = (ModuleInstantiation)element;
                            BuildingBlock? buildingBlock = moduleInstantiation.GetInstancedBuildingBlock();
                            if(buildingBlock == null)
                            {
                                word.AddError("unfound object");
                                word.MoveNext();
                                return new UnfoundObjectReference() { Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference()) };
                            }
                            NameSpaceReference nameSpaceReference = new NameSpaceReference(buildingBlock) { 
                                Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference()) 
                            };
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            return nameSpaceReference;
                        }
                    }

                    {
                        if (element is NameSpace)
                        {
                            NameSpace space = (NameSpace)element;
                            WordReference beginRef = word.GetReference();
                            NameSpaceReference nameSpaceReference = new NameSpaceReference(space)
                            {
                                Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference())
                            };
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            return nameSpaceReference;
                        }
                    }
                    {
                        WordReference beginRef = word.GetReference();
                        word.AddError("unfound object");
                        word.MoveNext();
                        return new UnfoundObjectReference() { Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference()) };
                    }
            }
            return null;
        }

        /// <summary>
        /// Parse chained method calls on a function call's return value.
        /// e.g. obj.getObj().method(), obj.getObj().method().method2()
        /// (method_call ::= method_call_root . method_call_body, method_call_root ::= primary)
        /// FunctionCall must be the last Primary in the chain.
        /// </summary>
        private static Primary? parseChainedMethodCalls(WordScanner word, NameSpace nameSpace, Primary primary)
        {
            while (true)
            {
                if (word.Eof) return primary;
                if (word.Text != ".") return primary;

                // the chained method name follows the dot
                if (!General.IsIdentifier(word.NextText)) return primary;

                // the return value of the previous call must be a class object
                FunctionCall? previousCall = primary as FunctionCall;
                if (previousCall == null) return primary;
                BuildingBlocks.Class? returnClass = previousCall.GetReturnClass();
                if (returnClass == null) return primary;

                // consume the dot and resolve the next method within the return class
                word.MoveNext();
                if (!returnClass.NamedElements.ContainsKey(word.Text)) return primary;
                INamedElement nextElement = returnClass.NamedElements[word.Text];

                if (nextElement is Function nextFunction)
                {
                    Primary? nextCall = FunctionCall.ParseCreate(word, nameSpace, returnClass);
                    if (nextCall == null) return null;
                    primary = nextCall;
                    continue;
                }
                else if (nextElement is Task_)
                {
                    // task call in a chain (as a statement); arguments are parsed by TaskReference
                    TaskReference taskReference = TaskReference.ParseCreate(word, nameSpace, returnClass);
                    return taskReference;
                }
                else
                {
                    return primary;
                }
            }
        }

        public static Primary? parseDataObject(WordScanner word, NameSpace nameSpace, INamedElement owner, bool lValue, bool acceptRange, string nameSpaceText)
        {
            DataObjectReference? dataObjectReference = DataObjectReference.ParseCreate(word, nameSpace, owner, lValue, acceptRange, nameSpaceText);

            if (dataObjectReference == null) return null;
            if (dataObjectReference.TargetDataObject == null) return null;

            DataObjects.Variables.Object? obj = null;
            if (dataObjectReference.TargetDataObject is DataObjects.Variables.Object)
            {
                obj = (DataObjects.Variables.Object)dataObjectReference.TargetDataObject;
                if (!word.RootParsedDocument.ReferencedUnitNameSpace.Contains(obj.Name)) word.RootParsedDocument.ReferencedUnitNameSpace.Add(obj.Name);
            }

            return dataObjectReference;

        }

        private static Primary? parseUndefinedFunction(WordScanner word)
        {
            WordReference beginRef = word.GetReference();

            word.AddError("undefined function");
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                word.SkipToKeywords(new List<string> { ";", ")" });
                if (word.Text == ")") word.MoveNext();
            }
            word.RootParsedDocument.ReparseRequested = true;
            WordReference wordReference = WordReference.CreateReferenceRange(beginRef, word.GetReference());
            return new UnfoundObjectReference() { Reference = wordReference };
        }



    }

}



