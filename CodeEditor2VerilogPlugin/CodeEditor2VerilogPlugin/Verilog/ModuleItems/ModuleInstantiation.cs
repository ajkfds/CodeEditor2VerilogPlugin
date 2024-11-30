using Microsoft.CodeAnalysis.CSharp.Syntax;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
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

        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        protected ModuleInstantiation() { }
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
        public void AppendLabel(IndexReference iref,AjkAvaloniaLibs.Contorls.ColorLabel label)
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
                if (ParameterOverrides.Count == 0) return "";
                StringBuilder sb = new StringBuilder();
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

        public bool Prototype { get; set; } = false;

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; }


        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // interface instantiation can be placed only in module
            BuildingBlock buildingBlock = nameSpace.BuildingBlock as BuildingBlock;
            if (buildingBlock == null) return false;

            WordScanner moduleIdentifier = word.Clone();
            string moduleName = word.Text;
            IndexReference beginIndexReference = word.CreateIndexReference();
            Module? instancedModule = word.ProjectProperty.GetBuildingBlock(moduleName) as Module;
            if (instancedModule == null)
            {
                return false;
            }
            word.MoveNext();

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
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text != "(")
                {
                    word.AddError("( expected");
                    word.SkipToKeyword(";");
                    return true;
                }
                word.MoveNext();

                if (word.Text == ".")
                { // named parameter assignment
                    while (!word.Eof && word.Text == ".")
                    {
                        bool error = false;
                        word.MoveNext();
                        word.Color(CodeDrawStyle.ColorType.Parameter);
                        string paramName = word.Text;
                        if (instancedModule != null && !instancedModule.PortParameterNameList.Contains(paramName)){
                            word.AddError("illegal parameter name");
                            error = true;
                        }
                        word.MoveNext();

                        if (word.Text != "(")
                        {
                            word.AddError("( expected");
                        }
                        else
                        {
                            word.MoveNext();
                        }
                        Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if(expression == null)
                        {
                            error = true;
                        }else if (!expression.Constant)
                        {
                            word.AddError("port parameter should be constant");
                            error = true;
                        }

                        if (!error )//& word.Prototype)
                        {
                            if (parameterOverrides.ContainsKey(paramName))
                            {
                                word.AddPrototypeError("duplicated");
                            }
                            else
                            {
                                if(expression != null) parameterOverrides.Add(paramName, expression);
                            }
                        }

                        if (word.Text != ")")
                        {
                            word.AddError(") expected");
                        }
                        else
                        {
                            word.MoveNext();
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
                else
                { // ordered parameter assignment
                    int i = 0;
                    while (!word.Eof && word.Text != ")")
                    {
                        Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if(instancedModule != null)
                        {
                            if (i >= instancedModule.PortParameterNameList.Count)
                            {
                                word.AddError("too many parameters");
                            }
                            else
                            {
                                string paramName = instancedModule.PortParameterNameList[i];
                                if (word.Prototype && expression != null)
                                {
                                    if (parameterOverrides.ContainsKey(paramName))
                                    {
                                        word.AddError("duplicated");
                                    }
                                    else
                                    {
                                       parameterOverrides.Add(paramName, expression);
                                    }
                                }
                            }

                        }
                        i++;
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

                if (word.Text != ")")
                {
                    word.AddError("( expected");
                    return true;
                }
                word.MoveNext();
            }


            while (!word.Eof)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);


                if (!General.IsIdentifier(word.Text))
                {
                    if (word.Prototype) word.AddError("illegal instance name");
                    word.SkipToKeyword(";");
                }

                ModuleInstantiation moduleInstantiation = new ModuleInstantiation()
                {
                    BeginIndexReference = beginIndexReference,
                    DefinitionReference = word.CrateWordReference(),
                    Name = word.Text,
                    Project = word.RootParsedDocument.Project,
                    SourceName = moduleName,
                    ParameterOverrides = parameterOverrides
                };

                // swap to parameter overrided module
                if (instancedModule != null)
                {
                    if (parameterOverrides.Count != 0)
                    {
                        instancedModule = word.ProjectProperty.GetInstancedBuildingBlock(moduleInstantiation) as Module;
                    }
                    if (instancedModule == null) nameSpace.BuildingBlock.ReparseRequested = true;
                }

                if (word.Prototype)
                {
                    moduleInstantiation.Prototype = true;

                    if (moduleInstantiation.Name == null)
                    {
                        // 
                    }
                    else if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    {   // duplicated
                        word.AddPrototypeError("instance name duplicated");
                    }
                    else
                    {
                        buildingBlock.NamedElements.Add(moduleInstantiation.Name, moduleInstantiation);
                    }
                }
                else
                {
                    if (moduleInstantiation.Name == null)
                    {
                        // 
                    }
                    else if (buildingBlock.NamedElements.ContainsIBuldingBlockInstantiation(moduleInstantiation.Name))
                    {   // duplicated
                        if (((IBuildingBlockInstantiation)buildingBlock.NamedElements[moduleInstantiation.Name]).Prototype)
                        {
                            ModuleInstantiation? mod = buildingBlock.NamedElements[moduleInstantiation.Name] as ModuleInstantiation;
                            if(mod != null)
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

                if (!word.Prototype && word.Active) word.AppendBlock(moduleInstantiation.BeginIndexReference , moduleInstantiation.LastIndexReference);
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
            WordScanner moduleIdentifier)
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
            WordScanner moduleIdentifier)
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
            WordScanner moduleIdentifier)
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


            while (!word.Eof && word.Text == ".")
            {
                word.MoveNext();    // .
                if (word.Text == "*") // 23.3.2.4 Connecting module instances using wildcard named port connections ( .*)
                {
                    wildcardConnection = true;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
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

                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (word.Prototype)
                {
                    if (moduleInstantiation.PortConnection.ContainsKey(pinName))
                    {
                        word.AddError("duplicated");
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
                    parseImplicitPortConnection(word, nameSpace, instancedModule, moduleInstantiation, pinName, moduleIdentifier);
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
                    parseWidCardPortConnection(word, nameSpace, instancedModule, moduleInstantiation, moduleIdentifier, notWrittenPortName);
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
            WordScanner moduleIdentifier,
            List<string> notWrittenPortName
            )
        {
            if (instancedModule == null) return;

            foreach (string pinName in notWrittenPortName)
            {
                DataObject? targetObject = nameSpace.NamedElements.GetDataObject(pinName);
                if (targetObject == null) continue;
                Port port = instancedModule.Ports[pinName];
                Variable? variable = targetObject as Variable;

                Expressions.Expression? expression;
                if (variable != null)
                {
                    expression = Expressions.VariableReference.Create(variable, nameSpace);// Expressions.Expression.CreateTempExpression(pinName);
                }
                else
                {
                    expression = Expressions.Expression.CreateTempExpression(pinName);
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
        WordScanner moduleIdentifier)
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

            Expressions.Expression? expression;
            if (outPort)
            {
                expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace);
            }
            else
            {
                expression = Expressions.Expression.ParseCreate(word, nameSpace);
            }

            if(expression != null) connectPort(word, moduleInstantiation, instancedModule, pinName, expression);

            if (word.Text == ")")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError(") expected");
            }
        }

        // 23.3.2.3 Connecting module instance using implicit named port connections
        private static void parseImplicitPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Module? instancedModule,
            ModuleInstantiation moduleInstantiation,
            string pinName,
            WordScanner moduleIdentifier)
        {
            if (instancedModule == null) return;
            DataObject? targetObject = nameSpace.NamedElements.GetDataObject(pinName);

            if (targetObject == null)
            {
                word.AddError("illegal port connection");
                return;
            }

            Port port = instancedModule.Ports[pinName];
            Variable? variable = targetObject as Variable;

            Expressions.Expression? expression;
            if (variable != null)
            {
                expression = Expressions.VariableReference.Create(variable, nameSpace);// Expressions.Expression.CreateTempExpression(pinName);
            }
            else
            {
                expression = Expressions.Expression.CreateTempExpression(pinName);
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
                if (!word.Prototype) checkPortConnection(instancedModule, expression, pinName, true);
            }
            else
            {
                if (word.Prototype && expression != null && !moduleInstantiation.PortConnection.ContainsKey(pinName))
                {
                    moduleInstantiation.PortConnection.Add(pinName, expression);
                }
                if (!word.Prototype) checkPortConnection(instancedModule, expression, pinName, false);
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

        private static void checkPortConnection(Module? instancedModule,Expressions.Expression? expression,string pinName,bool output)
        {
            if (instancedModule == null) return;
            if (expression == null) return;
            if (!instancedModule.Ports.ContainsKey(pinName)) return;
            Port? port = instancedModule.Ports[pinName];
            if (port == null) throw new Exception();

            DataObject? portDataObject = instancedModule.Ports[pinName].DataObject;

            if (portDataObject is InterfaceInstance)
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
                    checkInterfacePortConnection(expression, portInterfaceInstantiation, interface_, pinName, output);
                }
            }
            else if( portDataObject is Variable)
            {
                Variable? variable = portDataObject as Variable;
                if (variable == null) throw new Exception();

                checkVariablePortConnection( expression, port, variable, pinName, output);
            }
            else if(portDataObject is DataObjects.Nets.Net)
            {
                DataObjects.Nets.Net? net = portDataObject as DataObjects.Nets.Net;
                if (net == null) throw new Exception();

                checkNetPortConnection(expression, port, net, pinName, output);
            }

        }
        private static void checkNetPortConnection(
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
            Expressions.Expression expression,
            InterfaceInstance portInterfaceInstantiation,
            Interface interface_,
            string pinName,
            bool output
            )
        {
//            if (portInterfaceInstantiation.ModPortName == "")
            { // interface
                Expressions.InterfaceReference? expressionInterface = expression as Expressions.InterfaceReference;
                if (expressionInterface == null)
                {
                    expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                }
                else
                {
                    if (expressionInterface == null)
                    {
                        expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                    }
                    else if (expressionInterface.interfaceInstantiation.SourceName != portInterfaceInstantiation.SourceName)
                    {
                        expression.Reference.AddError("should be " + portInterfaceInstantiation.SourceName);
                    }
                    else
                    {
                        // properly connected
                    }
                }
            }
            //else
            //{ // modport
            //}
        }
        private static void checkDataObjectPortConnection(
            Module? instancedModule, Expressions.Expression? expression, string pinName, bool output)
        {
        }

        public BuildingBlock? GetInstancedBuildingBlock()
        {
            BuildingBlock? instancedModule = ProjectProperty.GetBuildingBlock(SourceName);

            if (ParameterOverrides.Count != 0)
            {
                instancedModule = ProjectProperty.GetInstancedBuildingBlock(this);
            }

            return instancedModule;
        }


        public string? CreateString()
        {
            return CreateString("\t");

        }
        public string? CreateString(string indent)
        {
            Module? instancedModule = ProjectProperty.GetBuildingBlock(SourceName) as Module;
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
            string? sectionName = null;
            foreach (var port in instancedModule.Ports.Values)
            {
                if (!first) sb.Append(",\r\n");

                if(port.SectionName != sectionName && sectionName != "")
                {
                    sectionName = port.SectionName;
                    sb.Append("// ");
                    sb.Append(sectionName);
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
