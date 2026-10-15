using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes

{
    // ## Verilog 2001
    // Variable -+-net +-> wire         4state  >=1bit      v
    //           +-data+-> reg          4state  >=1bit      v
    //                 +-> real                             v
    //                 +-> integer              32bit       v
    //                 +-> event                            v
    //                 +-> genvar                           v
    //                 +-> real                             v
    //                 +-> realtime                         v
    //                 +-> reg                              v
    //                 +-> time                             v
    //                 +-> trireg                           v
    // ## SystemVerilog2012
    // Variable -+-data+-> logic        4state  >=1bit      v
    //                 +-> bit          2state  >=1bit      v
    //                 +-> byte         2state  8bit
    //                 +-> shortint     2state  16bit
    //                 +-> int          2state  32bit
    //                 +-> longint      2state  64bit
    //                 +-> shortreal

    /*
    shortint    2-state data type, 16-bit signed integer 
    int         2-state data type, 32-bit signed integer 
    longint     2-state data type, 64-bit signed integer 
    byte        2-state data type, 8-bit signed integer or ASCII character 
    bit         2-state data type, user-defined vector size, unsigned 
    logic       4-state data type, user-defined vector size, unsigned 
    reg         4-state data type, user-defined vector size, unsigned 
    integer     4-state data type, 32-bit signed integer 
    time        4-state data type, 64-bit unsigned integer
     */

    public enum DataTypeEnum
    {
        //integer_vector_type::= bit | logic | reg
        Bit,
        Logic,
        Reg,
        //integer_atom_type::= byte | shortint | int | longint | integer | time
        Byte,
        Shortint,
        Int,
        Longint,
        Integer,
        Time,
        //non_integer_type::= "shortreal" | "real" | "realtime"
        Shortreal,
        Real,
        Realtime,
        // others
        Enum,
        String,
        Chandle,
        Virtual,
        Class,
        Interface,
        InterfaceClass,
        Event,
        CoverGroup,
        Struct,
        Union,
        TypeReference,
        UserDefined,
        Mailbox,
        Semaphore,
        Process
    }

    public static class DataTypeFactory
    {
        //        protected List<Variables.Dimension> dimensions = new List<Variables.Dimension>();
        //        public IReadOnlyList<Variables.Dimension> Dimensions { get; }
        //        public virtual int BitWidth { get; }
        //        public virtual bool State4 { get; }

        /*
        data_type::=
              integer_vector_type[signing] { packed_dimension }
            | integer_atom_type[signing]
            | non_integer_type
            | struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
            | "enum" [enum_base_type] { enum_name_declaration { , enum_name_declaration } { packed_dimension }
            | "string"
            | "chandle"
            | "virtual" ["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier] 
            | [class_scope | package_scope] type_identifier { packed_dimension }
            | class_type
            | "event"
            | ps_covergroup_identifier 
            | type_reference

        type_reference ::=   "type" "(" expression ")"
                           | "type" "(" data_type ")"

        integer_atom_type   ::= "byte" | "shortint" | "int" | "longint" | "integer" | "time"
        integer_vector_type ::= "bit" | "logic" | "reg"
        non_integer_type    ::= "shortreal" | "real" | "realtime"

        signing ::= "signed" | "unsigned"
        */

        public static IDataType? ParseCreate(WordScanner word, NameSpace nameSpace, DataTypeEnum? defaultDataType,bool virtual_ = false)
        {
            /*
            data_type::=
                  integer_vector_type[signing] { packed_dimension }
                | integer_atom_type[signing]
                | non_integer_type
                | struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                | "enum" [enum_base_type] { enum_name_declaration { , enum_name_declaration } { packed_dimension }
                | "string"
                | "chandle"
                | "virtual" ["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier] 
                | [class_scope | package_scope] type_identifier { packed_dimension }
                | class_type
                | "event"
                | ps_covergroup_identifier 
                | type_reference

            integer_atom_type   ::= "byte" | "shortint" | "int" | "longint" | "integer" | "time"
            integer_vector_type ::= "bit" | "logic" | "reg"
            non_integer_type    ::= "shortreal" | "real" | "realtime"

            signing ::= "signed" | "unsigned"


            14) When a type_reference is used in a net declaration, it shall be preceded by a net type keyword; and when it is used in a variable declaration, it shall be preceded by the var keyword.

            */

            // SystemVerilog data type does not include nets


            switch (word.Text)
            {
                //integer_vector_type [signing] { packed_dimension }
                //integer_vector_type::= bit | logic | reg
                case "bit":
                case "logic":
                case "reg":
                    return IntegerVectorType.ParseCreate(word, nameSpace);

                //integer_atom_type[signing]
                //integer_atom_type::= byte | shortint | int | longint | integer | time
                case "byte":
                case "shortint":
                case "int":
                case "longint":
                case "integer":
                case "time":
                    return IntegerAtomType.ParseCreate(word, nameSpace);

                //non_integer_type
                //non_integer_type::= "shortreal" | "real" | "realtime"
                case "shortreal":
                    return ShortRealType.ParseCreate(word, nameSpace);
                case "real":
                    return RealType.ParseCreate(word, nameSpace);
                case "realtime":
                    return RealTimeType.ParseCreate(word, nameSpace);

                //struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                case "struct":
                    return StructType.ParseCreate(word, nameSpace);
                case "union":
                    return UnionType.ParseCreate(word, nameSpace);

                // "enum" [enum_base_type] { enum_name_declaration { , enum_name_declaration } { packed_dimension }
                case "enum":
                    return Enum.ParseCreate(word, nameSpace);

                // "string"
                case "string":
                    return StringType.ParseCreate(word, nameSpace);
                case "chandle":
                    return Chandle.ParseCreate(word, nameSpace);

                // "mailbox" (IEEE 1800-2017 section 15)
                case "mailbox":
                    return MailboxType.ParseCreate(word, nameSpace);

                // "semaphore" (IEEE 1800-2017 section 20.4)
                case "semaphore":
                    return SemaphoreType.ParseCreate(word, nameSpace);

               // "process" built-in class (IEEE 1800-2017 section 9.7)
               case "process":
                   return ProcessType.ParseCreate(word, nameSpace);

                // "virtual" ["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier]
                // (e.g. a "type" parameter default: class Foo #(type IFType = virtual x_if); )
                case "virtual":
                    return parseVirtualInterfaceType(word, nameSpace);

                // "event"
                case "event":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    return EventType.Create();

                // ps_covergroup_identifier 

                // type_reference
                case "type":
                    return parseTypeReference(word, nameSpace);
            }

            // [package_scope] type_identifier  (e.g. "typedef some_package::some_type myalias;")
            if (word.NextText == "::" && General.IsIdentifier(word.Text))
            {
                IDataType? scoped = parsePackageScopeType(word, nameSpace);
                if (scoped != null) return scoped;
            }

            {
                INamedElement? namedElement = nameSpace.GetNamedElementUpward(word.Text);
                if (namedElement != null)
                {
                    // class
                    if (namedElement is BuildingBlocks.Class)
                    {
                        BuildingBlocks.Class classType = (BuildingBlocks.Class)namedElement;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        // parameter_value_assignment : class_identifier # ( ... )
                        // same pattern as module instantiation parameter override
                        if (word.Text == "#" && word.GetCharAt(0) == '#')
                        {
                            Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                            Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, classType);
                            // apply the overrides to the class definition's parameters (name-based resolution)
                            // note: same override-exchange logic as parseClassItems
                            if (!word.Prototype && parameterOverrides.Count != 0)
                            {
                                foreach (var vkp in parameterOverrides)
                                {
                                    if (classType.NamedElements.ContainsKey(vkp.Key) && classType.NamedElements[vkp.Key] is DataObjects.Constants.Constants)
                                    {
                                        DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)classType.NamedElements[vkp.Key];
                                        if (constants.DefinedReference != null)
                                        {
                                            constants.DefinedReference.AddHint("override " + vkp.Value.Value.ToString());
                                        }
                                    }
                                }
                            }
                        }

                        return classType;
                    }

                    // interface class
                    if (namedElement is BuildingBlocks.InterfaceClass)
                    {
                        BuildingBlocks.InterfaceClass interfaceClassType = (BuildingBlocks.InterfaceClass)namedElement;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        // parameter_value_assignment : interface_class_identifier # ( ... )
                        if (word.Text == "#")
                        {
                            Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                            Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, interfaceClassType);
                        }

                        return interfaceClassType;
                    }
                    // [class_scope | package_scope] type_identifier { packed_dimension }


                    // "virtual"["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier]
                    //if(namedElement is BuildingBlocks.Interface)
                    //{
                    //    IDataType dType = (BuildingBlocks.Interface)namedElement;
                    //    word.Color(CodeDrawStyle.ColorType.Keyword);
                    //    word.MoveNext();
                    //    return dType;
                    //}

                    // typedef
                    if (namedElement is Typedef)
                    {
                        Typedef defType = (Typedef)namedElement;
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();

                        // packed_dimension
                        List<PackedArray> packedArrays = new List<PackedArray>();
                        while (word.Text == "[")
                        {
                            PackedArray? range = PackedArray.ParseCreate(word, nameSpace);
                            if (word.Eof || range == null)
                            {
                                word.AddError("illegal reg declaration");
                                return null;
                            }
                            if (range != null) packedArrays.Add(range);
                        }
                        UserDefinedType userDefinedType = UserDefinedType.Create(defType, packedArrays);
                        return userDefinedType;
                    }
                }
            }

            // class_type
            // resolve class by name (this section is reached for identifiers that
            // were not resolved by the immediate name-space search above)
            Class? @class = word.ProjectProperty.UnitNameSpace.Get(word.Text) as Class;
            if (@class != null)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                // parameter_value_assignment : class_identifier # ( ... )
                if (word.Text == "#")
                {
                    Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                    Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, @class);
                }

                return @class;
            }

            BuildingBlock? buildingBlock = word.ProjectProperty.DefinitionNameSpace.Get(word.Text) as BuildingBlock;
            // interface
            if (buildingBlock is Interface && virtual_)
            {
                Interface interface_ = (Interface)buildingBlock;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                return interface_;
            }

            // interface_class_type
            if (buildingBlock is InterfaceClass)
            {
                InterfaceClass interfaceClass = (InterfaceClass)buildingBlock;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                // parameter_value_assignment : interface_class_identifier # ( ... )
                if (word.Text == "#")
                {
                    Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                    Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, interfaceClass);
                }

                return interfaceClass;
            }

            // interface class is registered to UnitNameSpace (compilation-unit scope)
            InterfaceClass? unitInterfaceClass = word.ProjectProperty.UnitNameSpace.Get(word.Text) as InterfaceClass;
            if (unitInterfaceClass != null)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                return unitInterfaceClass;
            }

            // class / interface class defined at root level or inside another
            // building block of the same file (class Parent chain does not
            // reach the enclosing module, so an identifier inside a class body
            // cannot resolve module-scope classes via GetNamedElementUpward;
            // fall back to Root.BuildingBlocks and their NamedElements.
            // Forward references are resolved by the second parse pass.)
            {
                BuildingBlock? rootElement = null;
                if (word.RootParsedDocument.Root != null)
                {
                    word.RootParsedDocument.Root.BuildingBlocks.TryGetValue(word.Text, out rootElement);
                    if (rootElement == null)
                    {
                        // search inside same-file building blocks (module-scope classes).
                        // Nested classes / interface classes are registered both in the
                        // enclosing block's NamedElements and in its BuildingBlocks, so
                        // check both (the BuildingBlocks table also covers classes
                        // declared later in the same file — forward declarations).
                        foreach (BuildingBlock bb in word.RootParsedDocument.Root.BuildingBlocks.Values)
                        {
                            if (bb.BuildingBlocks.TryGetValue(word.Text, out BuildingBlock? innerBlock)
                                && (innerBlock is Class || innerBlock is InterfaceClass))
                            {
                                rootElement = innerBlock;
                                break;
                            }
                            if (bb.NamedElements.TryGetValue(word.Text, out INamedElement? inner)
                                && inner is BuildingBlock innerBb
                                && (innerBb is Class || innerBb is InterfaceClass))
                            {
                                rootElement = innerBb;
                                break;
                            }
                        }
                    }
                }
                if (rootElement is Class rootClass)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text == "#")
                    {
                        Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                        Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, rootClass);
                    }
                    return rootClass;
                }
                if (rootElement is InterfaceClass rootIfaceClass)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text == "#")
                    {
                        Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                        Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, rootIfaceClass);
                    }
                    return rootIfaceClass;
                }
            }

            // Same-file module-scope class referenced from within a class body:
            // a class's Parent points at the file Root (for symbol registration),
            // not at the enclosing module, so module-scope sibling classes are
            // not reachable via GetNamedElementUpward. Walk the lexical scope via
            // Class.DeclaringNameSpace (and the enclosing building blocks) and
            // search each block's BuildingBlocks / NamedElements. This also
            // covers classes declared later in the same file (forward
            // references resolved by the enclosing module's prototype pass).
            {
                NameSpace? ns = nameSpace;
                while (ns != null)
                {
                    BuildingBlock? bb = ns.BuildingBlock;
                    if (bb != null)
                    {
                        if (bb.BuildingBlocks.TryGetValue(word.Text, out BuildingBlock? scoped)
                            && scoped is Class scopedClass)
                        {
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            if (word.Text == "#")
                            {
                                Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                                Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, scopedClass);
                            }
                            return scopedClass;
                        }
                        if (bb.NamedElements.TryGetValue(word.Text, out INamedElement? namedInner)
                            && namedInner is Class namedClass)
                        {
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            if (word.Text == "#")
                            {
                                Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                                Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, namedClass);
                            }
                            return namedClass;
                        }
                    }
                    // Prefer the lexical declaring scope for classes (Parent points
                    // at the file Root); fall back to Parent otherwise.
                    ns = (ns as Class)?.DeclaringNameSpace ?? ns.Parent;
                }
            }

            //if (nameSpace.NamedElements.ContainsKey(word.Text))
            //{
            //    INamedElement namedElement = nameSpace.NamedElements[word.Text];

            //}else if(word.RootParsedDocument.Root != null && word.RootParsedDocument.Root.NamedElements.ContainsKey(word.Text))
            //{
            //    INamedElement namedElement = word.RootParsedDocument.Root.NamedElements[word.Text];
            //    if (namedElement is Typedef)
            //    {
            //        IDataType dType = ((Typedef)namedElement).VariableType;
            //        if (dType != null)
            //        {
            //            word.Color(CodeDrawStyle.ColorType.Keyword);
            //            word.MoveNext();
            //            return dType;
            //        }
            //    }
            //}

            if (defaultDataType == null) return null;

            DataTypeEnum dataTypeEnum = (DataTypeEnum)defaultDataType;
            // data_type_or_implicit::= data_type | implicit_data_type

            // signing ::= "signed" | "unsigned"
            // implicit_data_type::= [signing] { packed_dimension }
            bool signed = false;
            if (word.Text == "unsigned")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (word.Text == "signed")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                signed = true;
            }

            IDataType? dType = null;
            switch (dataTypeEnum)
            {
                case DataTypeEnum.Bit:
                    dType = BitType.Create(signed, null);
                    break;
                case DataTypeEnum.Logic:
                    dType = LogicType.Create(signed, null);
                    break;
                case DataTypeEnum.Reg:
                    dType = RegType.Create(signed, null);
                    break;
                case DataTypeEnum.Byte:
                    dType = ByteType.Create(signed);
                    break;
                case DataTypeEnum.Shortint:
                    dType = ShortIntType.Create(signed);
                    break;
                case DataTypeEnum.Int:
                    dType = IntType.Create(signed);
                    break;
                case DataTypeEnum.Longint:
                    dType = LongIntType.Create(signed);
                    break;
                case DataTypeEnum.Integer:
                    dType = IntegerType.Create(signed);
                    break;
                case DataTypeEnum.Time:
                    dType = TimeType.Create(signed);
                    break;
                case DataTypeEnum.Shortreal:
                    dType = new ShortRealType();
                    break;
                case DataTypeEnum.Real:
                    dType = new ShortRealType();
                    break;
                case DataTypeEnum.Realtime:
                    dType = new RealTimeType();
                    break;
            }


            while (word.Text == "[")
            {
                PackedArray? range = PackedArray.ParseCreate(word, nameSpace);
                if (word.Eof || range == null)
                {
                    word.AddError("illegal reg declaration");
                    return null;
                }

                PackedArray? packedArray = range as Arrays.PackedArray;

                if (packedArray != null)
                {
                    dType.PackedDimensions.Add(packedArray);
                }
            }
            return dType;
        }

        /// <summary>
        /// Parse "[package_scope] type_identifier" where package_scope is "pkg::".
        /// Resolves the type inside the referenced package (same-file or cross-file)
        /// and returns a UserDefinedType / IDataType for the resolved type identifier.
        /// Returns null when the scope identifier is not a package or the member is
        /// not a type (caller falls through to other resolution paths).
        /// </summary>
        private static IDataType? parsePackageScopeType(WordScanner word, NameSpace nameSpace)
        {
            string packageName = word.Text;

            // resolve the package: same-file Root.BuildingBlocks, then PackageNameSpace
            BuildingBlocks.Package? package = null;
            if (word.RootParsedDocument.Root != null
                && word.RootParsedDocument.Root.BuildingBlocks.TryGetValue(packageName, out BuildingBlock? localPackage)
                && localPackage is BuildingBlocks.Package pkg)
            {
                package = pkg;
            }
            if (package == null)
            {
                var packageFile = word.ProjectProperty.PackageNameSpace.GetFile(packageName);
                if (packageFile != null
                    && packageFile.VerilogParsedDocument != null
                    && packageFile.VerilogParsedDocument.Root != null)
                {
                    var element = packageFile.VerilogParsedDocument.Root.NamedElements;
                    if (element.TryGetValue(packageName, out INamedElement? pkgElement)
                        && pkgElement is BuildingBlocks.Package pf)
                    {
                        package = pf;
                    }
                }
            }
            if (package == null) return null;

            // consume "pkg" "::"
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext(); // pkg
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // ::

            if (!General.IsIdentifier(word.Text)) return null;

            INamedElement? member = null;
            package.NamedElements.TryGetValue(word.Text, out member);
            if (member is Typedef memberTypedef)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                List<PackedArray> packedArrays = new List<PackedArray>();
                while (word.Text == "[")
                {
                    PackedArray? range = PackedArray.ParseCreate(word, nameSpace);
                    if (word.Eof || range == null) return null;
                    packedArrays.Add(range);
                }
                return UserDefinedType.Create(memberTypedef, packedArrays);
            }
            return null;
        }

        /// <summary>
        /// Parse "virtual" ["interface"] interface_identifier
        /// [parameter_value_assignment] [ "." modport_identifier] as a data type.
        /// Used e.g. as the default of a type parameter:
        ///   class Foo #(type IFType = virtual x_if);
        ///   class Foo #(type IFType = virtual interface x_if);
        /// Resolves to the declared interface when available; otherwise consumes
        /// the tokens and returns an unresolved placeholder (TypeReference).
        /// </summary>
        private static IDataType? parseVirtualInterfaceType(WordScanner word, NameSpace nameSpace)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // virtual

            if (word.Text == "interface")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // interface
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("interface identifier expected");
                return null;
            }

            string interfaceName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // optional parameter_value_assignment ( # ( ... ) )
            if (word.Text == "#")
            {
                Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, null);
            }

            // optional "." modport_identifier
            if (word.Text == ".")
            {
                word.MoveNext();
                if (General.IsIdentifier(word.Text))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
            }

            // resolve the interface if declared; otherwise keep an unresolved placeholder
            INamedElement? namedElement = nameSpace.GetNamedElementUpward(interfaceName);
            if (namedElement is BuildingBlocks.Interface interface_)
            {
                return interface_;
            }
            BuildingBlock? buildingBlock = word.ProjectProperty.DefinitionNameSpace.Get(interfaceName) as BuildingBlock;
            if (buildingBlock is BuildingBlocks.Interface interfaceFromDefinition)
            {
                return interfaceFromDefinition;
            }

            // unresolved entity: use an empty type reference placeholder (same
            // fallback as an omitted type parameter default in Constants.cs)
            return new TypeReference(null, null);
        }

        private static IDataType? parseTypeReference(WordScanner word, NameSpace nameSpace)
        {
            //type_reference::= type(expression) | type(data_type)
            if (word.Text != "type") System.Diagnostics.Debugger.Break();

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // type

            if (word.Text != "(")
            {
                word.AddError("( required");
                return null;
            }
            word.MoveNext(); // (

            IDataType? dtype = ParseCreate(word, nameSpace, null);
            if (dtype == null)
            {
                Expressions.Expression? ex = Expressions.Expression.ParseCreate(word, nameSpace);
                if (ex == null)
                {
                    word.AddError("expression or data_type required");
                    return null;
                }
                else
                {
                    // Create TypeReference for expression-based type reference
                    return TypeReference.CreateForExpression(ex);
                }
            }
            else
            {
                // Create TypeReference for data type
                return TypeReference.CreateForDataType(dtype);
            }
        }
    }
}
