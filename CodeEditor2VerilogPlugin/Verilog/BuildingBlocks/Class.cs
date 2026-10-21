using AjkAvaloniaLibs.Controls;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Class : BuildingBlock, IModuleOrInterface, IModuleOrInterfaceOrCheckerOrClass, DataObjects.DataTypes.IDataType, IPortNameSpace
    {
        public Class() : base(null, null)
        {
            { //function int len();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("randomize", Verilog.DataObjects.DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("randomize", returnVal, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function void pre_randomize();
                List<Port> ports = new List<Port>();
                BuiltInMethod builtInMethod = BuiltInMethod.Create("pre_randomize", null, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function void post_randomize();
                List<Port> ports = new List<Port>();
                BuiltInMethod builtInMethod = BuiltInMethod.Create("post_randomize", null, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function void srandom(int seed);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("seed", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("seed", Verilog.DataObjects.DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("srandom", null, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function string get_randstate();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.String.Create("get_randstate", Verilog.DataObjects.DataTypes.StringType.Create(null));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("get_randstate", returnVal, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function int rand_mode();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("rand_mode", Verilog.DataObjects.DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("rand_mode", returnVal, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //task rand_mode(bit on_off);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("on_off", null, Port.DirectionEnum.Input, DataObjects.Variables.Bit.Create("on_off", Verilog.DataObjects.DataTypes.BitType.Create(false, null)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("rand_mode", null, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //function int constraint_mode();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("constraint_mode", Verilog.DataObjects.DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("constraint_mode", returnVal, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { //task constraint_mode(bit on_off);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("on_off", null, Port.DirectionEnum.Input, DataObjects.Variables.Bit.Create("on_off", Verilog.DataObjects.DataTypes.BitType.Create(false, null)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("constraint_mode", null, ports);
                NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

        }
        /// <summary>
        /// Returns true when <paramref name="methodName"/> is one of the class
        /// built-in methods that shall not be overridden by user code (IEEE
        /// 1800-2017 18.6.3 / 18.8 / 18.9): randomize / srandom / rand_mode /
        /// constraint_mode / get_randstate. pre_randomize / post_randomize are
        /// user callbacks and are therefore overridable.
        /// The list matches the implicit methods registered by the constructor.
        /// </summary>
        public static bool IsNonOverridableBuiltInMethod(string methodName)
        {
            switch (methodName)
            {
                case "randomize":
                case "srandom":
                case "rand_mode":
                case "constraint_mode":
                case "get_randstate":
                    return true;
                default:
                    return false;
            }
        }

        public bool Packable
        {
            get { return false; }
        }
        public bool IsValidForNet { get { return false; } }

        public int? BitWidth { get; } = null;
        public new CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public bool IsVector { get { return false; } }
        public bool IsVirtual { get; set; }
        public virtual bool PartSelectable { get { return false; } }

        /// <summary>
        /// The name space in which this class was declared (the enclosing module
        /// for a module-scope class, the file Root for a root-level class).
        /// Unlike <see cref="NameSpace.Parent"/> (which points at the file Root
        /// for symbol registration), this preserves the lexical scope so that a
        /// class body can resolve sibling classes declared in the same module by
        /// name (mutual / forward references such as
        /// "class C1 { C2 c; } class C2 { C1 c; }").
        /// </summary>
        public NameSpace? DeclaringNameSpace { get; set; } = null;

        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();

        // IModuleOrInterfaceOrProgram

        // Port
        public Dictionary<string, DataObjects.Port> Ports { get; } = new Dictionary<string, DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get; } = new List<DataObjects.Port>();

        //        public WordReference NameReference;
        //        public List<string> PortParameterNameList { get; } = new List<string>();

        // Module
        //        public Dictionary<string, ModuleItems.IInstantiation> Instantiations { get; } = new Dictionary<string, ModuleItems.IInstantiation>();


        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public required override Data.IVerilogRelatedFile File
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
            get { return DataTypeEnum.Class; }
            set { }
        }

        /// <summary>
        /// Base class that this class extends (if any)
        /// </summary>
        public Class? ExtendedClass { get; private set; }

        /// <summary>
        /// List of interface classes that this class implements
        /// </summary>
        public List<InterfaceClass> ImplementedInterfaceClasses { get; } = new List<InterfaceClass>();
        public static void ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
            Class? class_ = ParseCreate(word, nameSpace);
            if (class_ == null) return;

            if (word.Prototype)
            {
                if (word.CompletionContext != null)
                {
                    // do not update building block tree @ code completion partial parse
                }
                else if (!nameSpace.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.NamedElements.Add(class_.Name, class_);
                }
                else
                {
                    word.AddError("duplicate");
                }
            }
            else if (word.CompletionContext != null)
            {
                // do not update building block tree @ code completion partial parse
            }
            else
            {
                if (!nameSpace.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.NamedElements.Add(class_.Name, class_);
                }
                else if (nameSpace.NamedElements[class_.Name] is Class prototypeClass
                         && !ReferenceEquals(prototypeClass, class_))
                {
                    // A class declared inside a module / interface is parsed
                    // twice: the enclosing building block's prototype pass
                    // registers the prototype Class instance and the real pass
                    // produces a fresh Class instance. Only the prototype
                    // instance survives in nameSpace.NamedElements, while the
                    // real instance is the one registered as the building
                    // block (and used as the name space of the class body).
                    // Parameter ports / type parameters registered only by the
                    // prototype pass (e.g. "class c #(int t = 12);") are
                    // therefore missing from the real instance, which makes a
                    // member / method body referencing them report
                    // "unfound object". Carry over the prototype elements that
                    // the real parse did not (re)create.
                    foreach (INamedElement prototypeElement in prototypeClass.NamedElements)
                    {
                        if (class_.NamedElements.ContainsKey(prototypeElement.Name)) continue;
                        class_.NamedElements.Add(prototypeElement.Name, prototypeElement);
                    }
                }
            }
        }


        public IDataType Clone()
        {
            Class class_ = new Class()
            {
                BeginIndexReference = BeginIndexReference,
                DefinitionReference = DefinitionReference,
                File = File,
                Name = Name,
                Parent = Parent,
                Project = Project,
                IsVirtual = IsVirtual,
                ExtendedClass = ExtendedClass
            };
            foreach (var namedElement in NamedElements)
            {
                class_.NamedElements.Add(namedElement.Name, namedElement);
            }
            foreach (var iface in ImplementedInterfaceClasses)
            {
                class_.ImplementedInterfaceClasses.Add(iface);
            }
            return class_;
        }

        public static Class? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, null);
        }
        public static Class? ParseCreate(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression>? parameterOverrides
            )
        {
            //            bool protoType = false;
            /*
            class_declaration ::=  
                    [ "virtual" ] "class" [ lifetime ] class_identifier [ parameter_port_list ] [ "extends" class_type [ ( list_of_arguments ) ] ]  
                    [ "implements" interface_class_type { , interface_class_type } ] ;  
                    { class_item } "endclass" [ ":" class_identifier]  

            interface_class_type ::= ps_class_identifier [ parameter_value_assignment ]

            interface_class_declaration ::= 
                    "interface" "class" class_identifier [ parameter_port_list ]  
                    [ "extends" interface_class_type { , interface_class_type } ] ;  
                    { interface_class_item } "endclass" [ ":" class_identifier]  

            interface_class_item ::= type         
            
            parameter_port_list ::=  
                # ( list_of_param_assignments { , parameter_port_declaration } )  
                | # ( parameter_port_declaration { , parameter_port_declaration } )  
                | #( )   
            
            It shall be legal to omit the constant_param_expression from a param_assignment or the data_type from a type_as-signment only within a parameter_port_list.
            However, it shall not be legal to omit them from localparam declara-tions in a parameter_port_list.

            parameter_port_declaration ::=
                parameter_declaration
              | local_parameter_declaration
              | data_type list_of_param_assignments
              | type list_of_type_assignments

            The parameter keyword can be omitted in a parameter port list. 
            For example:
            ```
            class vector #(size = 1); // size is a parameter in a parameter port list logic [size-1:0] v;
            endclass
            ```
            ```
            interface simple_bus #(AWIDTH = 64, type T = word) // parameter port list 
            (input logic clk) ; // port list
            ...
            endinterface
            ```
             */
            bool virtial = false;
            if (word.Text == "virtual")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                virtial = true;
                if (word.Text != "class")
                {
                    word.AddError("mist be class");
                    return null;
                }
            }

            if (word.Text != "class") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
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
                word.AddError("illegal class name");
                word.SkipToKeyword(";");
                return null;
            }

            Class class_ = new Class()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = word.RootParsedDocument.File,
                Name = word.Text,
                Parent = word.RootParsedDocument.Root,
                Project = word.Project,
                IsVirtual = virtial,
                DeclaringNameSpace = nameSpace
            };
            class_.NameReference = word.GetReference();
            class_.BuildingBlock = class_;

            if (word.CellDefine) class_.cellDefine = true;
            word.MoveNext();

            // the class body is a design element: some compiler directives
            // (e.g. `resetall) are illegal inside of it (IEEE 1800-2017 22.3)
            word.EnterDesignElement();
            try
            {
                if (nameSpace.BuildingBlock is Root)
                {
                    // prototype parse
                    word.Prototype = true;
                    WordScanner prototypeWord = word.Clone(false);
                    word.Prototype = false;
                    // document頭の`* parseによるColor付けを避けるため、prototype modeにしてからCloneする必要がある。

                    parseClassItems(prototypeWord, nameSpace, parameterOverrides, null, class_);
                    prototypeWord.Dispose();

                    // parse
                    word.RootParsedDocument.Macros = macroKeep;
                    parseClassItems(word, nameSpace, parameterOverrides, null, class_);
                }
                else
                {
                    parseClassItems(word, nameSpace, parameterOverrides, null, class_);
                }
            }
            finally
            {
                word.ExitDesignElement();
            }

            /*
            if (!word.CellDefine && !word.Prototype)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parseClassItems(prototypeWord, nameSpace, parameterOverrides, null, class_);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parseClassItems(word, nameSpace, parameterOverrides, null, class_);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                parseClassItems(word, nameSpace, parameterOverrides, null, class_);
                word.Prototype = false;
            }
            */

            // endclass keyword
            if (word.Text == "endclass")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                class_.LastIndexReference = word.CreateIndexReference();
                word.AppendBlock(class_.BeginIndexReference, class_.LastIndexReference);
                word.MoveNext();
            }
            else
            {
                word.AddError("endclass expected");
                class_.LastIndexReference = word.CreateIndexReference();
                word.AppendBlock(class_.BeginIndexReference, class_.LastIndexReference);
            }



            if (word.Text == ":")
            {
                word.MoveNext();
                if (class_ != null && word.Text == class_.Name)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    if (General.IsIdentifier(word.Text))
                    {
                        word.AddError("illegal clas name");
                    }
                    else
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.AddError("illegal clas name");
                        word.MoveNext();
                    }
                }
            }

            // add implicit new function
            if (class_ != null && !class_.NamedElements.ContainsKey("new"))
            {
                Function function = Function.Create(class_, "new");
                class_.NamedElements.Add("new", function);
            }

            // A non-virtual (concrete) class that extends a virtual class with a
            // pure constraint shall implement that constraint. A virtual class
            // itself is not required to (IEEE 1800-2017 18.5.2).
            if (class_ != null && !word.Prototype && word.CompletionContext == null)
            {
                checkPureConstraintsImplemented(word, class_);
                checkInterfaceClassMethodNameConflict(word, class_);
            }

            bool added = nameSpace.BuildingBlock.AddOrUpdateBuildingBlock(class_.Name, class_);
            if (!added && word.Prototype)
            {
                word.AddError("duplicated class name");
            }

            return class_;
        }

        /// <summary>
        /// A pure constraint declared in a virtual (abstract) base class shall be
        /// implemented (redeclared with a body) by every non-virtual derived
        /// class (IEEE 1800-2017 18.5.2). Reports each unimplemented pure
        /// constraint inherited through the extends chain.
        /// </summary>
        private static void checkPureConstraintsImplemented(WordScanner word, Class class_)
        {
            if (class_.IsVirtual) return;

            Class? current = class_.ExtendedClass;
            HashSet<string> checkedNames = new HashSet<string>();
            while (current != null)
            {
                foreach (INamedElement element in current.NamedElements.Values)
                {
                    if (element is not Coverage.ConstraintDeclaration pureConstraint) continue;
                    if (!pureConstraint.IsPure) continue;
                    if (!checkedNames.Add(pureConstraint.Name)) continue;

                    // the derived class implements the constraint by redeclaring
                    // a non-pure constraint of the same name (a pure redeclaration
                    // does not satisfy the requirement).
                    bool implemented = class_.NamedElements.ContainsKey(pureConstraint.Name)
                        && class_.NamedElements[pureConstraint.Name] is Coverage.ConstraintDeclaration impl
                        && !impl.IsPure;
                    if (!implemented)
                    {
                        class_.NameReference?.AddError(
                            "pure constraint '" + pureConstraint.Name + "' must be implemented by non-virtual class");
                    }
                }
                current = current.ExtendedClass;
            }
        }

        /// <summary>
        /// When a class implements several interface classes that declare the
        /// same method name, the derived class must resolve the conflict with a
        /// single method that matches every prototype. When the prototypes differ
        /// (e.g. "void hello()" vs "int hello()"), a single method cannot satisfy
        /// both, so the name conflict is unresolved and it is an error
        /// (IEEE 1800-2017 8.26.6.1).
        /// </summary>
        private static void checkInterfaceClassMethodNameConflict(WordScanner word, Class class_)
        {
            if (class_.ImplementedInterfaceClasses.Count < 2) return;

            // collect method signatures per name across the implemented
            // interface classes (including their extended interface classes).
            Dictionary<string, string> firstSignature = new Dictionary<string, string>();
            Dictionary<string, string> firstOwner = new Dictionary<string, string>();
            foreach (InterfaceClass iface in class_.ImplementedInterfaceClasses)
            {
                foreach (InterfaceClass currentIface in EnumerateInterfaceClassAndBases(iface))
                {
                    foreach (INamedElement element in currentIface.NamedElements.Values)
                    {
                        if (element is not Function && element is not Task_) continue;
                        string name = element.Name;
                        string signature = buildMethodSignature(element);
                        if (!firstSignature.ContainsKey(name))
                        {
                            firstSignature[name] = signature;
                            firstOwner[name] = currentIface.Name;
                            continue;
                        }
                        if (firstSignature[name] != signature)
                        {
                            if (class_.NameReference != null)
                            {
                                class_.NameReference.AddError(
                                    "unresolved interface class method name conflict: '"
                                    + name + "' declared differently in "
                                    + firstOwner[name] + " and " + currentIface.Name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates an interface class and every interface class it extends
        /// (breadth-first, cycle-safe).
        /// </summary>
        private static IEnumerable<InterfaceClass> EnumerateInterfaceClassAndBases(InterfaceClass iface)
        {
            HashSet<InterfaceClass> visited = new HashSet<InterfaceClass>();
            Queue<InterfaceClass> queue = new Queue<InterfaceClass>();
            queue.Enqueue(iface);
            while (queue.Count > 0)
            {
                InterfaceClass current = queue.Dequeue();
                if (!visited.Add(current)) continue;
                yield return current;
                foreach (InterfaceClass baseIface in current.ExtendedInterfaceClasses)
                {
                    queue.Enqueue(baseIface);
                }
            }
        }

        /// <summary>
        /// Builds a comparable signature (method kind + return type + port types
        /// / directions) for a function / task prototype so that two prototypes
        /// with the same name can be compared.
        /// </summary>
        private static string buildMethodSignature(INamedElement element)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (element is Task_)
            {
                sb.Append("task ");
            }
            else if (element is Function function)
            {
                sb.Append("function ");
                sb.Append(function.ReturnVariable?.DataType?.CreateString() ?? "void");
                sb.Append(" ");
            }

            IPortNameSpace? portNameSpace = element as IPortNameSpace;
            if (portNameSpace != null)
            {
                sb.Append("(");
                foreach (DataObjects.Port port in portNameSpace.PortsList)
                {
                    sb.Append(port.Direction.ToString());
                    sb.Append(" ");
                    sb.Append(port.DataObject?.DataType?.CreateString() ?? "");
                    sb.Append(", ");
                }
                sb.Append(")");
            }
            return sb.ToString();
        }

        /*
            class_declaration ::=  
                    [ "virtual" ] "class" [ lifetime ] class_identifier [ parameter_port_list ]  
                    [ "extends" class_type [ ( list_of_arguments ) ] ]  
                    [ "implements" interface_class_type { , interface_class_type } ] ;  
                    { class_item } "endclass" [ ":" class_identifier]  

            parameter_port_list ::=  
                # ( list_of_param_assignments { , parameter_port_declaration } )  
                | # ( parameter_port_declaration { , parameter_port_declaration } )  
                | #( )   
        */
        /// <summary>
        /// Skip an optional parameter_value_assignment "#( ... )" that may follow
        /// an interface_class_type, e.g. "implements Package::Bar#(1, 2)".
        /// Positions at the token after the closing ")".
        /// </summary>
        private static void SkipParameterValueAssignment(WordScanner word)
        {
            if (word.Text != "#") return;
            word.MoveNext();
            if (word.Text != "(") return;
            int depth = 0;
            while (!word.Eof)
            {
                if (word.Text == "(") depth++;
                else if (word.Text == ")")
                {
                    depth--;
                    word.MoveNext();
                    if (depth <= 0) break;
                    continue;
                }
                word.MoveNext();
            }
        }

        protected static void parseClassItems(
            WordScanner word,
            NameSpace nameSpace,
            //            string parameterOverrideModueName,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute,
            Class class_
            )
        {


            while (!word.Eof)
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
                            if (word.Text == ")") break;   // # ( )  : empty parameter_port_list
                           if (word.Text == "type")
                           {
                               // parameter_port_declaration ::= "type" list_of_type_assignments
                               // ex: class Foo #(type Int = int, type T2);
                               IndexReference beforeTypeRef = word.CreateIndexReference();
                               Verilog.DataObjects.Constants.Constants.ParseCreateTypeAssignmentsForPort(word, class_);
                               if (beforeTypeRef.IsSameAs(word.CreateIndexReference()))
                               {   // error recovery: no progress on broken type declaration
                                   word.AddError("illegal type parameter declaration");
                                   word.MoveNext();
                               }
                           }
                           else if (word.Text == "parameter")
                           {
                               IndexReference beforeParamRef = word.CreateIndexReference();
                                Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, class_, null);
                                if (beforeParamRef.IsSameAs(word.CreateIndexReference()))
                                {   // error recovery: no progress on broken parameter declaration
                                    // consume one token to keep the loop advancing
                                    word.AddError("illegal parameter declaration");
                                    word.MoveNext();
                                }
                            }
                            else if (General.IsIdentifier(word.Text))
                            {   // parameter_port_declaration without "parameter" keyword
                                //  parameter_port_list ::= # ( list_of_param_assignments { , parameter_port_declaration } )
                                //                        | # ( parameter_port_declaration { , parameter_port_declaration } )
                                //  parameter_port_declaration ::= data_type list_of_param_assignments (implicit type list_of_param_assignments)
                                IndexReference beforeRef = word.CreateIndexReference();
                                DataObjects.DataTypes.IDataType? dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, class_, null);
                                if (beforeRef.IsSameAs(word.CreateIndexReference()))
                                {   // no data type keyword consumed : implicit data type param_assignment list
                                    DataObjects.Constants.Constants.ParseCreateParamAssignmentsForPort(word, class_, null);
                                }
                                else
                                {   // data_type list_of_param_assignments
                                    DataObjects.Constants.Constants.ParseCreateParamAssignmentsForPort(word, class_, dataType);
                                }
                            }
                            else
                            {
                                word.AddError("illegal parameter port declaration");
                                word.MoveNext();
                            }
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
                        if (class_.NamedElements.ContainsKey(vkp.Key) && class_.NamedElements[vkp.Key] is DataObjects.Constants.Constants)
                        {
                            DataObjects.Constants.Constants constants = (DataObjects.Constants.Constants)class_.NamedElements[vkp.Key];
                            if (constants.DefinedReference != null)
                            {
                                //                                module.Parameters[vkp.Key].DefinitionRefrecnce.AddNotice("override " + vkp.Value.Value.ToString());
                                constants.DefinedReference.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            class_.NamedElements.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter() { Name = vkp.Key, DefinedReference = vkp.Value.Reference, Expression = vkp.Value };
                            class_.NamedElements.Add(param.Name, param);
                        }
                        else
                        {
                            //System.Diagnostics.Debug.Print("undefed params "+module.File.Name +":" + vkp.Key );
                        }
                    }
                }

                if (word.Eof || word.Text == "endclass") break;
                //if (word.Text == "(")
                //{
                //    parseListOfPorts_ListOfPortsDeclarations(word, module);
                //} // list_of_ports or list_of_posrt_declarations


                // [ "extends" class_type [ ( list_of_arguments ) ] ]  
                if (word.Text == "extends")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    // class_type ::= [ "$unit" "." ] [ package_scope ] class_identifier
                    // resolve through package scope (pkg::B) when "::" follows
                    Class? baseClass = null;
                    {
                        // package_scope: ps_identifier :: or $unit .
                        if (word.NextText == "::" || (word.Text == "$unit" && word.NextText == "."))
                        {
                            string scopeName = word.Text;
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            word.MoveNext(); // :: or .

                            if (scopeName == "$unit")
                            {
                                INamedElement? unitElement = null;
                                nameSpace.NamedElements.TryGetValue(word.Text, out unitElement);
                                baseClass = unitElement as Class;
                            }
                           else
                           {
                               // resolve via package name space file
                               // (the package file's parsed Root.NamedElements holds
                               //  its classes; the local nameSpace does not)
                               var packageFile = word.ProjectProperty.PackageNameSpace.GetFile(scopeName);
                               if (packageFile != null
                                   && packageFile.VerilogParsedDocument != null
                                   && packageFile.VerilogParsedDocument.Root != null)
                               {
                                   INamedElement? pkgElement = null;
                                   packageFile.VerilogParsedDocument.Root.NamedElements.TryGetValue(word.Text, out pkgElement);
                                   baseClass = pkgElement as Class;
                               }
                               if (baseClass == null)
                               {
                                   // same-file package: the package building block
                                   // lives in this document's Root.BuildingBlocks
                                   if (word.RootParsedDocument.Root != null
                                       && word.RootParsedDocument.Root.BuildingBlocks.TryGetValue(scopeName, out BuildingBlock? localPackage)
                                       && localPackage is Package)
                                   {
                                       INamedElement? pkgElement = null;
                                       localPackage.NamedElements.TryGetValue(word.Text, out pkgElement);
                                       baseClass = pkgElement as Class;
                                   }
                               }
                           }
                        }
                        else if (nameSpace.NamedElements.ContainsKey(word.Text) && nameSpace.NamedElements[word.Text] is Class)
                        {
                            baseClass = (Class)nameSpace.NamedElements[word.Text];
                        }
                        else
                        {
                            // cross-file class resolution via UnitNameSpace / Root building blocks
                            INamedElement? element = null;
                            var file = word.ProjectProperty.DefinitionNameSpace.GetFile(word.Text);
                            if (file != null)
                            {
                                baseClass = word.ProjectProperty.DefinitionNameSpace.Get(word.Text) as Class;
                            }
                            if (baseClass == null)
                            {
                                // cross-file class resolution via DefinitionNameSpace / upward search
                                BuildingBlock? upward = nameSpace.BuildingBlock.SearchBuildingBlockUpward(word.Text);
                                baseClass = upward as Class;
                            }
                        }
                    }

                    if (baseClass == null)
                    {
                        word.AddError("illegal class_type");
                    }
                    else
                    {
                        class_.ExtendedClass = baseClass; // Store reference to extended class
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();

                       // parameter_value_assignment (optional)
                       // class_type can have parameter values like: base_class#(32, 8) or #((x,y,z)
                       // parse with ParameterValueAssignment (named / ordered both supported)
                       if (word.Text == "#")
                       {
                           Dictionary<string, Expressions.Expression> baseParameterOverrides = new Dictionary<string, Expressions.Expression>();
                           Verilog.ParameterValueAssignment.ParseCreate(word, nameSpace, baseParameterOverrides, baseClass);
                       }

                        Function? constructor = null;
                        if (baseClass != null)
                        {
                            baseClass.NamedElements.TryGetValue("new", out INamedElement? newElement);
                            constructor = newElement as Function;
                        }

                        if (word.Text == "(")
                        {
                            Dictionary<string, Expressions.Expression> portConnection = new Dictionary<string, Expressions.Expression>();
                            Expressions.ListOfArguments.ParseListOfArguments(word, nameSpace, constructor, portConnection);
                        }

                        DataObjects.Variables.Object superClassObject = DataObjects.Variables.Object.Create("super", baseClass);
                        superClassObject.Defined = true;
                        class_.NamedElements.Add(superClassObject.Name, superClassObject);

                        // Inherit elements from base class
                        foreach (INamedElement namedElement in baseClass.NamedElements.Values)
                        {
                            if (namedElement is DataObjects.DataObject)
                            {
                                DataObjects.DataObject dataObject = (DataObjects.DataObject)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, dataObject);
                            }
                            else if (namedElement is Typedef)
                            {
                                Typedef typeDef = (Typedef)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, typeDef);
                            }
                            else if (namedElement is Function)
                            {
                                Function function = (Function)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, function);
                            }
                            else if (namedElement is Task_)
                            {
                                Task_ task = (Task_)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, task);
                            }
                        }

                    }
                }

                if (word.Eof || word.Text == "endclass") break;


                // ["implements" interface_class_type { , interface_class_type } ] ;
                if (word.Text == "implements")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    while (!word.Eof)
                    {
                        // interface_class_type ::= ps_class_identifier [ parameter_value_assignment ]
                        // resolution: package scope (Pkg::Bar) or local / cross-file
                        if (word.NextText == "::")
                        {
                            string scopeName = word.Text;
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();
                            word.MoveNext(); // ::

                            var packageFile = word.ProjectProperty.PackageNameSpace.GetFile(scopeName);
                            if (packageFile != null
                                && packageFile.VerilogParsedDocument != null
                                && packageFile.VerilogParsedDocument.Root != null
                                && packageFile.VerilogParsedDocument.Root.NamedElements.TryGetValue(word.Text, out INamedElement? pkgElement)
                                && pkgElement is InterfaceClass pkgIface)
                            {
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                word.MoveNext();
                                SkipParameterValueAssignment(word);
                                class_.ImplementedInterfaceClasses.Add(pkgIface);
                                foreach (INamedElement namedElement in pkgIface.NamedElements.Values)
                                {
                                    if (!class_.NamedElements.ContainsKey(namedElement.Name))
                                    {
                                        class_.NamedElements.Add(namedElement.Name, namedElement);
                                    }
                                }
                            }
                            else if (word.RootParsedDocument.Root != null
                                && word.RootParsedDocument.Root.BuildingBlocks.TryGetValue(scopeName, out BuildingBlock? localPackage)
                                && localPackage is Package
                                && localPackage.NamedElements.TryGetValue(word.Text, out INamedElement? localPkgElement)
                                && localPkgElement is InterfaceClass localPkgIface)
                            {
                                // same-file package: the package building block
                                // lives in this document's Root.BuildingBlocks
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                word.MoveNext();
                                SkipParameterValueAssignment(word);
                                class_.ImplementedInterfaceClasses.Add(localPkgIface);
                                foreach (INamedElement namedElement in localPkgIface.NamedElements.Values)
                                {
                                    if (!class_.NamedElements.ContainsKey(namedElement.Name))
                                    {
                                        class_.NamedElements.Add(namedElement.Name, namedElement);
                                    }
                                }
                            }
                            else
                            {
                                word.AddError("illegal interface_class_type");
                            }

                            if (word.Text == ",")
                            {
                                word.Color(CodeDrawStyle.ColorType.Keyword);
                                word.MoveNext();
                                continue;
                            }
                            break;
                        }

                        InterfaceClass? ifaceClass = null;
                        if (nameSpace.NamedElements.ContainsKey(word.Text) && nameSpace.NamedElements[word.Text] is InterfaceClass localIface)
                        {
                            ifaceClass = localIface;
                        }
                        else
                        {
                            // cross-file resolution
                            ifaceClass = word.ProjectProperty.DefinitionNameSpace.Get(word.Text) as InterfaceClass;
                            if (ifaceClass == null)
                            {
                                ifaceClass = word.ProjectProperty.UnitNameSpace.Get(word.Text) as InterfaceClass;
                            }
                            if (ifaceClass == null)
                            {
                                BuildingBlock? upward = nameSpace.BuildingBlock.SearchBuildingBlockUpward(word.Text);
                                ifaceClass = upward as InterfaceClass;
                            }
                        }
                        if (ifaceClass == null)
                        {
                            word.AddError("illegal interface_class_type");
                        }
                        else
                        {
                            InterfaceClass interfaceClass = ifaceClass;
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();

                            // parameter_value_assignment (optional)
                            // interface_class_type can have parameter values like: my_interface_class#(32)
                            if (word.Text == "#")
                            {
                                word.MoveNext();
                                if (word.Text == "(")
                                {
                                    word.MoveNext();
                                    if (word.Text != ")")
                                    {
                                        // Parse parameter expressions
                                        word.SkipToKeyword(")");
                                    }
                                    if (word.Text == ")")
                                    {
                                        word.MoveNext();
                                    }
                                }
                            }

                            // Add to implemented interface classes list
                            class_.ImplementedInterfaceClasses.Add(interfaceClass);

                            // Inherit elements from implemented interface class (methods, typedefs, etc.)
                            foreach (INamedElement namedElement in interfaceClass.NamedElements.Values)
                            {
                                if (!class_.NamedElements.ContainsKey(namedElement.Name))
                                {
                                    class_.NamedElements.Add(namedElement.Name, namedElement);
                                }
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
                }
                if (word.GetCharAt(0) == ';')
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                }


                // A class declared inside a module / interface is parsed once per
                // enclosing building block pass. Parameter ports / type
                // parameters may have been registered by an earlier pass on a
                // different Class instance with the same name (e.g. the module's
                // prototype pass). Carry them over before the body is parsed so
                // that method bodies referencing them resolve.
                if (!ReferenceEquals(nameSpace.NamedElements, class_.NamedElements)
                    && nameSpace.NamedElements.ContainsKey(class_.Name)
                    && nameSpace.NamedElements[class_.Name] is Class earlierClass
                    && !ReferenceEquals(earlierClass, class_))
                {
                    foreach (INamedElement earlierElement in earlierClass.NamedElements)
                    {
                        if (class_.NamedElements.ContainsKey(earlierElement.Name)) continue;
                        class_.NamedElements.Add(earlierElement.Name, earlierElement);
                    }
                }

                while (!word.Eof)
                {
                    if (!Verilog.Items.ClassItem.Parse(word, class_))
                    {
                        if (word.Text == "endclass") break;
                        word.AddError("illegal class item");
                        // error recovery: skip to the end of the broken item,
                        // stop at structural boundaries to keep the rest parseable
                        if (!word.SkipToKeyword(";"))
                        {
                            word.MoveNext();
                        }
                        else
                        {
                            if (word.Text == ";") word.MoveNext();
                        }
                    }
                }
                break;
            }

            //if (!word.Prototype)
            //{
            //    checkVariablesUseAndDriven(word, class_);
            //}

            return;
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
            label.AppendText("class ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
        }

        //protected static void checkVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        //{
        //    foreach (var variable in nameSpace.DataObjects.Values)
        //    {
        //        if (variable.DefinedReference == null) continue;

        //        DataObjects.Variables.ValueVariable valueVar = variable as DataObjects.Variables.ValueVariable;
        //        if (valueVar == null) continue;

        //        if (valueVar.AssignedReferences.Count == 0)
        //        {
        //            if (valueVar.UsedReferences.Count == 0)
        //            {
        //                word.AddNotice(variable.DefinedReference, "undriven & unused");
        //            }
        //            else
        //            {
        //                word.AddNotice(variable.DefinedReference, "undriven");
        //            }
        //        }
        //        else
        //        {
        //            if (valueVar.UsedReferences.Count == 0)
        //            {
        //                word.AddNotice(variable.DefinedReference, "unused");
        //            }
        //        }
        //    }
        //}


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
