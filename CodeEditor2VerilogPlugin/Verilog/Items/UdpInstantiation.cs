using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.Data;
using pluginVerilog.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    /// <summary>
    /// SystemVerilog/Verilog UDP (User Defined Primitive) instantiation
    /// IEEE 1800-2017 A.5.4 / IEEE 1364-2005
    ///
    /// udp_instantiation ::= udp_identifier [ drive_strength ] [ delay2 ] udp_instance { , udp_instance } ;
    /// udp_instance      ::= [ name_of_instance ] ( output_terminal , input_terminal { , input_terminal } )
    /// </summary>
    public class UdpInstantiation : NamedItem, IBuildingBlockInstantiation, INamedElement, IItem
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
        A.5.4 UDP instantiation
        udp_instantiation ::= udp_identifier [ drive_strength ] [ delay2 ] udp_instance { , udp_instance } ;
        udp_instance ::= [ name_of_instance ] ( output_terminal , input_terminal { , input_terminal } )
        */

        public required string SourceName { get; init; }
        public required string SourceProjectName { get; init; }

        public DriveStrength? DriveStrength = null;

        // UDPs do not have parameter ports; keep an empty dictionary for IBuildingBlockInstantiation.
        public required Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }

        public Dictionary<string, Expressions.Expression> PortConnection { get; set; } = new Dictionary<string, Expressions.Expression>();

        public Delay2? Delay2 = null;

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
            Primitive? originalPrimitive = ProjectProperty.DefinitionNameSpace.Get(SourceName) as Primitive;
            if (originalPrimitive == null) return;
            if (!originalPrimitive.Ports.ContainsKey(portName)) return;
            Verilog.DataObjects.Port port = originalPrimitive.Ports[portName];
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

            Data.IVerilogRelatedFile? file = projectProperty.DefinitionNameSpace.GetFile(SourceName);
            if (file == null) return null;
            if (file is not Data.VerilogFile) return null;

            Data.VerilogFile source = (Data.VerilogFile)file;
            if (source == null) return null;

            string instanceKey = Verilog.ParsedDocument.KeyGenerator(file, SourceName, ParameterOverrides);

            CodeEditor2.CodeEditor.ParsedDocument? codeEditorParsedDocument = source.GetInstancedParsedDocument(instanceKey);
            // UDPs do not have parameter ports, so the base parse result suffices.
            if (codeEditorParsedDocument == null)
            {
                codeEditorParsedDocument = source.ParsedDocument;
            }
            if (codeEditorParsedDocument == null) return null;
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
        public IndexReference? LastIndexReference { get; set; } = null;

        public IndexReference? BlockBeginIndexReference { get; set; } = null;

        /*
        A.5.4 UDP instantiation
        udp_instantiation ::= udp_identifier [ drive_strength ] [ delay2 ] udp_instance { , udp_instance } ;
        udp_instance ::= [ name_of_instance ] ( output_terminal , input_terminal { , input_terminal } )
        */
        public static async Task<Func<Data.VerilogCommon.AutoCompleteItem, bool>> ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            Func<Data.VerilogCommon.AutoCompleteItem, bool> itemFilter = (Data.VerilogCommon.AutoCompleteItem ac) =>
            {
                if (ac.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword) return false;
                if (ac.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.Task) return false;
                return true;
            };

            // udp_instantiation can be placed only in module / interface
            BuildingBlock buildingBlock = nameSpace.BuildingBlock as BuildingBlock;
            if (buildingBlock == null) return itemFilter;

            if (!General.IsSimpleIdentifier(word.Text)) return itemFilter;
            if (General.ListOfKeywords.Contains(word.Text)) return itemFilter;

            Project sourceProject = word.Project;

            var udpIdentifier = word.CrateWordReference();
            string udpName = word.Text;
            IndexReference beginIndexReference = word.CreateIndexReference();

            // instance target udp
            Primitive? instancedUdp = word.ProjectProperty.DefinitionNameSpace.Get(udpName) as Primitive;
            if (!word.RootParsedDocument.ReferencedDefinitionNameSpace.Contains(udpName)) word.RootParsedDocument.ReferencedDefinitionNameSpace.Add(udpName);

            if (instancedUdp == null)
            {
                if (word.ProjectProperty.ExtenralLibraryPath.ContainsKey(udpName))
                {
                    word.AddHint("external library");
                    if (!word.RootParsedDocument.ExternalRefrenceModules.Contains(udpName)) word.RootParsedDocument.ExternalRefrenceModules.Add(udpName);
                }
                else
                {
                    word.AddError("unfound udp");
                    word.RootParsedDocument.ReparseRequested = true;
                    if (!word.RootParsedDocument.UnfoundModules.Contains(udpName)) word.RootParsedDocument.UnfoundModules.Add(udpName);
                    CodeEditor2.Controller.AppendLog("## unfound " + udpName + " at " + buildingBlock.Name, Avalonia.Media.Colors.Orange);
                }
            }
            // else: already registered

            word.MoveNext();
            IndexReference blockBeginIndexReference = word.CreateIndexReference();

            string next = word.NextText;

            // [ drive_strength ]
            DriveStrength? driveStrength = null;
            if (word.Text == "(")
            {
                driveStrength = DriveStrength.ParseCreate(word, nameSpace);
            }

            // [ delay2 ]
            Delay2? delay2 = null;
            if (word.Text == "#")
            {
                delay2 = Delay2.ParseCreate(word, nameSpace);
            }

            // [ name_of_instance ] ( output_terminal , input_terminal { , input_terminal } ) { , udp_instance }
            while (!word.Eof)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (!General.IsIdentifier(word.Text))
                {
                    if (word.Prototype) word.AddError("illegal instance name");
                    word.SkipToKeyword(";");
                }

                if (word.RootParsedDocument.Project == null) throw new Exception();

                UdpInstantiation udpInstantiation = new UdpInstantiation()
                {
                    BeginIndexReference = beginIndexReference,
                    DefinitionReference = word.CrateWordReference(),
                    Name = word.Text,
                    Project = word.RootParsedDocument.Project,
                    SourceName = udpName,
                    ParameterOverrides = new Dictionary<string, Expressions.Expression>(),
                    SourceProjectName = sourceProject.Name,
                    DriveStrength = driveStrength,
                    Delay2 = delay2
                };
                udpInstantiation.BlockBeginIndexReference = blockBeginIndexReference;

                if (word.Prototype)
                {
                    udpInstantiation.Prototype = true;

                    if (udpInstantiation.Name == null)
                    {
                        //
                    }
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(udpInstantiation.Name))
                    {
                        word.AddPrototypeError("instance name duplicated");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(udpInstantiation.Name, udpInstantiation);
                    }
                }
                else
                {
                    if (udpInstantiation.Name == null)
                    {
                        //
                    }
                    else if (nameSpace.NamedElements.ContainsIBuldingBlockInstantiation(udpInstantiation.Name))
                    {
                        // duplicated; just replace so that prototype flag is cleared
                        nameSpace.NamedElements.Replace(udpInstantiation.Name, udpInstantiation);
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(udpInstantiation.Name, udpInstantiation);
                    }
                }

                word.MoveNext();

                if (word.Text != "(")
                {
                    word.AddError("( expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return itemFilter;
                }
                word.MoveNext();

                parseListOfPortConnections(word, nameSpace, instancedUdp, udpInstantiation, udpIdentifier);

                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return itemFilter;
                }
                word.MoveNext();
                udpInstantiation.LastIndexReference = word.CreateIndexReference();
                if (!word.Prototype) nameSpace.Items.Add(udpInstantiation);

                if (!word.Prototype && word.Active && udpInstantiation.BlockBeginIndexReference != null)
                {
                    word.AppendBlock(udpInstantiation.BlockBeginIndexReference, udpInstantiation.LastIndexReference);
                }

                if (word.Text != ",") break;
                word.MoveNext();
            }

            if (word.Text != ";")
            {
                word.AddError("; expected");
                return itemFilter;
            }
            word.MoveNext();
            return itemFilter;
        }

        private static void parseListOfPortConnections(
            WordScanner word,
            NameSpace nameSpace,
            Primitive? instancedUdp,
            UdpInstantiation udpInstantiation,
            WordReference udpIdentifier)
        {
            /*
            udp_instance ::= [ name_of_instance ] ( output_terminal , input_terminal { , input_terminal } )

            All UDP port connections must be positional (ordered_port_connection), so we
            do not implement named connections / wildcard here.  The order is:
            output_terminal, input_terminal, input_terminal, ...
            */

            // First: output_terminal (net_lvalue). Use ParseCreateVariableLValue for assignment semantics.
            Expressions.Expression? output = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, true);
            if (output == null)
            {
                if (!word.Prototype) word.AddError("udp output terminal expected");
            }
            else
            {
                if (!word.Prototype && output.Reference != null) output.AssertAssigned();
            }

            // Then: input_terminal { , input_terminal }
            int inputIndex = 0;
            while (!word.Eof && word.Text == ",")
            {
                word.MoveNext();
                Expressions.Expression? input = Expressions.Expression.ParseCreateAcceptImplicitNet(word, nameSpace, false);
                if (input == null)
                {
                    word.AddError("illegal udp input terminal");
                }
                else
                {
                    connectUdpPort(udpInstantiation, instancedUdp, inputIndex, output, input, word.Prototype);
                }

                inputIndex++;
            }

            udpInstantiation.LastIndexReference = word.CreateIndexReference();
        }

        private static void connectUdpPort(
            UdpInstantiation udpInstantiation,
            Primitive? instancedUdp,
            int inputIndex,
            Expressions.Expression? output,
            Expressions.Expression input,
            bool prototype)
        {
            // Track the named port in the connection dictionary as a fallback for CreateString.
            // UDP always has exactly one output (PortsList[0]) followed by N inputs (PortsList[1..]).
            if (instancedUdp != null && !prototype)
            {
                if (instancedUdp.PortsList.Count > 1 + inputIndex && inputIndex >= 0)
                {
                    string portName = instancedUdp.PortsList[1 + inputIndex].Name;
                    udpInstantiation.PortConnection[portName] = input;
                }
            }
        }

        public string? CreateString()
        {
            return CreateString("\t");
        }

        public string? CreateString(string indent)
        {
            Primitive? instancedUdp = GetInstancedBuildingBlock() as Primitive;
            if (instancedUdp == null) return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(SourceName);
            sb.Append(" ");

            // DriveStrength.CreateString() is intentionally not used here to avoid
            // ambiguous resolution between the legacy and modern DriveStrength
            // implementations; users can recover the textual form via a separate
            // helper or a code generator when needed.
            if (DriveStrength != null)
            {
                sb.Append("(strength) ");
            }

            if (Delay2 != null)
            {
                sb.Append("#(");
                if (Delay2.DelayValue0 != null) sb.Append(Delay2.DelayValue0.CreateString());
                if (Delay2.DelayValue1 != null)
                {
                    sb.Append(", ");
                    sb.Append(Delay2.DelayValue1.CreateString());
                }
                sb.Append(") ");
            }

            sb.Append(Name);
            sb.Append(" (");
            if (instancedUdp.PortsList.Count > 0)
            {
                // Output terminal
                sb.Append(" ");
                if (PortConnection.ContainsKey(instancedUdp.PortsList[0].Name))
                {
                    sb.Append(PortConnection[instancedUdp.PortsList[0].Name].CreateString());
                }
                // Input terminals
                for (int i = 1; i < instancedUdp.PortsList.Count; i++)
                {
                    sb.Append(", ");
                    if (PortConnection.ContainsKey(instancedUdp.PortsList[i].Name))
                    {
                        sb.Append(PortConnection[instancedUdp.PortsList[i].Name].CreateString());
                    }
                }
                sb.Append(" ");
            }
            sb.Append(");");
            return sb.ToString();
        }

        // UDP instantiation has no parameter id concept (UDPs do not accept parameter overrides).
        public string ParameterId
        {
            get { return SourceName + ":"; }
        }
    }
}
