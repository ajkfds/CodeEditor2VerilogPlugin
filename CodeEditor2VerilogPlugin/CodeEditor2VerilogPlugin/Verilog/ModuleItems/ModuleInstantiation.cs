//using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public class ModuleInstantiation : Item,IBuildingBlockInstantiation,INamedElement
    {
        public NamedElements NamedElements { get; } = new NamedElements();

        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Identifier; } }
        //protected ModuleInstantiation() { }
        /*
        A.4.1.1 Module instantiation 

        module_instantiation ::= 
            module_identifier [ parameter_value_assignment ] hierarchical_instance { , hierarchical_instance } ;

        parameter_value_assignment ::= "#" "(" [ list_of_parameter_assignments ] ")"

        list_of_parameter_assignments ::=
              ordered_parameter_assignment { "," ordered_parameter_assignment }
            | named_parameter_assignment { , named_parameter_assignment }

        ordered_parameter_assignment ::= param_expression

        named_parameter_assignment ::= . parameter_identifier "(" [ param_expression ] ")"

        hierarchical_instance ::= name_of_instance "(" [ list_of_port_connections ] ")"

        name_of_instance ::= instance_identifier { unpacked_dimension } 

        list_of_port_connections ::= 
              ordered_port_connection { "," ordered_port_connection }
            | named_port_connection { "," named_port_connection }

        ordered_port_connection ::= { attribute_instance } [ expression ]

        named_port_connection ::= 
              { attribute_instance } "." port_identifier [ "(" [ expression ] ")" ] 
            | { attribute_instance } ".*"
        */
        public required string SourceName{ get; init; }
        public required string SourceProjectName { get;init; }

        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }
            
        public Dictionary<string, Expressions.Expression> PortConnection { get; set; } = new Dictionary<string, Expressions.Expression>();

        public List<PortReference> PortReferences = new List<PortReference>();
        public class PortReference
        {
            public PortReference(string name, IndexReference beginIndexReference, IndexReference lastIndexReference)
            {
                BeginIndexReference = beginIndexReference;
                LastIndexReference = lastIndexReference;
                Name = name;
            }
            public readonly IndexReference BeginIndexReference;
            public readonly IndexReference LastIndexReference;
            public readonly string Name;
        }
        public void AppendLabel(IndexReference iref,AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            PortReference? portRef = null;
            foreach(PortReference pRef in PortReferences)
            {
                if (iref.IsSmallerThan(pRef.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(pRef.LastIndexReference)) continue;
                portRef = pRef;
                break;
            }
            if (portRef == null) return;

            string portName = portRef.Name;
            Module? originalModule = ProjectProperty.GetBuildingBlock(SourceName) as Module;
            if (originalModule == null) return;
            if (!originalModule.Ports.ContainsKey(portName)) return;
            Verilog.DataObjects.Port port = originalModule.Ports[portName];
            label.AppendLabel(port.GetLabel());
        }

        public string OverrideParameterID
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(SourceName);
                sb.Append(":");
                foreach (var kvp in ParameterOverrides)
                {
                    sb.Append(kvp.Key);
                    sb.Append("=");
                    sb.Append(kvp.Value.Value.ToString());
                    sb.Append(",");
                }
                return sb.ToString();
            }
        }

        public Project GetInstancedBuildingBlockProject()
        {
            Project sourceProject = Project;
            if (CodeEditor2.Global.Projects.ContainsKey(SourceProjectName))
            {
                sourceProject = CodeEditor2.Global.Projects[SourceProjectName];
            }
            return sourceProject;
        }
        public BuildingBlock? GetInstancedBuildingBlock()
        {
            ProjectProperty projectProperty = ProjectProperty;
            Project sourceProject = Project;
            if (CodeEditor2.Global.Projects.ContainsKey(SourceProjectName)) 
            {
                sourceProject = CodeEditor2.Global.Projects[SourceProjectName];
                projectProperty = (ProjectProperty)sourceProject.GetPluginProperty();
            }

            Data.IVerilogRelatedFile? file = projectProperty.GetFileOfBuildingBlock(SourceName);
            if(file == null) return null;
            if (file is not Data.VerilogFile) return null;

            Data.VerilogFile source = (Data.VerilogFile)file;
            if (source == null) return null;

            CodeEditor2.CodeEditor.ParsedDocument? codeEditorParsedDocument = source.GetInstancedParsedDocument(OverrideParameterID);
            if (codeEditorParsedDocument is not ParsedDocument) return null;
            ParsedDocument? parsedDocument = (ParsedDocument)codeEditorParsedDocument;
            if (parsedDocument == null) return null;
            if (parsedDocument.Root == null) return null;
            if (!parsedDocument.Root.BuildingBlocks.ContainsKey(SourceName)) return null;
            return parsedDocument.Root.BuildingBlocks[SourceName];
        }

        public bool Prototype { get; set; } = false;

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? BlockBeginIndexReference { get; set; } = null;
        public IndexReference? LastIndexReference { get; set; }


        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // interface instantiation can be placed only in module
            BuildingBlock buildingBlock = nameSpace.BuildingBlock as BuildingBlock;
            if (buildingBlock == null) return false;

            Project sourceProject = word.Project;

            var moduleIdentifier = word.CrateWordReference();
            string moduleName = word.Text;
            IndexReference beginIndexReference = word.CreateIndexReference();
            Module? instancedModule = word.ProjectProperty.GetBuildingBlock(moduleName) as Module;
            if (instancedModule == null)
            {
                if (word.GetNextComment().Contains("@project"))
                {
                    var comment = word.GetNextCommentScanner();
                    while (!comment.EOC)
                    {
                        if(comment.Text != "@project")
                        {
                            comment.MoveNext();
                            continue;
                        }
                        comment.Color(CodeDrawStyle.ColorType.HighLightedComment);
                        comment.MoveNext();
                        comment.Color(CodeDrawStyle.ColorType.HighLightedComment);
                        if (!CodeEditor2.Global.Projects.ContainsKey(comment.Text)) break;

                        sourceProject = CodeEditor2.Global.Projects[comment.Text];
                        instancedModule = ((ProjectProperty)sourceProject.GetPluginProperty()).GetBuildingBlock(moduleName) as Module;

                        break;
                    }
                    if(instancedModule == null)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            word.MoveNext();
            IndexReference blockBeginIndexReference = word.CreateIndexReference();
            

            string next = word.NextText;
            if(word.Text != "#" && next != "(" && next != ";" && General.IsIdentifier(word.Text))
            {
                moduleIdentifier.AddError("illegal module item");
                word.SkipToKeyword(";");
                return true;
            }
            moduleIdentifier.Color(CodeDrawStyle.ColorType.Keyword);
//            word.MoveNext();

            Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();

            if (word.Text == "#") // parameter
            {
                ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, instancedModule);
            }


            while (!word.Eof)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);


                if (!General.IsIdentifier(word.Text))
                {
                    if (word.Prototype) word.AddError("illegal instance name");
                    word.SkipToKeyword(";");
                }

                if(word.RootParsedDocument.Project==null) throw new Exception();

                ModuleInstantiation moduleInstantiation = new ModuleInstantiation()
                {
                    BeginIndexReference = beginIndexReference,
                    DefinitionReference = word.CrateWordReference(),
                    Name = word.Text,
                    Project = word.RootParsedDocument.Project,
                    SourceName = moduleName,
                    ParameterOverrides = parameterOverrides,
                    SourceProjectName = sourceProject.Name
                };
                moduleInstantiation.BlockBeginIndexReference = blockBeginIndexReference;

                // swap to parameter overrided module
                if (instancedModule != null)
                {
                    if (parameterOverrides.Count != 0)
                    {
                        instancedModule = word.ProjectProperty.GetInstancedBuildingBlock(moduleInstantiation) as Module;
                    }
                    if (instancedModule == null)
                    {
                        nameSpace.BuildingBlock.ReparseRequested = true;
                        word.AddWarning("not parsed yet.");
                    }
                }

