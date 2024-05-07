using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class Class : BuildingBlock, IModuleOrInterface
    {
        protected Class() : base(null, null)
        {

        }

        // IModuleOrInterfaceOrProgram

        // Port
        public Dictionary<string, DataObjects.Port> Ports { get; } = new Dictionary<string, DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get; } = new List<DataObjects.Port>();

        //        public WordReference NameReference;
        //        public List<string> PortParameterNameList { get; } = new List<string>();

        // Module
        //        public Dictionary<string, ModuleItems.IInstantiation> Instantiations { get; } = new Dictionary<string, ModuleItems.IInstantiation>();


        private WeakReference<Data.IVerilogRelatedFile> fileRef;
        public override Data.IVerilogRelatedFile File
        {
            get
            {
                Data.IVerilogRelatedFile ret;
                if (!fileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
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


        public static Class Create(WordScanner word, Attribute attribute, Data.IVerilogRelatedFile file, bool protoType)
        {
            return Create(word, null, attribute, file, protoType);
        }
        public static Class Create(
            WordScanner word,
            Dictionary<string, Expressions.Expression> parameterOverrides,
            Attribute attribute,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
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
            Class class_ = new Class();
            class_.Parent = word.RootParsedDocument.Root;

            class_.BuildingBlock = class_;
            class_.File = file;
            class_.BeginIndexReference = word.CreateIndexReference();
            if (word.CellDefine) class_.cellDefine = true;
            word.MoveNext();


            // parse definitions
            Dictionary<string, Macro> macroKeep = new Dictionary<string, Macro>();
            foreach (var kvpair in word.RootParsedDocument.Macros)
            {
                macroKeep.Add(kvpair.Key, kvpair.Value);
            }


            if (!word.CellDefine && !protoType)
            {
                // prototype parse
                WordScanner prototypeWord = word.Clone();
                prototypeWord.Prototype = true;
                parseClassItems(prototypeWord, parameterOverrides, null, class_);
                prototypeWord.Dispose();

                // parse
                word.RootParsedDocument.Macros = macroKeep;
                parseClassItems(word, parameterOverrides, null, class_);
            }
            else
            {
                // parse prototype only
                word.Prototype = true;
                parseClassItems(word, parameterOverrides, null, class_);
                word.Prototype = false;
            }

            // endclass keyword
            if (word.Text == "endclass")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                class_.LastIndexReference = word.CreateIndexReference();

                word.AppendBlock(class_.BeginIndexReference, class_.LastIndexReference);
                word.MoveNext();
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
            //            string parameterOverrideModueName,
            Dictionary<string, Expressions.Expression> parameterOverrides,
            Attribute attribute, Class module)
        {

            // module_identifier
            module.Name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal class name");
            }
            else
            {
                module.NameReference = word.GetReference();
            }
            word.MoveNext();

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
                            if (word.Text == "parameter") Verilog.DataObjects.Constants.Parameter.ParseCreateDeclarationForPort(word, module, null);
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
                        if (module.Constants.ContainsKey(vkp.Key))
                        {
                            if (module.Constants[vkp.Key].DefinitionRefrecnce != null)
                            {
                                //                                module.Parameters[vkp.Key].DefinitionRefrecnce.AddNotice("override " + vkp.Value.Value.ToString());
                                module.Constants[vkp.Key].DefinitionRefrecnce.AddHint("override " + vkp.Value.Value.ToString());
                            }

                            module.Constants.Remove(vkp.Key);
                            DataObjects.Constants.Parameter param = new DataObjects.Constants.Parameter();
                            param.Name = vkp.Key;
                            param.Expression = vkp.Value;
                            module.Constants.Add(param.Name, param);
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
                    if (!Items.ClassItem.Parse(word, module))
                    {
                        if (word.Text == "endclass") break;
                        word.AddError("illegal class item");
                        word.MoveNext();
                    }
                }
                break;
            }

            if (!word.Prototype)
            {
                checkVariablesUseAndDriven(word, module);
            }

            return;
        }

        private CodeEditor2.CodeEditor.AutocompleteItem newItem(string text, CodeDrawStyle.ColorType colorType)
        {
            return new CodeEditor2.CodeEditor.AutocompleteItem(text, CodeDrawStyle.ColorIndex(colorType), Global.CodeDrawStyle.Color(colorType));
        }
        public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            foreach (IInstantiation inst in Instantiations.Values)
            {
                if (inst.Name == null) System.Diagnostics.Debugger.Break();
                items.Add(newItem(inst.Name, CodeDrawStyle.ColorType.Identifier));
            }
        }

        protected static void checkVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        {
            foreach (var variable in nameSpace.DataObjects.Values)
            {
                if (variable.DefinedReference == null) continue;

                DataObjects.Variables.ValueVariable valueVar = variable as DataObjects.Variables.ValueVariable;
                if (valueVar == null) continue;

                if (valueVar.AssignedReferences.Count == 0)
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        word.AddNotice(variable.DefinedReference, "undriven & unused");
                    }
                    else
                    {
                        word.AddNotice(variable.DefinedReference, "undriven");
                    }
                }
                else
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        word.AddNotice(variable.DefinedReference, "unused");
                    }
                }
            }
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
