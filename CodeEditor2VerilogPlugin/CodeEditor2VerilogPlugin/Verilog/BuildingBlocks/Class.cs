using AjkAvaloniaLibs.Controls;
using Avalonia.Input;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.Data;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.ModuleItems;
using Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static pluginVerilog.Verilog.ModuleItems.ModuleInstantiation;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Class : BuildingBlock, IModuleOrInterface, IModuleOrInterfaceOrCheckerOrClass, DataObjects.DataTypes.IDataType, IPortNameSpace
    {
        protected Class() : base(null,null)
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
        public bool IsVirtual { get; set; }
        public virtual bool PartSelectable { get { return false; } }

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

        public DataTypeEnum Type {
            get { return DataTypeEnum.Class; }
            set { } 
        }
        public static async System.Threading.Tasks.Task ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
            Class? class_ = await Create(word, nameSpace);
            if (class_ == null) return;

            if (word.Prototype)
            {
                if (!nameSpace.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.NamedElements.Add(class_.Name, class_);
                }
                else
                {
                    word.AddError("duplicate");
                }
            }
            else
            {
                if (!nameSpace.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.NamedElements.Add(class_.Name, class_);
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
                IsVirtual = IsVirtual
            };
            foreach(var namedElement in NamedElements)
            {
                class_.NamedElements.Add(namedElement.Name,namedElement);
            }
            return class_;
        }

        public static async Task<Class?> Create(WordScanner word, NameSpace nameSpace)
        {
            return await Create(word, nameSpace, null);
        }
        public static async Task<Class?> Create(
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
             
             */
            bool virtial = false;
            if(word.Text == "virtual")
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

            Class class_ = new Class() {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = word.RootParsedDocument.File,
                Name = word.Text,
                Parent = word.RootParsedDocument.Root,
                Project = word.Project,
                IsVirtual = virtial
            };
            class_.NameReference = word.GetReference();
            class_.BuildingBlock = class_;

            if (word.CellDefine) class_.cellDefine = true;
            word.MoveNext();

            if(nameSpace.BuildingBlock is Root)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
//                WordScanner prototypeWord = word;
                prototypeWord.Prototype = true;
                await parseClassItems(prototypeWord, nameSpace, parameterOverrides, null, class_);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                await parseClassItems(word, nameSpace, parameterOverrides, null, class_);
            }
            else
            {
                await parseClassItems(word, nameSpace, parameterOverrides, null, class_);
            }

            await parseClassItems(word, nameSpace, parameterOverrides, null, class_);
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
            if (word.Text != "endclass")
            {
                word.AddError("endclass expected");
            }
            else
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                class_.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(class_.BeginIndexReference, class_.LastIndexReference);
                word.MoveNext();

                if (!nameSpace.BuildingBlock.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.BuildingBlock.NamedElements.Add(class_.Name, class_);
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
            }

            // add implicit new function
            if (class_ != null && !class_.NamedElements.ContainsKey("new"))
            {
                Function function = Function.Create(class_,"new");
                class_.NamedElements.Add("new", function);
            }


            if (word.RootParsedDocument?.Root == null)
            {

            }
            else if (!word.RootParsedDocument.Root.BuildingBlocks.ContainsKey(class_.Name))
            {
                word.RootParsedDocument.Root.BuildingBlocks.Add(class_.Name, class_);
            }
            else if(word.Prototype)
            {
                word.AddError("duplicated class name");
            }
            else
            {
                word.RootParsedDocument.Root.BuildingBlocks[class_.Name]=class_;
            }

            return class_;
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
        protected static async System.Threading.Tasks.Task parseClassItems(
            WordScanner word,
            NameSpace nameSpace,
            //            string parameterOverrideModueName,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute? attribute, 
            Class class_
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
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, class_, null);
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


                // [ "extends" class_type[(list_of_arguments)] ]  
                if(word.Text == "extends")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    if (!nameSpace.NamedElements.ContainsKey(word.Text) || !(nameSpace.NamedElements[word.Text] is Class))
                    {
                        word.AddError("illegal class_type");
                    }
                    else
                    {
                        Class baseClass = (Class)nameSpace.NamedElements[word.Text];
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        if(word.Text == "(")
                        {
                            word.MoveNext();
                            parseListOfPortConnections(word, nameSpace, class_);
                            if (word.Text == ")")
                            {
                                word.MoveNext();
                            }
                            else
                            {
                                word.AddError("illegal port connection");
                            }
                        }

                        DataObjects.Variables.Object superClassObject = DataObjects.Variables.Object.Create("super", baseClass);
                        superClassObject.Defined = true;
                        class_.NamedElements.Add( superClassObject.Name,superClassObject );

                        foreach(INamedElement namedElement in baseClass.NamedElements.Values)
                        {
                            if(namedElement is DataObjects.DataObject)
                            {
                                DataObjects.DataObject dataObject = (DataObjects.DataObject)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, namedElement);
                            } else if(namedElement is Typedef)
                            {
                                Typedef typeDef = (Typedef)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, namedElement);
                            } else if(namedElement is Function)
                            {
                                Function function = (Function)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, namedElement);
                            } else if(namedElement is Task)
                            {
                                Task task = (Task)namedElement;
                                if (!class_.NamedElements.ContainsKey(namedElement.Name)) class_.NamedElements.Add(namedElement.Name, namedElement);
                            }
                        }

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
                    if (!await Items.ClassItem.Parse(word, class_))
                    {
                        if (word.Text == "endclass") break;
                        word.AddError("illegal class item");
                        word.MoveNext();
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

        private static void parseListOfPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Class? class_)
            //,
            //ModuleInstantiation moduleInstantiation,
            //WordReference moduleIdentifier
        {
            /*
            list_of_port_connections ::= 
                  ordered_port_connection { "," ordered_port_connection }
                | named_port_connection { "," named_port_connection }
             */

            if (word.GetCharAt(0) == '.')
            { // named port assignment
                parseNamedPortConnections(word, nameSpace, class_);
            }
            else
            { // ordered port assignment
                parseOrderedPortConnections(word, nameSpace, class_);
            }
        }

        private static void parseOrderedPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Class? class_)
            //,
            //ModuleInstantiation moduleInstantiation,
            //WordReference moduleIdentifier)
        {
            /*
            ordered_port_connection ::= { attribute_instance } [ expression ]
             */
            int i = 0;
            while (!word.Eof && word.Text != ")")
            {
                string pinName = "";
                if (class_ != null && i < class_.PortsList.Count)
                {
                    pinName = class_.PortsList[i].Name;
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
//                    if (word.Prototype && expression != null && !moduleInstantiation.PortConnection.ContainsKey(pinName)) moduleInstantiation.PortConnection.Add(pinName, expression);
                }
                else
                {
                    if (class_ != null) word.AddError("illegal port connection");
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                }
                if (word.Text != ",")
                {
                    break;
                }
                else
                {
                    word.MoveNext();
                }
            }
        }

        private static void parseNamedPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Class? class_)
            //,
            //ModuleInstantiation moduleInstantiation,
            //WordReference moduleIdentifier)
        {
            /*
            named_port_connection ::= 
                  { attribute_instance } "." port_identifier [ "(" [ expression ] ")" ] 
                | { attribute_instance } ".*"
             
             */

            //bool wildcardConnection = false;

            //if (word.Eof) return;

            //List<string> notWrittenPortName;
            //if (class_ == null)
            //{
            //    notWrittenPortName = new List<string>();
            //}
            //else
            //{
            //    notWrittenPortName = class_.Ports.Keys.ToList();
            //}

            //WordReference? wildcardRef = null;
            //while (!word.Eof && word.Text == ".")
            //{
            //    word.MoveNext();    // .
            //    if (word.Text == "*") // 23.3.2.4 Connecting module instances using wildcard named port connections ( .*)
            //    {
            //        wildcardConnection = true;
            //        word.Color(CodeDrawStyle.ColorType.Identifier);
            //        wildcardRef = word.GetReference();
            //        word.MoveNext();
            //        if (word.Text != ",")
            //        {
            //            break;
            //        }
            //        else
            //        {
            //            word.MoveNext();
            //        }
            //        continue;
            //    }

            //    string pinName = word.Text;
            //    IndexReference startRef = word.CreateIndexReference();
            //    if (notWrittenPortName.Contains(pinName)) notWrittenPortName.Remove(pinName);
            //    WordReference pinReference = word.GetReference();
            //    word.Color(CodeDrawStyle.ColorType.Identifier);

            //    if (word.Prototype)
            //    {
            //        //if (moduleInstantiation.PortConnection.ContainsKey(pinName))
            //        //{
            //        //    word.AddPrototypeError("duplicated");
            //        //}
            //        word.MoveNext();
            //    }
            //    else
            //    {
            //        if (class_ != null)
            //        {
            //            if (class_.Ports.ContainsKey(pinName))
            //            {

            //            }
            //            else
            //            {
            //                word.AddError("illegal port name");
            //            }
            //        }
            //        word.MoveNext();

            //        PortReference pRef = new PortReference(pinName, startRef, word.CreateIndexReference());
            //        //moduleInstantiation.PortReferences.Add(pRef);
            //    }

            //    if (word.Text == "(")
            //    {
            //        parseNamedPortConnection(word, nameSpace, class_, moduleInstantiation, pinName, moduleIdentifier);
            //    }
            //    else
            //    {
            //        // 23.3.2.3 Connecting module instance using implicit named port connections
            //        parseImplicitPortConnection(word, nameSpace, class_, moduleInstantiation, pinName, pinReference, moduleIdentifier);
            //    }

            //    if (word.Text != ",")
            //    {
            //        break;
            //    }
            //    else
            //    {
            //        word.MoveNext();
            //    }
            //}

            //if (notWrittenPortName.Count != 0)
            //{
            //    if (wildcardConnection)
            //    {
            //        parseWidCardPortConnection(word, nameSpace, class_, moduleInstantiation, moduleIdentifier, notWrittenPortName, wildcardRef);
            //    }
            //    else
            //    {
            //        if (!word.Prototype) moduleIdentifier.AddWarning("missing port " + notWrittenPortName[0]);
            //    }
            //}

        }





        private AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach(INamedElement namedElement in NamedElements.Values)
            {
                if(namedElement is IBuildingBlockInstantiation)
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
