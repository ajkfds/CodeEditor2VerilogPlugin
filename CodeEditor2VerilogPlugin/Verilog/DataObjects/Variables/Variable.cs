using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    // #SystemVerilog 2017
    //	variable	+ integer_vector_type	+ bit 		user-defined-size	2state	sv
    //										+ logic		user-defined-size	4state  sv
    //										+ reg		user-defined-size	4state	v
    //
    //				+ integer_atom_type		+ byte		8bit signed			2state  sv
    //										+ shortint	16bit signed		2state  sv
    //										+ int		32bit signed		2state  sv
    //										+ longint	64bit signed		2state  sv
    //										+ integer	32bit signed		4state	v
    //										+ time		64bit unsigned		        v
    //
    //            	+ non_integer_type		+ shortreal	                            sv
    //										+ real		                            v
    //										+ realtime	                            v
    //              + struct/union
    //              + enum
    //              + string
    //              + chandle
    //              + virtual(interface)
    //              + class/package
    //              + event
    //              + pos_covergroup
    //              + type_reference


    public class Variable : DataObject, ICommentAnnotated
    {
        protected Variable() { }

        /// <summary>
        /// create variable instance from DataType
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static new Variable Create(string name, IDataType dataType)
        {
            /*
            6.8 Variable declarations 
             
             */

            /* TODO
                | struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                | "enum" [enum_base_type] { enum_name_declaration { , enum_name_declaration } { packed_dimension }
                | "chandle"
                | "virtual" ["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier] 
                | [class_scope | package_scope] type_identifier { packed_dimension }
                | class_type
                | "event"
                | ps_covergroup_identifier 
                | type_reference
            */
            switch (dataType.Type)
            {
                //struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                case DataTypeEnum.Struct:
                case DataTypeEnum.Union:
                    return Struct.Create(name, dataType);
                //integer_vector_type ::= "bit" | "logic" | "reg"
                case DataTypeEnum.Logic:
                case DataTypeEnum.Bit:
                case DataTypeEnum.Reg:
                    return IntegerVectorValueVariable.Create(name, dataType);
                //integer_atom_type   ::= "byte" | "shortint" | "int" | "longint" | "integer" | "time"
                case DataTypeEnum.Byte:
                case DataTypeEnum.Shortint:
                case DataTypeEnum.Int:
                case DataTypeEnum.Longint:
                case DataTypeEnum.Integer:
                case DataTypeEnum.Time:
                    return IntegerAtomVariable.Create(name, dataType);

                //non_integer_type    ::= "shortreal" | "real" | "realtime"
                case DataTypeEnum.Shortreal:
                    return Shortreal.Create(name, dataType);
                case DataTypeEnum.Real:
                    return Real.Create(name, dataType);
                case DataTypeEnum.Realtime:
                    return Realtime.Create(name, dataType);

                // "string"
                case DataTypeEnum.String:
                    return String.Create(name, dataType);
                case DataTypeEnum.Enum:
                    return Enum.Create(name, dataType);
                case DataTypeEnum.Class:
                    return Object.Create(name, dataType);

                case DataTypeEnum.Chandle:
                    return Chandle.Create(name, dataType);

                // "mailbox" (IEEE 1800-2017 section 15)
                case DataTypeEnum.Mailbox:
                    return Mailbox.Create(name, dataType);

                // "semaphore" (IEEE 1800-2017 section 20.4)
                case DataTypeEnum.Semaphore:
                    return Semaphore.Create(name, dataType);

               // "process" built-in class (IEEE 1800-2017 section 9.7)
               case DataTypeEnum.Process:
                   return Process.Create(name, dataType);

                // "event" - event variables are simple data objects
                case DataTypeEnum.Event:
                    return DataObjects.Variables.Event.Create(name, dataType);

                case DataTypeEnum.UserDefined:
                    return UserDefinedVariable.Create(name, dataType);
                case DataTypeEnum.TypeReference:
                    return CreateFromTypeReference(name, dataType);

                case DataTypeEnum.Interface:
                    return VirtualInterface.Create(name, dataType);

                // interface class variable ("ihello iobj;") — a handle whose
                // member resolution follows the interface class via the
                // lazy NamedElements getter (same pattern as UserDefinedVariable).
                case DataTypeEnum.InterfaceClass:
                    return InterfaceClassVariable.Create(name, dataType);
                default:
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                    return null;
//                    throw new Exception();
            }
        }

        /// <summary>
        /// Create variable from type_reference
        /// IEEE 1800-2017
        /// 
        /// type_reference ::= type ( expression )
        ///                   | type ( expression {, expression } )
        /// 
        /// For variable declarations:
        /// [class_scope | package_scope] type_identifier { packed_dimension }
        /// </summary>
        private static Variable CreateFromTypeReference(string name, IDataType dataType)
        {
            if (dataType is DataTypes.TypeReference typeRef)
            {
                // Get the actual referenced type
                IDataType? referencedType = typeRef.GetReferencedType();
                if (referencedType != null)
                {
                    return Create(name, referencedType);
                }

                // Unresolved type_reference (e.g. "var type(a+b) c;" — the
                // expression's type cannot be determined statically here).
                // The reference is still a legal variable declaration, so
                // register a placeholder variable that carries the
                // TypeReference rather than failing the declaration.
                return new Variable() { Name = name, DataType = dataType };
            }
            return UserDefinedVariable.Create(name, dataType);
        }



        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(" ");
            label.AppendText(Name, Global.CodeDrawStyle.Color(ColorType));
            SyncContext.AppendLabel(label);
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {

        }

        public override Variable Clone()
        {
            Variable val = new Variable() { Name = Name };
            return val;
        }

        public override Variable Clone(string name)
        {
            Variable val = new Variable() { Name = name };
            return val;
        }

        private static readonly HashSet<string> dataTypeStartKeywords = new HashSet<string>()
        {
            "bit", "logic", "reg",
            "byte", "shortint", "int", "longint", "integer", "time",
            "shortreal", "real", "realtime",
            "struct", "union", "enum", "string", "chandle", "event", "var",
            "signed", "unsigned", "["
        };

        /// <summary>
        /// Returns true when the token after the current "," starts a new
        /// data_type declaration (for_initialization allows
        /// "for (int a=0, bit b=1; ...)" where each for_variable_declaration
        /// has its own data type). Probes with a clone to avoid consuming
        /// tokens or updating the building block tree.
        /// </summary>
        private static bool NextStartsWithDataType(WordScanner word, NameSpace nameSpace)
        {
            if (dataTypeStartKeywords.Contains(word.NextText)) return true;
            if (!General.IsIdentifier(word.NextText)) return false;

            WordScanner probe = word.Clone(false);
            probe.MoveNext(); // move to the token after ","
            IndexReference beforeRef = probe.CreateIndexReference();
            IDataType? dataType = DataTypeFactory.ParseCreate(probe, nameSpace, null);
            probe.Dispose();
            if (dataType == null) return false;
            return !beforeRef.IsSameAs(probe.CreateIndexReference());
        }

        public static bool ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
            return ParseDeclaration(word, nameSpace, null);
        }
        public static bool ParseDeclaration(WordScanner word, NameSpace nameSpace,Action<DataObject>? declared)
        {
            // data_declaration::=    [ "const" ] ["var"] [lifetime] data_type_or_implicit list_of_variable_decl_assignments;
            //                      | type_declaration
            //                      | package_import_declaration11
            //                      | net_type_declaration
            // lifetime ::= static | automatic 

            // list_of_variable_decl_assignments ::= variable_decl_assignment { , variable_decl_assignment } 

            // variable_decl_assignment     ::=   variable_identifier                                   { variable_dimension }  [ = expression]
            //                                  | dynamic_array_variable_identifier unsized_dimension   { variable_dimension }  [ = dynamic_array_new]
            //                                  | class_variable_identifier                                                     [ = class_new]

            // variable_dimension::=
            //        unsized_dimension
            //      | unpacked_dimension
            //      | associative_dimension
            //      | queue_dimension

            // unsized_dimension::= "[" "]"

            // associative_dimension ::=
            //        "[" data_type "]"
            //      | "[" "*" "]"

            // unpacked_dimension ::=
            //        "[" constant_range "]"
            //      | "[" constant_expression "]"

            // queue_dimension::= "[" "$" [ ":" constant_expression] "]"

            bool pointerMoved = false;

            if (word.Text == "const")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            if (word.Text == "var")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            if (word.Text == "static" | word.Text == "automatic")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            bool virtual_ = false;
            if (word.Text == "virtual")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                virtual_ = true;
            }

            if (!pointerMoved)
            {   // shadowing check
                // buidingblock内にclassと同名のclass 以外のidentifierが存在した場合はクラスの定義とはみなさない。
                // int a;
                // a = 1;
                // のような場合にclass aが存在した場合に、a=1をclass aのinstance生成と誤認しないために必要。
                INamedElement? namedElement = nameSpace.GetNamedElementUpward(word.Text);
                if (namedElement != null && namedElement is pluginVerilog.Verilog.DataObjects.Variables.Variable) return false;
            }


            IDataType? dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null,virtual_);
            if (dataType == null)
            {
                if (virtual_)
                {
                    word.AddError("virtual interface type required");
                    if(!word.RootParsedDocument.ReferencedUnitNameSpace.Contains(word.Text)) word.RootParsedDocument.ReferencedUnitNameSpace.Add(word.Text);
                }
                return pointerMoved;
            }

            if(dataType is BuildingBlocks.Class)
            {
                BuildingBlocks.Class class_ = (BuildingBlocks.Class)dataType;
                if (!word.RootParsedDocument.ReferencedUnitNameSpace.Contains(class_.Name)) word.RootParsedDocument.ReferencedUnitNameSpace.Add(class_.Name);
            }

            if(dataType is InterfaceClass interfaceClassType)
            {
                // "ihello iobj;" / "itest it_ref;" — a variable whose type is an
                // interface class. This is a variable declaration, not an
                // interface/module instantiation, so it must not fall through to
                // InterfaceInstance.Parse (which reports "unfound module").
                // Register the type reference for the simulation setup and
                // continue to the generic variable declaration loop below.
                if (!word.RootParsedDocument.ReferencedUnitNameSpace.Contains(interfaceClassType.Name)) word.RootParsedDocument.ReferencedUnitNameSpace.Add(interfaceClassType.Name);
            }

            List<DataObject> vars = new List<DataObject>();

            while (!word.Eof && word.Text != ";")
            {
                string name = word.Text;
                if (!General.IsIdentifier(name))
                {
                    word.AddError("illegal identifier");
                    break;
                }

                DataObject variable = Variable.Create(word.Text, dataType);
                variable.DefinedNameSpace = nameSpace;
                if (variable == null) return true;
                variable.DefinedReference = word.GetReference();

                word.Color(variable.ColorType);
                word.MoveNext();

                // { variable_dimension }
                List<UnPackedArray>? unpackedArrays = new List<UnPackedArray>();
                while (word.Text == "[" && !word.Eof)
                {
                    IArray? array = DataObjects.Arrays.VariableArray.ParseCreate(variable, word, nameSpace);
                    if (array is UnPackedArray)
                    {
                        UnPackedArray unPackedArray = (UnPackedArray)array;
                        variable.UnpackedArrays.Add(unPackedArray);

                        if (unpackedArrays != null) unpackedArrays.Add(unPackedArray);
                    }
                    else if (array is Queue)
                    {
                        Queue queue = (Queue)array;
                        variable = queue;
                        unpackedArrays = null;
                    }
                    else if (array is DynamicArray)
                    {
                        DynamicArray dynamicArray = (DynamicArray)array;
                        variable = dynamicArray;
                        unpackedArrays = null;
                    }
                    else if (array is AssociativeArray)
                    {
                        AssociativeArray associativeArray = (AssociativeArray)array;
                        variable = associativeArray;
                        unpackedArrays = null;
                    }
                }


                // create assigned map if variable_dimension is generated only from unpackedarrays
                if (unpackedArrays != null)
                {
                    // a fixed-size unpacked array exposes the section 7.12 array
                    // manipulation methods (sum / product / find / ...) exactly
                    // like queue / dynamic / associative arrays (IEEE 1800-2017 7.12)
                    if (unpackedArrays.Count != 0)
                    {
                        Arrays.ArrayMethods.AppendArrayLocatorMethods(variable.NamedElements, variable);
                        Arrays.ArrayMethods.AddSizeOrNum(variable.NamedElements, "size");
                    }

                    if (dataType is IntegerAtomType)
                    {
                        if (dataType.BitWidth == null) throw new Exception();
                        int bitWidth = (int)dataType.BitWidth;
                        variable.AssignedMap = new ArraysBoolMap(bitWidth, dataType.PackedDimensions, unpackedArrays);
                    }
                    else if (dataType is IntegerVectorType)
                    {
                        variable.AssignedMap = new ArraysBoolMap(dataType.PackedDimensions, unpackedArrays);
                    }
                    else if (dataType is DataTypes.StructType structType && structType.Packed)
                    {
                        if (dataType.BitWidth == null) throw new Exception();
                        int bitWidth = (int)dataType.BitWidth;
                        variable.AssignedMap = new ArraysBoolMap(bitWidth, structType.PackedDimensions, unpackedArrays);
                    }
                }

                bool assigned = false;
                if (word.Text == "=")
                {
                    word.MoveNext();    // =
                    if (variable.AssignedMap != null)
                    {
                        variable.AssignedMap.AssertAll();
                    }

                    if (word.Text == "new")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        if (word.Text == "[") // dynamic array size
                        {
                            word.MoveNext();

                            Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);

                            if (word.Text != "]")
                            {
                                word.AddError("] required");
                            }
                            else
                            {
                                word.MoveNext();
                            }
                        }

                        if (word.Text == "(")
                        {
                            word.MoveNext();
                            while (!word.Eof && word.Text != ")")
                            {
                                Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);
                                if (word.Text == ",")
                                {
                                    word.MoveNext();
                                    continue;
                                }
                                else if (word.Text == ")")
                                {
                                    break;
                                }
                                else
                                {
                                    word.AddError("illegal arguments");
                                    break;
                                }
                            }
                            if (word.Text == ")") word.MoveNext();
                        }
                        assigned = true;
                    }
                    else
                    {
                        Expressions.Expression? exp;
                        if (word.Text == "'" && word.NextText == "{")
                        {
                            exp = AssignmentPattern.ParseCreate(word, nameSpace, false);
                            if (exp == null)
                            {
                                word.AddError("illegal assignment pattern.");
                                return true;
                            }
                            else
                            {
                                assigned = true;
                            }
                        }
                        else
                        {
                            exp = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (exp == null)
                            {
                                word.AddError("expression required.");
                                word.SkipToKeyword(";");
                                if (word.Text == ";") word.MoveNext();
                                return true;
                            }
                            else
                            {
                                assigned = true;
                                variable.AssignedReferences.Add(variable.DefinedReference);
                                if (variable.BitWidth != exp.BitWidth && !word.Prototype && exp.BitWidth != null && exp.Reference != null && !word.Prototype)
                                {
                                    exp.Reference.AddWarning("Bitwidth mismatch " + variable.BitWidth.ToString() + " = " + exp.BitWidth.ToString());
                                }
                            }
                        }
                    }
                }

                if (word.Prototype)
                {
                    if (word.CompletionContext != null)
                    {
                        // do not update building block tree @ code completion partial parse
                    }
                    else if (!nameSpace.NamedElements.ContainsKey(variable.Name))
                    {   // new variable
                        nameSpace.NamedElements.Add(variable.Name, variable);
                        // class members are parsed in prototype mode; the member
                        // must be marked Defined here, otherwise references through
                        // class objects report "not defined here"
                        variable.Defined = true;
                    }
                    else
                    {   // duplicated name
                        INamedElement oldNamedElect = nameSpace.NamedElements[variable.Name];
                        DataObject? oldDataObject = oldNamedElect as DataObject;

                        if (oldDataObject != null && oldDataObject.DefinedNameSpace?.BuildingBlock != nameSpace.BuildingBlock)
                        { // override base class

                        }
                        else if (nameSpace.BuildingBlock.AnsiStylePortDefinition)
                        {
                            variable.DefinedReference?.AddError("duplicate");
                        }
                        else
                        {
                            BuildingBlocks.IModuleOrInterfaceOrProgram? buildingBlockWithPorts = nameSpace.BuildingBlock as BuildingBlocks.IModuleOrInterfaceOrProgram;
                            if (buildingBlockWithPorts == null)
                            {
                                variable.DefinedReference?.AddError("duplicate");
                            }
                            else
                            {
                                if (buildingBlockWithPorts.Ports.ContainsKey(variable.Name))
                                {
                                    Port port = buildingBlockWithPorts.Ports[variable.Name];
                                    port.DataObject = variable;
                                }
                                else
                                {
                                    variable.DefinedReference?.AddError("duplicate");
                                }
                            }

                        }
                        //                        word.AddError("duplicate");
                    }
                }
               else
                {
                    if (word.CompletionContext != null)
                    {
                        // do not update building block tree @ code completion partial parse
                    }
                    else if (!nameSpace.NamedElements.ContainsKey(variable.Name))
                    {
                        nameSpace.NamedElements.Add(variable.Name, variable);
                        variable.Defined = true;
                    }
                    else
                    {
                        DataObject? preDefined = nameSpace.NamedElements[variable.Name] as DataObject;
                        if (preDefined != null)
                        {
                            preDefined.Defined = true;
                            variable = preDefined;
                        }
                    }
                }
                variable.AssignedMap = new ArraysBoolMap(dataType, unpackedArrays);
                if (assigned) variable.AssignedMap.AssertAll();
                if (declared != null) declared(variable);

                vars.Add(variable);

                if (word.Text == ",")
                {
                    // for_initialization allows a fresh for_variable_declaration
                    // after a comma, each with its own data type:
                    //   for (int a=0, bit b=1; ...)
                    // When the token after the comma starts a new data_type,
                    // consume the comma, re-parse the data type, and continue the
                    // declaration list with the new type.
                    if (nameSpace is Statements.ForStatememt && NextStartsWithDataType(word, nameSpace))
                    {
                        word.MoveNext(); // ,
                        IDataType? forDataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
                        if (forDataType == null)
                        {
                            word.AddError("illegal for_initialization");
                            return true;
                        }
                        dataType = forDataType;
                        continue;
                    }
                    word.MoveNext();
                    CommentAnnotation.DataObjectAnnotaion.ParsePostComment(word, nameSpace, variable);
                }
                else
                {
                    if (word.Text == ";")
                    {
                        word.MoveNext();
                        CommentAnnotation.DataObjectAnnotaion.ParsePostComment(word, nameSpace, variable);
                    }
                    else
                    {
                        word.AddError("; required");
                    }
                    break;
                }
            }

            string comment = word.GetNextComment();

            if (comment != "")
            {
                foreach (DataObject variable in vars)
                {
                    variable.Comment = comment;
                }
            }

            return true;
        }


        // comment annotation


        private Dictionary<string, string> commentAnnotations = new Dictionary<string, string>();
        public Dictionary<string, string> CommentAnnotations { get { return commentAnnotations; } }
        public void AppendAnnotation(string key, string value)
        {
            if (commentAnnotations.ContainsKey(key))
            {
                string oldValue = commentAnnotations[key];
                string newValue = oldValue + "," + value;
                commentAnnotations[key] = newValue;
            }
            else
            {
                commentAnnotations.Add(key, value);
            }
        }

    }
}
