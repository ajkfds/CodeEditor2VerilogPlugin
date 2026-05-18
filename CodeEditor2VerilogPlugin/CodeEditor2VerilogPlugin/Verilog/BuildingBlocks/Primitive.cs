using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    /// <summary>
    /// SystemVerilog/Verilog UDP (User Defined Primitive) Declaration
    /// IEEE 1800-2017 / IEEE 1364-2005
    /// 
    /// udp_declaration ::=
    ///     { attribute_instance } primitive [ primitive_identifier ] 
    ///     ( [ output_port_identifier ] , [ input_port_identifier ] { , [ input_port_identifier ] } ) ;
    ///     [ primitive_port_list_or_body ]
    ///     "endprimitive" [ : primitive_identifier ]
    /// 
    /// primitive_port_list_or_body ::=
    ///     primitive_port_declaration { primitive_port_declaration } table_definition
    ///   | table_definition
    /// 
    /// primitive_port_declaration ::=
    ///     { attribute_instance } output_declaration
    ///   | { attribute_instance } input_declaration
    /// 
    /// table_definition ::=
    ///     "table" { table_entry } "endtable"
    /// 
    /// table_entry ::=
    ///     level_input_list : level_output ;
    ///   | edge_input_list : level_output ;
    ///   | edge_input_list : edge_output ;
    ///   | level_input_list : "x" ;
    /// 
    /// level_input_list ::= level_symbol { level_symbol }
    /// edge_input_list ::= edge_indicator { edge_indicator }
    /// edge_indicator ::= level_symbol | edge_symbol level_symbol
    /// 
    /// level_symbol ::= 0 | 1 | x | X | b | B
    /// edge_symbol ::= r | R | f | F | p | P | n | N | *
    /// 
    /// level_output ::= 0 | 1 | x | X | -
    /// edge_output ::= 0 | 1 | x | X
    /// </summary>
    public class Primitive : BuildingBlock, IBuildingBlock
    {
        protected Primitive() : base(null, null)
        {
        }

        // Port definitions
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

        /// <summary>
        /// UDP table entries
        /// </summary>
        public List<TableEntry> TableEntries { get; } = new List<TableEntry>();

        /// <summary>
        /// Whether this is a sequential UDP (has clock input)
        /// </summary>
        public bool IsSequential { get; set; } = false;

        /// <summary>
        /// Optional reset level for sequential UDPs
        /// </summary>
        public char? ResetLevel { get; set; }

        /// <summary>
        /// Table entry representing one row in the UDP truth table
        /// </summary>
        public class TableEntry
        {
            /// <summary>
            /// Input values - each character represents one input level
            /// </summary>
            public string InputLevels { get; set; } = "";

            /// <summary>
            /// Output value (0, 1, x, X, or - for no change in combinational)
            /// </summary>
            public char OutputLevel { get; set; } = 'x';

            /// <summary>
            /// Whether this is an edge-triggered entry
            /// </summary>
            public bool IsEdge { get; set; } = false;

            /// <summary>
            /// Edge symbols for sequential UDP (r, f, p, n, *)
            /// </summary>
            public string EdgeIndicators { get; set; } = "";
        }

        public static async Task<Primitive?> ParseCreate(WordScanner word, Attribute attribute, BuildingBlock parent, Data.IVerilogRelatedFile file, bool protoType)
        {
            return await ParseCreate(word, null, attribute, parent, file, protoType);
        }

        public static async Task<Primitive?> ParseCreate(
            WordScanner word,
            Dictionary<string, Expressions.Expression>? parameterOverrides,
            Attribute attribute,
            BuildingBlock parent,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
            primitive ::= { attribute_instance } "primitive" primitive_identifier 
                ( [ output_port_identifier ] , [ input_port_identifier ] { , [ input_port_identifier ] } ) ;
                [ primitive_port_list_or_body ]
                "endprimitive" [ : primitive_identifier ]
            */

            if (word.Text != "primitive") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();

            // primitive_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("primitive identifier expected");
                word.SkipToKeyword("endprimitive");
                if (word.Text == "endprimitive") word.MoveNext();
                return null;
            }

            Primitive primitive = new Primitive()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = file,
                Name = word.Text,
                Parent = parent,
                Project = word.Project
            };
            primitive.BuildingBlock = primitive;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            primitive.NameReference = word.GetReference();
            word.MoveNext();

            // Parse port list: ( output_port, input_port { , input_port } )
            if (word.Text == "(")
            {
                word.MoveNext();

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                else
                {
                    // Parse output port first
                    Port outputPort = new Port()
                    {
                        Name = word.Text,
                        Direction = Port.DirectionEnum.Output,
                        DefinitionReference = word.GetReference()
                    };
                    word.Color(CodeDrawStyle.ColorType.Variable);
                    word.MoveNext();
                    primitive.Ports.Add(outputPort.Name, outputPort);
                    primitive.PortsList.Add(outputPort);

                    while (word.Text == ",")
                    {
                        word.MoveNext();

                        // Parse input port
                        Port inputPort = new Port()
                        {
                            Name = word.Text,
                            Direction = Port.DirectionEnum.Input,
                            DefinitionReference = word.GetReference()
                        };
                        word.Color(CodeDrawStyle.ColorType.Variable);
                        word.MoveNext();
                        primitive.Ports.Add(inputPort.Name, inputPort);
                        primitive.PortsList.Add(inputPort);
                    }

                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(") expected");
                    }
                }
            }
            else
            {
                word.AddError("( expected");
                word.SkipToKeyword("endprimitive");
                if (word.Text == "endprimitive") word.MoveNext();
                return null;
            }

            // Semicolon after port list
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword("endprimitive");
                if (word.Text == "endprimitive") word.MoveNext();
                return null;
            }

            // Parse optional declarations (reg, etc.) and table
            await parsePrimitiveBody(word, primitive);

            // endprimitive keyword
            if (word.Text == "endprimitive")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                primitive.LastIndexReference = word.CreateIndexReference();

                if (primitive.BlockBeginIndexReference != null)
                {
                    word.AppendBlock(primitive.BlockBeginIndexReference, primitive.LastIndexReference, primitive.Name, false);
                }
                word.MoveNext();

                // Optional : primitive_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == primitive.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("primitive name mismatch");
                        word.MoveNext();
                    }
                }
            }
            else
            {
                word.AddError("endprimitive expected");
            }

            // Register with parent building block
            parent.AddOrUpdateBuildingBlock(primitive.Name, primitive);

            return primitive;
        }

        private static async System.Threading.Tasks.Task parsePrimitiveBody(WordScanner word, Primitive primitive)
        {
            /*
            primitive_port_list_or_body ::=
                primitive_port_declaration { primitive_port_declaration } table_definition
              | table_definition

            table_definition ::=
                "table" { table_entry } "endtable"

            table_entry ::=
                level_input_list : level_output ;
              | edge_input_list : level_output ;
              | edge_input_list : edge_output ;
              | level_input_list : "x" ;
            */

            // Skip declarations until we hit "table" keyword
            while (!word.Eof && word.Text != "endprimitive")
            {
                if (word.Text == "table")
                {
                    // Start of table definition
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    // Parse table entries
                    while (!word.Eof && word.Text != "endtable")
                    {
                        var entry = parseTableEntry(word, primitive);
                        if (entry != null)
                        {
                            primitive.TableEntries.Add(entry);
                        }
                        else if (word.Text == "endtable")
                        {
                            break;
                        }
                        else
                        {
                            // Skip invalid line
                            word.MoveNext();
                        }

                        // Check for endtable
                        if (word.Text == "endtable")
                        {
                            break;
                        }

                        // Expect semicolon after entry
                        if (word.Text == ";")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                        }
                    }

                    // endtable keyword
                    if (word.Text == "endtable")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("endtable expected");
                    }

                    break;
                }
                else if (word.Text == "reg")
                {
                    // Sequential UDP: output is registered
                    primitive.IsSequential = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else if (word.Text == "output")
                {
                    // Output port declaration
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    // Skip port name
                    if (General.IsIdentifier(word.Text))
                    {
                        word.Color(CodeDrawStyle.ColorType.Variable);
                        word.MoveNext();
                    }
                }
                else if (word.Text == "input")
                {
                    // Input port declaration
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    // Skip port name
                    if (General.IsIdentifier(word.Text))
                    {
                        word.Color(CodeDrawStyle.ColorType.Variable);
                        word.MoveNext();
                    }
                }
                else if (word.Text == "reg")
                {
                    // Sequential UDP output register
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    primitive.IsSequential = true;
                }
                else if (word.Text == ";")
                {
                    // Skip semicolon
                    word.MoveNext();
                }
                else
                {
                    // Unknown token, skip
                    word.MoveNext();
                }
            }
        }

        private static TableEntry? parseTableEntry(WordScanner word, Primitive primitive)
        {
            /*
            table_entry ::=
                level_input_list : level_output ;
              | edge_input_list : level_output ;
              | edge_input_list : edge_output ;
              | level_input_list : "x" ;

            level_input_list ::= level_symbol { level_symbol }
            edge_input_list ::= edge_indicator { edge_indicator }
            edge_indicator ::= level_symbol | edge_symbol level_symbol

            level_symbol ::= 0 | 1 | x | X | b | B
            edge_symbol ::= r | R | f | F | p | P | n | N | *

            level_output ::= 0 | 1 | x | X | -
            edge_output ::= 0 | 1 | x | X
            */

            TableEntry entry = new TableEntry();

            // Parse input levels
            while (!word.Eof && word.Text != ":")
            {
                string token = word.Text;

                // Single character level symbols: 0, 1, x, X, b, B
                if (token.Length == 1 && "01xXbB".IndexOf(token[0]) >= 0)
                {
                    entry.InputLevels += token.ToLower();
                    word.Color(CodeDrawStyle.ColorType.Number);
                    word.MoveNext();
                }
                // Edge symbols can be combined
                else if (token.Length >= 2)
                {
                    // Check for edge patterns like (01), (fx), etc.
                    if (token.StartsWith("(") && token.EndsWith(")"))
                    {
                        string inner = token.Substring(1, token.Length - 2);
                        entry.IsEdge = true;
                        entry.EdgeIndicators += token;
                        word.Color(CodeDrawStyle.ColorType.Number);
                        word.MoveNext();
                    }
                    else
                    {
                        // Could be edge like r, f, p, n, *
                        foreach (char c in token)
                        {
                            if ("rfpnRFPN*".IndexOf(c) >= 0)
                            {
                                entry.InputLevels += c;
                                entry.IsEdge = true;
                            }
                            else
                            {
                                entry.InputLevels += char.ToLower(c);
                            }
                        }
                        word.Color(CodeDrawStyle.ColorType.Number);
                        word.MoveNext();
                    }
                }
                else if (token == "(")
                {
                    // Edge pattern: (01), (fx), etc.
                    word.MoveNext();
                    string edgePattern = "(";
                    while (!word.Eof && word.Text != ")")
                    {
                        edgePattern += word.Text;
                        entry.IsEdge = true;
                        word.MoveNext();
                    }
                    if (word.Text == ")")
                    {
                        edgePattern += ")";
                        entry.EdgeIndicators += edgePattern;
                        word.MoveNext();
                    }
                }
                else
                {
                    // Unknown input symbol, skip
                    word.MoveNext();
                }

                if (word.Text == ":")
                {
                    break;
                }

                // Skip spaces between symbols
                if (word.Text == " " || word.Text == "\t")
                {
                    word.MoveNext();
                }
            }

            // Colon separator
            if (word.Text != ":")
            {
                word.AddError(": expected in table entry");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse output level
            string outputToken = word.Text;
            if (outputToken.Length == 1 && "01xX-".IndexOf(outputToken[0]) >= 0)
            {
                entry.OutputLevel = char.ToLower(outputToken[0]);
                word.Color(CodeDrawStyle.ColorType.Number);
                word.MoveNext();
            }
            else
            {
                word.AddError("invalid output level in table entry");
                return null;
            }

            // Semicolon will be consumed by caller
            return entry;
        }

        public override List<string> GetExitKeywords()
        {
            return new List<string> { "endprimitive" };
        }

        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            // Add port names as autocomplete items
            foreach (var port in Ports.Values)
            {
                items.Add(new AutocompleteItem(
                    port.Name,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Variable),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable)
                ));
            }
        }
    }
}
