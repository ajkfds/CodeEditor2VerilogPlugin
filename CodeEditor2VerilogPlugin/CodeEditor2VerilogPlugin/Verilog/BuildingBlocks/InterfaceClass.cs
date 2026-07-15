using AjkAvaloniaLibs.Controls;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class InterfaceClass : BuildingBlock, IModuleOrInterface, IModuleOrInterfaceOrCheckerOrClass, DataObjects.DataTypes.IDataType, IPortNameSpace
    {
        protected InterfaceClass() : base(null, null)
        {
        }

        public bool Packable
        {
            get { return false; }
        }

        public bool IsValidForNet { get { return false; } }

        public int? BitWidth { get; } = null;
        public new CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public bool IsVector { get { return false; } }
        public virtual bool PartSelectable { get { return false; } }

        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();

        // Port
        public Dictionary<string, DataObjects.Port> Ports { get; } = new Dictionary<string, DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get; } = new List<DataObjects.Port>();

        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public required override Data.IVerilogRelatedFile? File
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

        public DataTypeEnum Type
        {
            get { return DataTypeEnum.InterfaceClass; }
            set { }
        }

        /// <summary>
        /// List of interface classes that this interface class extends
        /// </summary>
        public List<InterfaceClass> ExtendedInterfaceClasses { get; } = new List<InterfaceClass>();

        public static void ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
            InterfaceClass? interfaceClass = ParseCreate(word, nameSpace);
            if (interfaceClass == null) return;

            if (word.Prototype)
            {
                if (!nameSpace.NamedElements.ContainsKey(interfaceClass.Name))
                {
                    nameSpace.NamedElements.Add(interfaceClass.Name, interfaceClass);
                }
                else
                {
                    word.AddError("duplicate");
                }
            }
            else
            {
                if (!nameSpace.NamedElements.ContainsKey(interfaceClass.Name))
                {
                    nameSpace.NamedElements.Add(interfaceClass.Name, interfaceClass);
                }
            }
        }

        public IDataType Clone()
        {
            InterfaceClass interfaceClass = new InterfaceClass()
            {
                BeginIndexReference = BeginIndexReference,
                DefinitionReference = DefinitionReference,
                File = File,
                Name = Name,
                Parent = Parent,
                Project = Project
            };
            foreach (var namedElement in NamedElements)
            {
                interfaceClass.NamedElements.Add(namedElement.Name, namedElement);
            }
            foreach (var extended in ExtendedInterfaceClasses)
            {
                interfaceClass.ExtendedInterfaceClasses.Add(extended);
            }
            return interfaceClass;
        }

        public static InterfaceClass? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, null);
        }

        public static InterfaceClass? ParseCreate(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression>? parameterOverrides
            )
        {
            /*
            interface_class_declaration ::= 
                "interface" "class" class_identifier [ parameter_port_list ]  
                [ "extends" interface_class_type { , interface_class_type } ] ;  
                { interface_class_item } "endclass" [ ":" class_identifier]  

            interface_class_type ::= ps_class_identifier [ parameter_value_assignment ]

            parameter_port_list ::=  
                # ( list_of_param_assignments { , parameter_port_declaration } )  
                | # ( parameter_port_declaration { , parameter_port_declaration } )  
                | #( )   
             */

            if (word.Text != "interface") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();

            if (word.Text != "class")
            {
                word.AddError("class keyword expected");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvPair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvPair.Key, kvPair.Value);
            }

            // class_identifier
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal interface class name");
                word.SkipToKeyword(";");
                return null;
            }

            InterfaceClass interfaceClass = new InterfaceClass()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = word.RootParsedDocument.File,
                Name = word.Text,
                Parent = word.RootParsedDocument.Root,
                Project = word.Project
            };
            interfaceClass.NameReference = word.GetReference();
            interfaceClass.BuildingBlock = interfaceClass;

            if (word.CellDefine) interfaceClass.cellDefine = true;
            word.MoveNext();

            if (nameSpace.BuildingBlock is Root)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parseInterfaceClassItems(prototypeWord, nameSpace, parameterOverrides, null, interfaceClass);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parseInterfaceClassItems(word, nameSpace, parameterOverrides, null, interfaceClass);
            }
            else
            {
                parseInterfaceClassItems(word, nameSpace, parameterOverrides, null, interfaceClass);
            }

            // endclass keyword
            if (word.Text != "endclass")
            {
                word.AddError("endclass expected");
            }
            else
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                interfaceClass.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(interfaceClass.BeginIndexReference, interfaceClass.LastIndexReference);
                word.MoveNext();

                if (!nameSpace.BuildingBlock.NamedElements.ContainsKey(interfaceClass.Name))
                {
                    nameSpace.BuildingBlock.NamedElements.Add(interfaceClass.Name, interfaceClass);
                }

                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (interfaceClass != null && word.Text == interfaceClass.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    else
                    {
                        if (General.IsIdentifier(word.Text))
                        {
                            word.AddError("illegal class name");
                        }
                        else
                        {
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.AddError("illegal class name");
                            word.MoveNext();
                        }
                    }
                }
            }

            bool added = nameSpace.BuildingBlock.AddOrUpdateBuildingBlock(interfaceClass.Name, interfaceClass);
            if (!added && word.Prototype)
            {
                word.AddError("duplicated interface class name");
            }

            return interfaceClass;
        }

        /*
            interface_class_declaration ::= 
                "interface" "class" class_identifier [ parameter_port_list ]  
                [ "extends" interface_class_type { , interface_class_type } ] ;  
                { interface_class_item } "endclass" [ ":" class_identifier]  

            interface_class_type ::= ps_class_identifier [ parameter_value_assignment ]

            interface_class_item ::=
                  type_declaration
                | { attribute_instance } interface_class_method
                | local_parameter_declaration ;
                | parameter_declaration ;
                | ;      
        
            interface_class_method ::=
                "pure" "virtual" method_prototype ;

            method_prototype ::=
                  task_prototype
                | function_prototype

            task_prototype ::= task task_identifier [ ( [ tf_port_list ] ) ]
            function_prototype ::= function data_type_or_void function_identifier [ ( [ tf_port_list ] ) ]

        
            parameter_port_list ::=  
                # ( list_of_param_assignments { , parameter_port_declaration } )  
                | # ( parameter_port_declaration { , parameter_port_declaration } )  
                | #( )   
        */
        protected static void parseInterfaceClassItems(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute,
            InterfaceClass interfaceClass
            )
        {
            while (true)
            {
                if (word.Eof || word.Text == "endclass")
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
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, interfaceClass, null);
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
                        if (interfaceClass.NamedElements.ContainsKey(vkp.Key) && interfaceClass.NamedElements[vkp.Key] is DataObjects.Constants.Constants)
                        {
                            DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)interfaceClass.NamedElements[vkp.Key];
                            if (constants.DefinedReference != null)
                            {
                                constants.DefinedReference.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            interfaceClass.NamedElements.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter() { Name = vkp.Key, DefinedReference = vkp.Value.Reference, Expression = vkp.Value };
                            interfaceClass.NamedElements.Add(param.Name, param);
                        }
                    }
                }

                if (word.Eof || word.Text == "endclass") break;

                // [ "extends" interface_class_type { , interface_class_type } ]  
                if (word.Text == "extends")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    // interface_class_type { , interface_class_type }
                    while (true)
                    {
                        if (!nameSpace.NamedElements.ContainsKey(word.Text) || !(nameSpace.NamedElements[word.Text] is InterfaceClass))
                        {
                            word.AddError("illegal interface_class_type");
                        }
                        else
                        {
                            InterfaceClass baseInterfaceClass = (InterfaceClass)nameSpace.NamedElements[word.Text];
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();

                            // parameter_value_assignment (optional)
                            if (word.Text == "#")
                            {
                                word.MoveNext();
                                if (word.Text == "(")
                                {
                                    word.MoveNext();
                                    if (word.Text != ")")
                                    {
                                        // Skip expression parsing for parameter values
                                        word.SkipToKeyword(")");
                                    }
                                    if (word.Text == ")")
                                    {
                                        word.MoveNext();
                                    }
                                }
                            }

                            // Copy inherited elements from base interface class
                            foreach (INamedElement namedElement in baseInterfaceClass.NamedElements.Values)
                            {
                                if (!interfaceClass.NamedElements.ContainsKey(namedElement.Name))
                                {
                                    interfaceClass.NamedElements.Add(namedElement.Name, namedElement);
                                }
                            }

                            // Add to extended list
                            interfaceClass.ExtendedInterfaceClasses.Add(baseInterfaceClass);
                        }

                        if (word.Text == ",")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            continue;
                        }
                        break;
                    }
                }

                if (word.Eof || word.Text == "endclass") break;

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
                    if (!parseInterfaceClassItem(word, nameSpace, interfaceClass))
                    {
                        if (word.Text == "endclass") break;
                        word.AddError("illegal interface class item");
                        word.MoveNext();
                    }
                }
                break;
            }

            return;
        }

        /// <summary>
        /// Parse interface_class_item
        /// interface_class_item ::= type
        /// </summary>
        private static bool parseInterfaceClassItem(WordScanner word, NameSpace nameSpace, InterfaceClass interfaceClass)
        {
            //interface_class_item ::=
            //      type_declaration
            //    | { attribute_instance } interface_class_method
            //    | local_parameter_declaration ;
            //    | parameter_declaration ;
            //    | ;
            if (word.Eof) return false;

            switch (word.Text)
            {
                // type_declaration
                case "typedef":
                    return parseTypedef(word, interfaceClass);
                // ;
                case ";":
                    word.MoveNext();
                    return true;

                case "endclass":
                    return false;

                // local_parameter_declaration ;
                // parameter_declaration ;
                case "parameter":
                case "localpram":
                    DataObjects.Constants.Constants.ParseCreateDeclaration(word, nameSpace, null);
                    return true;
                // { attribute_instance } interface_class_method
                case "(*":
                    Attribute attribute = Attribute.ParseCreate(word, nameSpace);
                    parseInterfaceClassItem(word, nameSpace, interfaceClass);
                    return true;
                // { attribute_instance } interface_class_method
                case "pure":
                    return MethodPrototype.ParseCreateWithPureVirtual(word, interfaceClass);
                default:
                    return false;
            }
        }

        private static async System.Threading.Tasks.Task<bool> parseImportAsync(WordScanner word, InterfaceClass interfaceClass)
        {
            /*
            import_declaration ::= 
                "import" import_item { , import_item } ;

            import_item ::= 
                package_identifier :: identifier 
                | package_identifier :: *
            */
            if (word.Text != "import") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (word.Text == ";")
                {
                    word.MoveNext();
                    return true;
                }

                // package_identifier
                if (General.IsIdentifier(word.Text))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                if (word.Text == "::")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    if (word.Text == "*")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else if (General.IsIdentifier(word.Text))
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }

                if (word.Text == ",")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    continue;
                }

                break;
            }

            return true;
        }

        private static bool parseTypedef(WordScanner word, InterfaceClass interfaceClass)
        {
            /*
            type_declaration ::= 
                "typedef" data_type type_identifier { variable_dimension } ;
                | "typedef" interface_instance_identifier . termiator_identifier type_identifier { variable_dimension } ;
                | "typedef" [ class_scope ] class_identifier type_identifier ;
            */
            if (word.Text != "typedef") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            word.AddSystemVerilogError();

            // Parse data_type
            IDataType? iDataType = DataTypeFactory.ParseCreate(word, interfaceClass, null);
            if (iDataType == null)
            {
                word.AddError("data type expected");
                word.SkipToKeyword(";");
                return true;
            }

            // type_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal type_identifier");
                word.SkipToKeyword(";");
                return true;
            }

            word.Color(CodeDrawStyle.ColorType.Identifier);
            Typedef typeDef = new Typedef() { Name = word.Text, VariableType = iDataType };
            word.MoveNext();

            // { variable_dimension }
            while (word.Text == "[")
            {
                word.MoveNext();
                if (word.Text != "]")
                {
                    Expressions.Expression.ParseCreate(word, interfaceClass);
                }
                if (word.Text == "]")
                {
                    word.MoveNext();
                }
            }

            if (word.Text == ";")
            {
                word.MoveNext();
                return true;
            }

            return true;
        }

        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }

        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (INamedElement namedElement in NamedElements.Values)
            {
                if (namedElement is IBuildingBlockInstantiation)
                {
                    IBuildingBlockInstantiation instantiation = (IBuildingBlockInstantiation)namedElement;
                    items.Add(newItem(instantiation.Name, CodeDrawStyle.ColorType.Identifier));
                }
            }
        }

        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("interface class ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
        }

        public static List<string> UniqueKeywords = new List<string> {
            "module","endmodule",
            "function","endfunction",
            "task","endtask",
            "always","initial",
            "assign","specify","endspecify",
            "generate","endgenerate"
        };
    }
}
