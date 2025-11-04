using CodeEditor2.Data;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    public class InterfaceInstance : DataObject,IBuildingBlockInstantiation,INamedElement
    {
        
        protected InterfaceInstance() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public Attribute? Attribute { get; set; }
        public required WordReference DefinitionReference { get; init; }

        [Newtonsoft.Json.JsonIgnore]
        public required NameSpace InstancedNameSpace { get; init; }

        [Newtonsoft.Json.JsonIgnore]
        public required Project Project { get; init; }

        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        [Newtonsoft.Json.JsonIgnore]
        public ProjectProperty ProjectProperty
        {
            get
            {
                ProjectProperty? projectProperty = Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectProperty == null) throw new Exception();
                return projectProperty;
            }
        }

        public required string SourceName { get; init; }
//        public string? ModPortName { get; set; }
        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }
        public Dictionary<string, Expressions.Expression> PortConnection { get; set; } = new Dictionary<string, Expressions.Expression>();

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference BlockBeginIndexReference { get; set; }
        public IndexReference? LastIndexReference { get; set; }
        public void AppendLabel(IndexReference iref, AjkAvaloniaLibs.Controls.ColorLabel label)
        {
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

        //public static InterfaceInstantiation Create(string name, string sourceName, Project project)
        //{
        //    InterfaceInstantiation instantiation = new InterfaceInstantiation() { }
        //    instantiation.Name = name;
        //    instantiation.SourceName = sourceName;
        //    instantiation.Project = project;
        //    return instantiation;
        //}
        //public static DataObject Create(string name, DataTypes.IDataType dataType)
        //{
        //    if (dataType is not BuildingBlocks.Interface) throw new Exception();
        //    BuildingBlocks.Interface interface_ = (BuildingBlocks.Interface)dataType;

        //    InterfaceInstance interfaceInstantiation = new InterfaceInstance()
        //    {
        //        BeginIndexReference = interface_.BeginIndexReference,
        //        DefinitionReference = interface_.DefinitionReference,
        //        Name = name,
        //        ParameterOverrides = new Dictionary<string, Expressions.Expression>(),
        //        Project = null,
        //        SourceName = interface_.Name
        //    };
        //    if (interface_ == null) return interfaceInstantiation;
        //    //foreach (var modPort in interface_.NamedElements.Values.OfType<ModPort>())
        //    //{
        //    //    interface_.ModPorts.cl
        //    //}
        //    copyItems(interfaceInstantiation, interface_);

        //    return interfaceInstantiation;
        //}
        public static InterfaceInstance CreatePortInstance(WordScanner word,string sourceInterfaceName,NameSpace nameSpace)
        {
            InterfaceInstance interfaceInstantiation = new InterfaceInstance() {
                BeginIndexReference = word.CreateIndexReference(),
                DefinitionReference = word.CrateWordReference(),
                Name = word.Text,
                ParameterOverrides = new Dictionary<string, Expressions.Expression>(),
                Project = word.Project,
                SourceName = sourceInterfaceName,
                InstancedNameSpace = nameSpace
            };
            Interface? interface_ = interfaceInstantiation.Interface;
            if (interface_ == null) return interfaceInstantiation;
            //foreach (var modPort in interface_.NamedElements.Values.OfType<ModPort>())
            //{
            //    interface_.ModPorts.cl
            //}
            copyItems(interfaceInstantiation, interface_);

            return interfaceInstantiation;
        }

        public Interface? Interface { 
            get
            {
                Interface? instancedInterface;
                BuildingBlock buildingBlock = InstancedNameSpace.BuildingBlock.SearchBuildingBlockUpward(SourceName);
                if(buildingBlock is Interface)
                {
                    instancedInterface = (Interface)buildingBlock;
                    return instancedInterface;
                }

                ProjectProperty projectProperty = (ProjectProperty)Project.ProjectProperties[Plugin.StaticID];
                instancedInterface = projectProperty.GetBuildingBlock(SourceName) as Interface;
                return instancedInterface;
            } 
        }

        /*
        interface_instantiation  ::= interface_identifier [ parameter_value_assignment ] hierarchical_instance { , hierarchical_instance } ;
        parameter_value_assignment ::= # ( [ list_of_parameter_assignments ] )
        hierarchical_instance ::= name_of_instance ( [ list_of_port_connections ] )
         */
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // interface instantiation can be placed only in module,or interface
            IModuleOrInterface? moduleOrInterface = nameSpace.BuildingBlock as IModuleOrInterface;
            if (moduleOrInterface == null) return false;

            WordReference moduleIdentifier = word.CrateWordReference();
            string interfaceName = word.Text;
            IndexReference beginIndexReference = word.CreateIndexReference();
            Interface? instancedInterface = word.ProjectProperty.GetBuildingBlock(interfaceName) as Interface;
            if (instancedInterface == null)
            {
                return false;
            }
            word.MoveNext();
            IndexReference blockBeginIndexReference = word.CreateIndexReference();

            string next = word.NextText;
            if (word.Text != "#" && next != "(" && next != ";" && General.IsIdentifier(word.Text))
            {
                moduleIdentifier.AddError("illegal module item");
                word.SkipToKeyword(";");
                return true;
            }
            moduleIdentifier.Color(CodeDrawStyle.ColorType.Keyword);

            Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();

            if (word.Text == "#") // parse parameter override
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
                        if (instancedInterface != null && !instancedInterface.PortParameterNameList.Contains(paramName))
                        {
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
                        if (expression == null)
                        {
                            error = true;
                        }
                        else if (!expression.Constant)
                        {
                            word.AddError("port parameter should be constant");
                            error = true;
                        }

                        if (!error)//& word.Prototype)
                        {
                            if (parameterOverrides.ContainsKey(paramName))
                            {
                                word.AddPrototypeError("duplicated");
                            }
                            else
                            {
                                parameterOverrides.Add(paramName, expression);
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
                { // ordered paramater assignment
                    int i = 0;
                    while (!word.Eof && word.Text != ")")
                    {
                        Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (instancedInterface != null)
                        {
                            if (i >= instancedInterface.PortParameterNameList.Count)
                            {
                                word.AddError("too many parameters");
                            }
                            else
                            {
                                string paramName = instancedInterface.PortParameterNameList[i];
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

                word.Color(CodeDrawStyle.ColorType.Variable);
                if (!General.IsIdentifier(word.Text))
                {
                    if (word.Prototype) word.AddError("illegal instance name");
                    word.SkipToKeyword("");
                    return false;
                }

                InterfaceInstance interfaceInstance = new InterfaceInstance()
                {
                    BeginIndexReference = beginIndexReference,
                    DefinitionReference = word.CrateWordReference(),
                    Name = word.Text,
                    ParameterOverrides = parameterOverrides,
                    Project = word.RootParsedDocument.Project,
                    SourceName = interfaceName,
                    InstancedNameSpace = nameSpace
                };
                interfaceInstance.BlockBeginIndexReference = blockBeginIndexReference;

                // swap to parameter overrided module
                if (instancedInterface != null)
                {
                    if (parameterOverrides.Count != 0)
                    {
                        instancedInterface = word.ProjectProperty.GetInstancedBuildingBlock(interfaceInstance) as Interface;
                    }
                    if (instancedInterface == null) nameSpace.BuildingBlock.ReparseRequested = true;
                }

                // register to upper bulding block
                if (word.Prototype)
                {
                    interfaceInstance.Prototype = true;

                    if (interfaceInstance.Name == null)
                    {
                        // 
                    }
                    else if (moduleOrInterface.NamedElements.ContainsIBuldingBlockInstantiation(interfaceInstance.Name))
                    {   // duplicated
                        word.AddPrototypeError("instance name duplicated");
                    }
                    else
                    {
                        moduleOrInterface.NamedElements.Add(interfaceInstance.Name, interfaceInstance);
                    }
                }
                else
                {
                    if (interfaceInstance.Name == null)
                    {
                        // 
                    }
                    else if (moduleOrInterface.NamedElements.ContainsIBuldingBlockInstantiation(interfaceInstance.Name))
                    {   // duplicated
                        if (((IBuildingBlockInstantiation)moduleOrInterface.NamedElements[interfaceInstance.Name]).Prototype)
                        {
                            interfaceInstance = moduleOrInterface.NamedElements[interfaceInstance.Name] as InterfaceInstance;
                            interfaceInstance.Prototype = false;
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

                if (word.GetCharAt(0) == '.')
                { // named parameter assignment
                    while (!word.Eof && word.Text == ".")
                    {
                        word.MoveNext();
                        string pinName = word.Text;
                        bool outPort = false;
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        if (instancedInterface != null && !word.Prototype)
                        {
                            if (instancedInterface.Ports.ContainsKey(pinName))
                            {
                                if (instancedInterface.Ports[pinName].Direction == Port.DirectionEnum.Output
                                    || instancedInterface.Ports[pinName].Direction == Port.DirectionEnum.Inout)
                                {
                                    outPort = true;
                                }
                            }
                            else
                            {
                                word.AddError("illegal port name");
                            }
                        }
                        if (word.Prototype && interfaceInstance.PortConnection.ContainsKey(pinName))
                        {
                            word.AddError("duplicated");
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
                        if (outPort)
                        {
                            Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace,false);
                            if (word.Prototype && expression != null && !interfaceInstance.PortConnection.ContainsKey(pinName)) interfaceInstance.PortConnection.Add(pinName, expression);

                            if (!word.Prototype)
                            {
                                if (instancedInterface != null && expression != null && expression.BitWidth != null && instancedInterface.Ports.ContainsKey(pinName))
                                {
                                    if (instancedInterface.Ports[pinName].Range == null)
                                    {
                                        if (expression.BitWidth != null && expression.Reference != null && expression.BitWidth != 1)
                                        {
                                            expression.Reference.AddWarning("bitwidth mismatch 1 vs " + expression.BitWidth);
                                        }

                                    }
                                    else if (instancedInterface.Ports[pinName].Range.Size != expression.BitWidth && expression.Reference != null)
                                    {
                                        expression.Reference.AddWarning("bitwidth mismatch " + instancedInterface.Ports[pinName].Range.Size + " vs " + expression.BitWidth);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (word.Prototype && expression != null && !interfaceInstance.PortConnection.ContainsKey(pinName)) interfaceInstance.PortConnection.Add(pinName, expression);

                            if (!word.Prototype)
                            {
                                if (instancedInterface != null && expression != null && expression.BitWidth != null && instancedInterface.Ports.ContainsKey(pinName))
                                {
                                    if (instancedInterface.Ports[pinName].Range == null)
                                    {
                                        if (expression.BitWidth != null && expression.Reference != null && expression.BitWidth != 1)
                                        {
                                            expression.Reference.AddWarning("bitwidth mismatch 1 vs " + expression.BitWidth);
                                        }

                                    }
                                    else if (instancedInterface.Ports[pinName].Range.Size != expression.BitWidth && expression.Reference != null)
                                    {
                                        expression.Reference.AddWarning("bitwidth mismatch " + instancedInterface.Ports[pinName].Range.Size + " vs " + expression.BitWidth);
                                    }
                                }
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
                { // ordered paramater assignment
                    int i = 0;
                    while (!word.Eof && word.Text != ")")
                    {
                        string pinName = "";
                        if (instancedInterface != null && i < instancedInterface.PortsList.Count)
                        {
                            pinName = instancedInterface.PortsList[i].Name;
                            Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (word.Prototype && expression != null && !interfaceInstance.PortConnection.ContainsKey(pinName)) interfaceInstance.PortConnection.Add(pinName, expression);
                        }
                        else
                        {
                            word.AddError("illegal port connection");
                            Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
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
                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return true;
                }
                word.MoveNext();
                interfaceInstance.LastIndexReference = word.CreateIndexReference();

                if (!word.Prototype && word.Active && interfaceInstance.BlockBeginIndexReference != null) {
                    word.AppendBlock(interfaceInstance.BlockBeginIndexReference, interfaceInstance.LastIndexReference);
                }

                // copy items from interface
                copyItems(interfaceInstance,instancedInterface);

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

        private static void copyItems(InterfaceInstance interfaceInstance, Interface? instancedInterface)
        {
            // copy items from interface
            if (instancedInterface != null)
            {
                foreach (var dataObject in instancedInterface.NamedElements.Values.OfType<DataObject>())
                {
                    interfaceInstance.NamedElements.Add(dataObject.Name, dataObject.Clone());
                }
                foreach (var modPort in instancedInterface.NamedElements.Values.OfType<ModPort>() )
                {
                    interfaceInstance.NamedElements.Add(modPort.Name, ModportInstance.Create(modPort.Name,instancedInterface,modPort));
                }
            }
        }

        public BuildingBlock? GetInstancedBuildingBlock()
        {
            BuildingBlock? instancedModule;
            {
                BuildingBlock? buildingBlock = InstancedNameSpace.BuildingBlock.SearchBuildingBlockUpward(SourceName);
                if(buildingBlock != null)
                {
                    instancedModule = buildingBlock;
                }
                else
                {
                    instancedModule = ProjectProperty.GetBuildingBlock(SourceName);
                }
            }

            if (ParameterOverrides.Count != 0)
            {
                instancedModule = ProjectProperty.GetInstancedBuildingBlock(this);
            }

            return instancedModule;
        }


        public string CreateString()
        {
            return CreateString("\t");

        }
        public string CreateString(string indent)
        {
            Interface instancedModule = ProjectProperty.GetBuildingBlock(SourceName) as Interface;
            if (instancedModule == null) return null;

            StringBuilder sb = new StringBuilder();
            bool first;

            sb.Append(SourceName);
            sb.Append(" ");

            if (instancedModule.PortParameterNameList.Count != 0)
            {
                sb.Append("#(\r\n");

                first = true;
                foreach (var paramName in instancedModule.PortParameterNameList)
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
                        if (
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
            string? portGroupName = null;
            foreach (var port in instancedModule.Ports.Values)
            {
                if (!first) sb.Append(",\r\n");

                if (port.PortGroupName != portGroupName)
                {
                    portGroupName = port.PortGroupName;
                    sb.Append("// ");
                    sb.Append(portGroupName);
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

        public override DataObject Clone()
        {
            return Clone(Name);
        }
        public override DataObject Clone(string name)
        {
            return new InterfaceInstance()
            {
                BeginIndexReference = BeginIndexReference,
                DefinitionReference = DefinitionReference,
                Name = name,
                ParameterOverrides = ParameterOverrides,
                Project = Project,
                SourceName = SourceName,
                InstancedNameSpace = InstancedNameSpace
            };
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
