using Avalonia.Input;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Class : BuildingBlock, IModuleOrInterface, DataObjects.DataTypes.IDataType
    {
        protected Class() : base(null, null)
        {

        }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public bool IsVector { get { return false; } }

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
                Data.IVerilogRelatedFile ret;
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
        public static void ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
            Class class_ = Create(word, nameSpace);
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

        public static Class? Create(WordScanner word, NameSpace nameSpace)
        {
            return Create(word, nameSpace, null);
        }
        public static Class? Create(
            WordScanner word,
            NameSpace nameSpace,
            Dictionary<string, Expressions.Expression>? parameterOverrides
            )
        {
            bool protoType = false;
            /*
            class_declaration ::=  
                    [ "virtual" ] "class" [ lifetime ] class_identifier [ parameter_port_list ]  
                    [ "extends" class_type [ ( list_of_arguments ) ] ]  
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

            if (word.Text != "class") System.Diagnostics.Debugger.Break();
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
                Project = word.Project
            };
            class_.NameReference = word.GetReference();
            class_.BuildingBlock = class_;

            if (word.CellDefine) class_.cellDefine = true;
            word.MoveNext();

            if (!word.CellDefine && !protoType)
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

            // endclass keyword
            if (word.Text == "endclass")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                class_.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(class_.BeginIndexReference, class_.LastIndexReference);
                word.MoveNext();

                if (!nameSpace.BuildingBlock.NamedElements.ContainsKey(class_.Name))
                {
                    nameSpace.BuildingBlock.NamedElements.Add(class_.Name, class_);
                }

                return class_;
            }

            {
                word.AddError("endclass expected");
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
        protected static void parseClassItems(
            WordScanner word,
            NameSpace nameSpace,
            //            string parameterOverrideModueName,
            Dictionary<string, Expressions.Expression> parameterOverrides,
            Attribute attribute, Class class_)
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
                            if (constants.DefinitionRefrecnce != null)
                            {
                                //                                module.Parameters[vkp.Key].DefinitionRefrecnce.AddNotice("override " + vkp.Value.Value.ToString());
                                constants.DefinitionRefrecnce.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            class_.NamedElements.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter() { Name = vkp.Key };
                            param.Expression = vkp.Value;
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
                    if (!Items.ClassItem.Parse(word, class_))
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
            throw new NotImplementedException();
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