//                moduleInstantiation.InstancedModule = instancedModule;

                if (word.Prototype)
                {
                    moduleInstantiation.Prototype = true;

                    if (moduleInstantiation.Name == null)
                    {
                        // 
                    }
                    //else if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    //{   // duplicated
                    //    word.AddPrototypeError("instance name duplicated");
                    //}
                    //else
                    //{
                    //    buildingBlock.NamedElements.Add(moduleInstantiation.Name, moduleInstantiation);
                    //}
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    {   // duplicated
                        word.AddPrototypeError("instance name duplicated");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(moduleInstantiation.Name, moduleInstantiation);
                    }
                }
                else
                {
                    if (moduleInstantiation.Name == null)
                    {
                        // 
                    }
                    //else if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    //{   // duplicated
                    //    if (((IBuildingBlockInstantiation)buildingBlock.NamedElements[moduleInstantiation.Name]).Prototype)
                    //    {
                    //        ModuleInstantiation? mod = buildingBlock.NamedElements[moduleInstantiation.Name] as ModuleInstantiation;
                    //        if (mod != null)
                    //        {
                    //            moduleInstantiation = mod;
                    //            moduleInstantiation.Prototype = false;
                    //        }
                    //    }
                    //    else
                    //    {
                    //    }
                    //}
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    {   // duplicated
                        if (((IBuildingBlockInstantiation)nameSpace.NamedElements[moduleInstantiation.Name]).Prototype)
                        {
                            ModuleInstantiation? mod = nameSpace.NamedElements[moduleInstantiation.Name] as ModuleInstantiation;
                            if (mod != null)
                            {
                                moduleInstantiation = mod;
                                moduleInstantiation.Prototype = false;
                            }
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        //module.ModuleInstantiations.Add(moduleInstantiation.Name, moduleInstantiation);
                    }
                }

                word.MoveNext();

                if (word.Text != "(")
                {
                    word.AddError("( expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }
                word.MoveNext();

                parseListOfPortConnections(word, nameSpace, instancedModule, moduleInstantiation, moduleIdentifier);

                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return true;
                }
                word.MoveNext();
                moduleInstantiation.LastIndexReference = word.CreateIndexReference();

                if (!word.Prototype && word.Active && moduleInstantiation.BlockBeginIndexReference != null)
                {
                    word.AppendBlock(moduleInstantiation.BlockBeginIndexReference, moduleInstantiation.LastIndexReference);
                }
                if (word.Text != ",") break;
                word.MoveNext();
            }


            if (word.Text != ";")
            {
                word.AddError("; expected");
                return true;
            }
            word.MoveNext();
            return true;
        }

        private static void parseListOfPortConnections(
            WordScanner word, 
            NameSpace nameSpace, 
            Module? instancedModule, 
            ModuleInstantiation moduleInstantiation, 
            WordReference moduleIdentifier)
        {
            /*
            list_of_port_connections ::= 
                  ordered_port_connection { "," ordered_port_connection }
                | named_port_connection { "," named_port_connection }
             */

            if (word.GetCharAt(0) == '.')
            { // named port assignment
                parseNamedPortConnections(word, nameSpace, instancedModule, moduleInstantiation, moduleIdentifier);
            }
            else
            { // ordered port assignment
                parseOrderedPortConnections(word, nameSpace, instancedModule, moduleInstantiation, moduleIdentifier);
            }
        }

        private static void parseOrderedPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Module? instancedModule,
            ModuleInstantiation moduleInstantiation,
            WordReference moduleIdentifier)
        {
            /*
            ordered_port_connection ::= { attribute_instance } [ expression ]
             */
            int i = 0;
            while (!word.Eof && word.Text != ")")
            {
                string pinName = "";
                if (instancedModule != null && i < instancedModule.PortsList.Count)
                {
                    pinName = instancedModule.PortsList[i].Name;
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (word.Prototype && expression != null && !moduleInstantiation.PortConnection.ContainsKey(pinName)) moduleInstantiation.PortConnection.Add(pinName, expression);
                }
                else
                {
                    word.AddError("illegal port connection");
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
            Module? instancedModule,
            ModuleInstantiation moduleInstantiation,
            WordReference moduleIdentifier)
        {
            /*
            named_port_connection ::= 
                  { attribute_instance } "." port_identifier [ "(" [ expression ] ")" ] 
                | { attribute_instance } ".*"
             
             */

            bool wildcardConnection = false;

            if (word.Eof) return;

            List<string> notWrittenPortName;
            if (instancedModule == null)
            {
                notWrittenPortName = new List<string>();
            }
            else
            {
                notWrittenPortName = instancedModule.Ports.Keys.ToList();
            }

            WordReference? wildcardRef = null;
            while (!word.Eof && word.Text == ".")
            {
                word.MoveNext();    // .
                if (word.Text == "*") // 23.3.2.4 Connecting module instances using wildcard named port connections ( .*)
                {
                    wildcardConnection = true;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    wildcardRef = word.GetReference();
                    word.MoveNext();
                    if (word.Text != ",")
                    {
                        break;
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    continue;
                }

                string pinName = word.Text;
                IndexReference startRef = word.CreateIndexReference();
                if (notWrittenPortName.Contains(pinName)) notWrittenPortName.Remove(pinName);
                WordReference pinReference = word.GetReference();
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (word.Prototype)
                {
                    if (moduleInstantiation.PortConnection.ContainsKey(pinName))
                    {
                        word.AddPrototypeError("duplicated");
                    }
                    word.MoveNext();
                }
                else
                {
                    if (instancedModule != null)
                    {
                        if (instancedModule.Ports.ContainsKey(pinName))
                        {

                        }
                        else
                        {
                            word.AddError("illegal port name");
                        }
                    }
                    word.MoveNext();

                    PortReference pRef = new PortReference(pinName, startRef, word.CreateIndexReference());
                    moduleInstantiation.PortReferences.Add(pRef);
                }

                if (word.Text == "(")
                {
                    parseNamedPortConnection(word, nameSpace, instancedModule, moduleInstantiation, pinName, moduleIdentifier);
                }
                else
                {
                    // 23.3.2.3 Connecting module instance using implicit named port connections
                    parseImplicitPortConnection(word, nameSpace, instancedModule, moduleInstantiation, pinName, pinReference, moduleIdentifier);
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

            if (notWrittenPortName.Count != 0)
            {
                if (wildcardConnection)
                {
                    parseWidCardPortConnection(word, nameSpace, instancedModule, moduleInstantiation, moduleIdentifier, notWrittenPortName,wildcardRef);
                }
                else
                {
                    moduleIdentifier.AddWarning("missing port " + notWrittenPortName[0]);
                }
            }

        }

        private static void parseWidCardPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Module? instancedModule,
            ModuleInstantiation moduleInstantiation,
            WordReference moduleIdentifier,
            List<string> notWrittenPortName,
            WordReference? wildcardRef
            )
        {
            if (instancedModule == null) return;
            if (wildcardRef == null) throw new Exception();

            foreach (string pinName in notWrittenPortName)
            {
                DataObject? targetObject = nameSpace.NamedElements.GetDataObject(pinName);
                if (targetObject == null)
                {
                    if(!word.Prototype) wildcardRef.ApplyRule(word.ProjectProperty.RuleSet.NotAllPortConnectedWithWildcardNamedPortConnections, "\nport :" + pinName);
                    continue;
                }
                Port port = instancedModule.Ports[pinName];

                Expressions.Expression? expression;

                expression = Expressions.DataObjectReference.Create(targetObject, nameSpace);
                if (port.Direction == Port.DirectionEnum.Output)
                {
                    targetObject.AssignedReferences.Add(wildcardRef);
                }
                else
                {
                    targetObject.UsedReferences.Add(wildcardRef);
                }

                if (targetObject.CommentAnnotation_Discarded)
                {
                    if (!word.Prototype) word.AddError("Disarded.");
                }

                connectPort(word, moduleInstantiation, instancedModule, pinName, expression);
            }
        }


        //23.3.2.2 Connecting module instance ports by name
        private static void parseNamedPortConnection(
        WordScanner word,
        NameSpace nameSpace,
        Module? instancedModule,
        ModuleInstantiation moduleInstantiation,
        string pinName,
        WordReference moduleIdentifier)
        {
            if (word.Text != "(") throw new Exception();
            word.MoveNext();

            bool outPort = false;
            if (instancedModule != null && instancedModule.Ports.ContainsKey(pinName))
            {
                if (instancedModule.Ports[pinName].Direction == DataObjects.Port.DirectionEnum.Output
                    || instancedModule.Ports[pinName].Direction == DataObjects.Port.DirectionEnum.Inout)
                {
                    outPort = true;
                }
            }


            if (word.Text == ")")
            {
                if (!outPort)
                {
                    word.AddWarning("floating input");
                }
                word.MoveNext();
                return;
            }


            Expressions.Expression? expression;
            if (outPort)
            {
                expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace,true);
            }
            else
            {
                expression = Expressions.Expression.ParseCreateAcceptImplicitNet(word, nameSpace,false);
            }

            if (expression != null)
            {
                connectPort(word, moduleInstantiation, instancedModule, pinName, expression);
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError(") expected");
                }
            }
            else
            {
                word.AddError("illegal port connection");
                if (word.Text == ")")
                {
                    word.MoveNext();
                    return;
                }
                while (true)
                {
                    if (new List<string> { "endmodule", "endtask", "end", "endinterface", "endfunction" }.Contains(word.Text)) return;
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                        return;
                    }
                    word.MoveNext();
                }
            }

        }

        // 23.3.2.3 Connecting module instance using implicit named port connections
        private static void parseImplicitPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Module? instancedModule,
            ModuleInstantiation moduleInstantiation,
            string pinName,
            WordReference pinReference,
            WordReference moduleIdentifier)
        {
            if (instancedModule == null) return;
            DataObject? targetObject = nameSpace.NamedElements.GetDataObject(pinName);

            if (targetObject == null)
            {
                word.AddError("illegal port connection");
                return;
            }
            if (!instancedModule.Ports.ContainsKey(pinName))
            {
                word.AddError("illegal port connection : "+pinName);
                return;
            }
            Port port = instancedModule.Ports[pinName];

            Expressions.Expression? expression;

            expression = Expressions.DataObjectReference.Create(targetObject, nameSpace);
            if(port.Direction == Port.DirectionEnum.Output)
            {
                targetObject.AssignedReferences.Add(pinReference);
            }
            else
            {
                targetObject.UsedReferences.Add(pinReference);
            }

            if (targetObject.CommentAnnotation_Discarded)
            {
                word.AddError("Disarded.");
            }

            connectPort(word, moduleInstantiation, instancedModule, pinName, expression);
       }
        private static void connectPort(
            WordScanner word,
            ModuleInstantiation moduleInstantiation,
            Module? instancedModule,
            string pinName,
            Expressions.Expression expression
        )
        {
            bool outPort = false;
            if (instancedModule != null && instancedModule.Ports.ContainsKey(pinName))
            {
                if (instancedModule.Ports[pinName].Direction == DataObjects.Port.DirectionEnum.Output
                    || instancedModule.Ports[pinName].Direction == DataObjects.Port.DirectionEnum.Inout)
                {
                    outPort = true;
                }
            }

            if (outPort)
            {
                if (word.Prototype && expression != null && !moduleInstantiation.PortConnection.ContainsKey(pinName))
                {
                    moduleInstantiation.PortConnection.Add(pinName, expression);
                }
                if (!word.Prototype) checkPortConnection(word.ProjectProperty, instancedModule, expression, pinName, true);
            }
            else
            {
                if (word.Prototype && expression != null && !moduleInstantiation.PortConnection.ContainsKey(pinName))
                {
                    moduleInstantiation.PortConnection.Add(pinName, expression);
                }
                if (!word.Prototype) checkPortConnection(word.ProjectProperty, instancedModule, expression, pinName, false);
            }
        }


        //private static void checkOutputPortConnection(Module? instancedModule, Expressions.Expression? expression, string pinName)
        //{
        //    if (instancedModule != null && expression != null && expression.BitWidth != null && instancedModule.Ports.ContainsKey(pinName))
        //    {
        //        if (instancedModule.Ports[pinName].Range == null)
        //        {
        //            if (expression.BitWidth != null && expression.Reference != null && expression.BitWidth != 1)
        //            {
        //                expression.Reference.AddWarning("bit width mismatch 1 vs " + expression.BitWidth);
        //            }

        //        }
        //        else if (instancedModule.Ports[pinName].Range.BitWidth != expression.BitWidth && expression.Reference != null)
        //        {
        //            expression.Reference.AddWarning("bit width mismatch " + instancedModule.Ports[pinName].Range.BitWidth + " vs " + expression.BitWidth);
        //        }
        //    }
        //}

        private static void checkPortConnection(ProjectProperty projectProperty,Module? instancedModule,Expressions.Expression? expression,string pinName,bool output)
        {
            if (instancedModule == null) return;
            if (expression == null) return;
            if (!instancedModule.Ports.ContainsKey(pinName)) return;
            Port? port = instancedModule.Ports[pinName];
            if (port == null) throw new Exception();

            DataObject? portDataObject = instancedModule.Ports[pinName].DataObject;

            if(portDataObject is ModportInstance)
            {

                // connect to modport
                ModportInstance? modportInstantiation = instancedModule.Ports[pinName].DataObject as ModportInstance;
                if (modportInstantiation == null) throw new Exception();
                string interfaceName = modportInstantiation.InterfaceName;
                Interface? interface_ = instancedModule.Project.GetPluginProperty().GetBuildingBlock(interfaceName) as Interface;
                if (interface_ == null)
                {
                    expression.Reference.AddError("illegal interface");
                }
                else
                {
                    checkModPortConnection(projectProperty, expression, modportInstantiation, interface_, pinName, output);
                }
            }
            else if (portDataObject is InterfaceInstance)
            {
                InterfaceInstance? portInterfaceInstantiation = instancedModule.Ports[pinName].DataObject as InterfaceInstance;
                if (portInterfaceInstantiation == null) throw new Exception();
                Interface? interface_ = instancedModule.Project.GetPluginProperty().GetBuildingBlock(portInterfaceInstantiation.SourceName) as Interface;
                if(interface_ == null)
                {
                    expression.Reference.AddError("illegal interface");
                }
                else
                {
                    checkInterfacePortConnection(projectProperty, expression, portInterfaceInstantiation, interface_, pinName, output);
                }
            }
            else if( portDataObject is Variable)
            {
                Variable? variable = portDataObject as Variable;
                if (variable == null) throw new Exception();

                checkVariablePortConnection(projectProperty, expression, port, variable, pinName, output);
            }
            else if(portDataObject is DataObjects.Nets.Net)
            {
                DataObjects.Nets.Net? net = portDataObject as DataObjects.Nets.Net;
                if (net == null) throw new Exception();

                checkNetPortConnection(projectProperty, expression, port, net, pinName, output);
            }

        }
        private static void checkNetPortConnection(
            ProjectProperty projectProperty,
            Expressions.Expression expression,
            Port port,
            DataObjects.Nets.Net net,
            string pinName,
            bool output
            )
        {
            if (port.Range == null)
            {
                if (expression.BitWidth != null && expression.Reference != null && expression.BitWidth != 1)
                {
                    expression.Reference.AddWarning("bit width mismatch 1 <- " + expression.BitWidth);
                }
            }
            else if (port.Range.Size != expression.BitWidth && expression.Reference != null)
            {
                expression.Reference.AddWarning("bit width mismatch " + port.Range.Size + " <- " + expression.BitWidth);
            }
        }
        private static void checkVariablePortConnection(
            ProjectProperty projectProperty,
            Expressions.Expression expression,
            Port port,
            Variable variable,
            string pinName, 
            bool output
            )
        {
            if (port.Range == null)
            {
                if (expression.BitWidth != null && expression.Reference != null && expression.BitWidth != 1)
                {
                    expression.Reference.AddWarning("bit width mismatch 1 <- " + expression.BitWidth);
                }

            }
            else if (port.Range.Size != expression.BitWidth && expression.Reference != null)
            {
                expression.Reference.AddWarning("bit width mismatch " + port.Range.Size + " <- " + expression.BitWidth);
            }
        }
        private static void checkInterfacePortConnection(
            ProjectProperty projectProperty,
            Expressions.Expression expression,
            InterfaceInstance portInterfaceInstantiation,
            Interface interface_,
            string pinName,
            bool output
            )
        {
            Expressions.DataObjectReference? variableReference = expression as Expressions.DataObjectReference;
            if(variableReference == null)
            {
                expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                return;
            }

            InterfaceInstance? interfaceInstance = variableReference.DataObject as InterfaceInstance;
            if(interfaceInstance == null)
            {
                expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                return;
            }

            if(interfaceInstance.SourceName != portInterfaceInstantiation.SourceName)
            {
                expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                return;
            }
            // properly connected
        }
        private static void checkModPortConnection(
            ProjectProperty projectProperty,
            Expressions.Expression expression,
            ModportInstance modportInstantiation,
            Interface interface_,
            string pinName,
            bool output
            )
        {
            Expressions.DataObjectReference? variableReference = expression as Expressions.DataObjectReference;
            if (variableReference == null)
            {
                expression.Reference.AddError("should be " + interface_.Name+"."+ modportInstantiation.ModportName);
                return;
            }

            ModportInstance? modportInstance = variableReference.DataObject as ModportInstance;
            if (modportInstance == null)
            {
                expression.Reference.ApplyRule(projectProperty.RuleSet.ImplicitModportInterfaceConnectionToInstance,
                    "\nshould be " + interface_.Name + "." + modportInstantiation.ModportName
                    );
                return;
            }

            if(( interface_.Name != modportInstantiation.InterfaceName ) | (modportInstance.ModportName != modportInstantiation.ModportName))
            {
                expression.Reference.AddError("should be " + interface_.Name + "." + modportInstantiation.ModportName);
                return;
            }
            // properly connected
        }
        private static void checkDataObjectPortConnection(
            Module? instancedModule, Expressions.Expression? expression, string pinName, bool output)
        {
        }



        public string? CreateString()
        {
            return CreateString("\t");

        }

        public string ParameterId
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(SourceName);
                sb.Append(":");
                foreach (var kvp in ParameterOverrides)
                {
                    sb.Append(kvp.Key);
                    sb.Append("=");
                    sb.Append(kvp.Value.Value.ToString());
                    sb.Append(",");
                }
                return sb.ToString();
            }
        }
        public string? CreateString(string indent)
        {
            Module? instancedModule = GetInstancedBuildingBlock() as Module;
            if (instancedModule == null) return null;

            StringBuilder sb = new StringBuilder();
            bool first;

            sb.Append(SourceName);
            sb.Append(" ");

            if(instancedModule.PortParameterNameList.Count != 0)
            {
                sb.Append("#(\r\n");

                first = true;
                foreach(var paramName in instancedModule.PortParameterNameList)
                {
                    if (!first) sb.Append(",\r\n");
                    sb.Append(indent);
                    sb.Append(".");
                    sb.Append(paramName);
                    sb.Append("\t( ");
                    if (ParameterOverrides.ContainsKey(paramName))
                    {
                        sb.Append(ParameterOverrides[paramName].CreateString());
                    }
                    else
                    {
                        if(
                            instancedModule.NamedElements.ContainsKey(paramName) &&
                            instancedModule.NamedElements[paramName] is DataObjects.Constants.Constants &&
                            ((DataObjects.Constants.Constants)instancedModule.NamedElements[paramName]).Expression != null
                            )
                        {
                            sb.Append(((DataObjects.Constants.Constants)instancedModule.NamedElements[paramName]).Expression.CreateString());
                        }
                    }
                    sb.Append(" )");
                    first = false;
                }
                sb.Append("\r\n) ");
            }

            sb.Append(Name);
            sb.Append(" (\r\n");

            first = true;
            string? portGroup = null;
            foreach (var port in instancedModule.Ports.Values)
            {
                if (!first) sb.Append(",\r\n");

                if(port.PortGroupName != portGroup && portGroup != "")
                {
                    portGroup = port.PortGroupName;
                    sb.Append("// ");
                    sb.Append(portGroup);
                    sb.Append("\r\n");
                }
                sb.Append(indent);
                sb.Append(".");
                sb.Append(port.Name);
                sb.Append("\t");
                sb.Append("( ");
                if (PortConnection.ContainsKey(port.Name))
                {
                    sb.Append(PortConnection[port.Name].CreateString());
                }
                sb.Append(" )");
                first = false;
            }
            sb.Append("\r\n);");


            return sb.ToString();
        }

        /*
        module_instantiation            ::= module_identifier [ parameter_value_assignment ] module_instance { , module_instance } ;
        parameter_value_assignment      ::= # ( list_of_parameter_assignments )  
        list_of_parameter_assignments   ::= ordered_parameter_assignment { , ordered_parameter_assignment } | named_parameter_assignment { , named_parameter_assignment }  
        ordered_parameter_assignment    ::= expression  named_parameter_assignment ::= .parameter_identifier ( [ expression ] ) 
        module_instance                 ::= name_of_instance ( [ list_of_port_connections ] )
        name_of_instance                ::= module_instance_identifier [ range ]  
        list_of_port_connections        ::= ordered_port_connection { , ordered_port_connection }          | named_port_connection { , named_port_connection }  
        ordered_port_connection         ::= { attribute_instance } [ expression ]
        named_port_connection           ::= { attribute_instance } .port_identifier ( [ expression ] )  
         */
    }
}
