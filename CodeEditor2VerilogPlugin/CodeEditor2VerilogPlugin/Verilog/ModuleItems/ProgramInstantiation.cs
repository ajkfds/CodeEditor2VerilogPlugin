using Avalonia.Threading;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.Data;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public class ProgramInstantiation : Item, IBuildingBlockInstantiation, INamedElement
    {
        public NamedElements NamedElements { get; } = new NamedElements();

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Identifier; } }

        /*
        program_instantiation ::= program_identifier [ parameter_value_assignment ] hierarchical_instance { , hierarchical_instance } ;
        parameter_value_assignment ::= # ( [ list_of_parameter_assignments ] )
        list_of_parameter_assignments ::= ordered_parameter_assignment { , ordered_parameter_assignment } | named_parameter_assignment { , named_parameter_assignment }
        ordered_parameter_assignment ::= expression
        named_parameter_assignment ::= .parameter_identifier ( [ expression ] )
        hierarchical_instance ::= name_of_instance ( [ list_of_port_connections ] )
        name_of_instance ::= module_instance_identifier [ range ]
        list_of_port_connections ::= ordered_port_connection { , ordered_port_connection } | named_port_connection { , named_port_connection }
        ordered_port_connection ::= { attribute_instance } [ expression ]
        named_port_connection ::= { attribute_instance } .port_identifier ( [ expression ] ) | { attribute_instance } .*
        */

        public required string SourceName { get; init; }
        public required string SourceProjectName { get; init; }
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

        public void AppendLabel(IndexReference iref, AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            PortReference? portRef = null;
            foreach (PortReference pRef in PortReferences)
            {
                if (iref.IsSmallerThan(pRef.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(pRef.LastIndexReference)) continue;
                portRef = pRef;
                break;
            }
            if (portRef == null) return;

            string portName = portRef.Name;
            Program? originalProgram = ProjectProperty.GetBuildingBlock(SourceName) as Program;
            if (originalProgram == null) return;
            if (!originalProgram.Ports.ContainsKey(portName)) return;
            Verilog.DataObjects.Port port = originalProgram.Ports[portName];
            label.AppendLabel(port.GetLabel());
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
            if (file == null) return null;
            if (file is not Data.VerilogFile) return null;

            Data.VerilogFile source = (Data.VerilogFile)file;
            if (source == null) return null;

            string instanceKey = Verilog.ParsedDocument.KeyGenerator(file, SourceName, ParameterOverrides);

            CodeEditor2.CodeEditor.ParsedDocument? codeEditorParsedDocument = source.GetInstancedParsedDocument(instanceKey);
            if (codeEditorParsedDocument is not ParsedDocument) return null;
            ParsedDocument? parsedDocument = (ParsedDocument)codeEditorParsedDocument;
            if (parsedDocument == null) return null;
            if (parsedDocument.Root == null) return null;

            if (parsedDocument.Root.BuildingBlocks.TryGetValue(SourceName, out BuildingBlock? buildingBlock))
            {
                return buildingBlock;
            }
            else
            {
                return null;
            }
        }

        public bool Prototype { get; set; } = false;

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? BlockBeginIndexReference { get; set; } = null;
        public IndexReference? LastIndexReference { get; set; }


        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            // program instantiation can be placed only in module, interface, or program
            BuildingBlock buildingBlock = nameSpace.BuildingBlock as BuildingBlock;
            if (buildingBlock == null) return false;

            if (!General.IsSimpleIdentifier(word.Text)) return false;
            if (General.ListOfKeywords.Contains(word.Text)) return false;

            Project sourceProject = word.Project;

            var programIdentifier = word.CrateWordReference();
            string programName = word.Text;
            IndexReference beginIndexReference = word.CreateIndexReference();

            // Get the instanced program
            Program? instancedProgram = word.ProjectProperty.GetBuildingBlock(programName) as Program;
            if (instancedProgram == null)
            {
                // module instanceである可能性がある。
                return false;
                word.AddError("unfound program");
                word.RootParsedDocument.ReparseRequested = true;
                if (!word.RootParsedDocument.UnfoundModules.Contains(programName)) word.RootParsedDocument.UnfoundModules.Add(programName);
                CodeEditor2.Controller.AppendLog("## unfound " + programName + " at " + buildingBlock.Name, Avalonia.Media.Colors.Orange);
            }

            word.MoveNext();
            IndexReference blockBeginIndexReference = word.CreateIndexReference();

            string next = word.NextText;
            if (word.Text != "#" && next != "(" && next != ";" && General.IsIdentifier(word.Text))
            {
                programIdentifier.AddError("illegal program item");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            programIdentifier.Color(CodeDrawStyle.ColorType.Keyword);

            Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();

            if (word.Text == "#") // parameter
            {
                ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, instancedProgram);
            }

            while (!word.Eof)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (!General.IsIdentifier(word.Text))
                {
                    if (word.Prototype) word.AddError("illegal instance name");
                    word.SkipToKeyword(";");
                }

                if (word.RootParsedDocument.Project == null) throw new Exception();

                ProgramInstantiation programInstantiation = new ProgramInstantiation()
                {
                    BeginIndexReference = beginIndexReference,
                    DefinitionReference = word.CrateWordReference(),
                    Name = word.Text,
                    Project = word.RootParsedDocument.Project,
                    SourceName = programName,
                    ParameterOverrides = parameterOverrides,
                    SourceProjectName = sourceProject.Name
                };
                programInstantiation.BlockBeginIndexReference = blockBeginIndexReference;

                // Swap to parameter overridden program
                if (instancedProgram != null)
                {
                    if (parameterOverrides.Count != 0)
                    {
                        instancedProgram = word.ProjectProperty.GetInstancedBuildingBlock(programInstantiation) as Program;
                    }

                    if (instancedProgram == null)
                    {
                        word.AddError("not parsed yet.");
                    }
                }

                if (word.Prototype)
                {
                    programInstantiation.Prototype = true;

                    if (programInstantiation.Name == null)
                    {
                        // 
                    }
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(programInstantiation.Name))
                    {   // duplicated
                        word.AddPrototypeError("instance name duplicated");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(programInstantiation.Name, programInstantiation);
                    }
                }
                else
                {
                    if (programInstantiation.Name == null)
                    {
                        // 
                    }
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(programInstantiation.Name))
                    {   // duplicated
                        if (((IBuildingBlockInstantiation)nameSpace.NamedElements[programInstantiation.Name]).Prototype)
                        {
                            ProgramInstantiation? prog = nameSpace.NamedElements[programInstantiation.Name] as ProgramInstantiation;
                            if (prog != null)
                            {
                                programInstantiation = prog;
                                programInstantiation.Prototype = false;
                            }
                        }
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

                parseListOfPortConnections(word, nameSpace, instancedProgram, programInstantiation, programIdentifier);

                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return true;
                }
                word.MoveNext();
                programInstantiation.LastIndexReference = word.CreateIndexReference();

                if (!word.Prototype && word.Active && programInstantiation.BlockBeginIndexReference != null)
                {
                    word.AppendBlock(programInstantiation.BlockBeginIndexReference, programInstantiation.LastIndexReference);
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
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            WordReference programIdentifier)
        {
            /*
            list_of_port_connections ::= 
                  ordered_port_connection { "," ordered_port_connection }
                | named_port_connection { "," named_port_connection }
             */

            if (word.GetCharAt(0) == '.')
            { // named port assignment
                parseNamedPortConnections(word, nameSpace, instancedProgram, programInstantiation, programIdentifier);
            }
            else
            { // ordered port assignment
                parseOrderedPortConnections(word, nameSpace, instancedProgram, programInstantiation, programIdentifier);
            }
        }

        private static void parseOrderedPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            WordReference programIdentifier)
        {
            /*
            ordered_port_connection ::= { attribute_instance } [ expression ]
             */
            int i = 0;
            while (!word.Eof && word.Text != ")")
            {
                string portName = "";
                if (instancedProgram != null && i < instancedProgram.PortsList.Count)
                {
                    portName = instancedProgram.PortsList[i].Name;
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (word.Prototype && expression != null && !programInstantiation.PortConnection.ContainsKey(portName)) programInstantiation.PortConnection.Add(portName, expression);
                }
                else
                {
                    if (instancedProgram != null) word.AddError("illegal port connection");
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
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            WordReference programIdentifier)
        {
            /*
            named_port_connection ::= 
                  { attribute_instance } "." port_identifier [ "(" [ expression ] ")" ] 
                | { attribute_instance } ".*"
             */

            bool wildcardConnection = false;

            if (word.Eof) return;

            List<string> notWrittenPortName;
            if (instancedProgram == null)
            {
                notWrittenPortName = new List<string>();
            }
            else
            {
                notWrittenPortName = instancedProgram.Ports.Keys.ToList();
            }

            WordReference? wildcardRef = null;
            while (!word.Eof && word.Text == ".")
            {
                word.MoveNext();    // .
                if (word.Text == "*") // wildcard named port connection (.*)
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

                string portName = word.Text;
                IndexReference startRef = word.CreateIndexReference();
                if (notWrittenPortName.Contains(portName)) notWrittenPortName.Remove(portName);
                WordReference pinReference = word.GetReference();
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (word.Prototype)
                {
                    if (programInstantiation.PortConnection.ContainsKey(portName))
                    {
                        word.AddPrototypeError("duplicated");
                    }
                    word.MoveNext();
                }
                else
                {
                    if (instancedProgram != null)
                    {
                        if (instancedProgram.Ports.ContainsKey(portName))
                        {

                        }
                        else
                        {
                            word.AddError("illegal port name");
                        }
                    }
                    word.MoveNext();

                    PortReference pRef = new PortReference(portName, startRef, word.CreateIndexReference());
                    programInstantiation.PortReferences.Add(pRef);
                }

                if (word.Text == "(")
                {
                    parseNamedPortConnection(word, nameSpace, instancedProgram, programInstantiation, portName, programIdentifier);
                }
                else
                {
                    // Implicit port connection
                    parseImplicitPortConnection(word, nameSpace, instancedProgram, programInstantiation, portName, pinReference, programIdentifier);
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

            if (notWrittenPortName.Count != 0 && !word.Prototype)
            {
                if (wildcardConnection)
                {
                    parseWildcardPortConnection(word, nameSpace, instancedProgram, programInstantiation, notWrittenPortName, wildcardRef);
                }
                else
                {
                    programIdentifier.AddWarning("missing port " + notWrittenPortName[0]);
                }
            }
        }

        private static void parseWildcardPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            List<string> notWrittenPortName,
            WordReference? wildcardRef)
        {
            if (instancedProgram == null) return;
            if (wildcardRef == null) throw new Exception();

            foreach (string portName in notWrittenPortName)
            {
                DataObject? targetObject = nameSpace.NamedElements.GetDataObject(portName);
                if (targetObject == null)
                {
                    wildcardRef.ApplyRule(word.ProjectProperty.RuleSet.NotAllPortConnectedWithWildcardNamedPortConnections, "\nport :" + portName);
                    continue;
                }
                Port port = instancedProgram.Ports[portName];

                Expressions.Expression? expression = Expressions.DataObjectReference.Create(targetObject, nameSpace);
                if (port.Direction == Port.DirectionEnum.Output)
                {
                    targetObject.AssignedReferences.Add(wildcardRef);
                }
                else
                {
                    targetObject.UsedReferences.Add(wildcardRef);
                }

                connectPort(word, programInstantiation, instancedProgram, portName, expression);
            }
        }

        private static void parseNamedPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            string portName,
            WordReference programIdentifier)
        {
            if (word.Text != "(") throw new Exception();
            var startRef = word.GetReference();
            word.MoveNext();

            bool outPort = false;
            if (instancedProgram != null && instancedProgram.Ports.ContainsKey(portName))
            {
                if (instancedProgram.Ports[portName].Direction == DataObjects.Port.DirectionEnum.Output
                    || instancedProgram.Ports[portName].Direction == DataObjects.Port.DirectionEnum.Inout)
                {
                    outPort = true;
                }
            }

            if (word.Text == ")")
            {
                if (!outPort)
                {
                    WordReference.CreateReferenceRange(startRef, word.GetReference()).AddWarning("floating input");
                }
                word.MoveNext();
                return;
            }

            Expressions.Expression? expression;
            if (outPort)
            {
                expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, true);
            }
            else
            {
                expression = Expressions.Expression.ParseCreateAcceptImplicitNet(word, nameSpace, false);
            }

            if (expression != null)
            {
                connectPort(word, programInstantiation, instancedProgram, portName, expression);
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
                    if (new List<string> { "endmodule", "endtask", "end", "endinterface", "endfunction", "endprogram" }.Contains(word.Text)) return;
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                        return;
                    }
                    word.MoveNext();
                }
            }
        }

        private static void parseImplicitPortConnection(
            WordScanner word,
            NameSpace nameSpace,
            Program? instancedProgram,
            ProgramInstantiation programInstantiation,
            string portName,
            WordReference pinReference,
            WordReference programIdentifier)
        {
            if (instancedProgram == null) return;
            DataObject? targetObject = nameSpace.NamedElements.GetDataObject(portName);

            if (targetObject == null)
            {
                word.AddError("illegal port connection");
                return;
            }
            if (!instancedProgram.Ports.ContainsKey(portName))
            {
                word.AddError("illegal port connection : " + portName);
                return;
            }
            Port port = instancedProgram.Ports[portName];

            Expressions.Expression? expression = Expressions.DataObjectReference.Create(targetObject, nameSpace);
            if (port.Direction == Port.DirectionEnum.Output)
            {
                targetObject.AssignedReferences.Add(pinReference);
            }
            else
            {
                targetObject.UsedReferences.Add(pinReference);
            }

            connectPort(word, programInstantiation, instancedProgram, portName, expression);
        }

        private static void connectPort(
            WordScanner word,
            ProgramInstantiation programInstantiation,
            Program? instancedProgram,
            string portName,
            Expressions.Expression expression)
        {
            if (word.Prototype)
            {
                if (expression != null && !programInstantiation.PortConnection.ContainsKey(portName))
                {
                    programInstantiation.PortConnection.Add(portName, expression);
                }
            }
            else
            {
                if (expression != null && programInstantiation.PortConnection.ContainsKey(portName))
                {
                    programInstantiation.PortConnection[portName] = expression;
                }
            }
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
            Program? instancedProgram = GetInstancedBuildingBlock() as Program;
            if (instancedProgram == null) return null;

            StringBuilder sb = new StringBuilder();
            bool first;

            sb.Append(SourceName);
            sb.Append(" ");

            if (instancedProgram.PortParameterNameList.Count != 0)
            {
                sb.Append("#(\n");

                first = true;
                foreach (var paramName in instancedProgram.PortParameterNameList)
                {
                    if (!first) sb.Append(",\n");
                    sb.Append(indent);
                    sb.Append(".");
                    sb.Append(paramName);
                    sb.Append("\t( ");
                    if (ParameterOverrides.ContainsKey(paramName))
                    {
                        sb.Append(ParameterOverrides[paramName].CreateString());
                    }
                    sb.Append(" )");
                    first = false;
                }
                sb.Append("\n) ");
            }

            sb.Append(Name);
            sb.Append(" (\n");

            first = true;
            string? portGroupName = null;
            foreach (var port in instancedProgram.Ports.Values)
            {
                if (!first) sb.Append(",\n");

                if (port.PortGroupName != portGroupName)
                {
                    portGroupName = port.PortGroupName;
                    sb.Append("// ");
                    sb.Append(portGroupName);
                    sb.Append("\n");
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
            sb.Append("\n);");

            return sb.ToString();
        }
    }
}
