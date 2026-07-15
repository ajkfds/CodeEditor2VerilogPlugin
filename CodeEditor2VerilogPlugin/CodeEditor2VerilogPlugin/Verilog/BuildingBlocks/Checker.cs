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
    /// SystemVerilog Checker Declaration
    /// IEEE 1800-2017
    /// 
    /// checker_declaration ::=
    ///     "checker" checker_identifier [ ( [ checker_port_list ] ) ] ;
    ///     { { attribute_instance } checker_or_generate_item }
    ///     "endchecker" [ : checker_identifier ]
    /// 
    /// checker_port_list ::=
    ///     checker_port_item {, checker_port_item}
    /// 
    /// checker_port_item ::=
    ///     { attribute_instance } [ checker_port_direction ] property_formal_type
    ///     formal_port_identifier {variable_dimension} [ "=" property_actual_arg ]
    /// 
    /// checker_port_direction ::= "input" | "output"
    /// 
    /// checker_or_generate_item ::=
    ///       checker_or_generate_item_declaration
    ///     | initial_construct
    ///     | always_construct
    ///     | final_construct
    ///     | assertion_item
    ///     | continuous_assign
    ///     | checker_generate_item
    /// 
    /// checker_or_generate_item_declaration ::=
    ///       [ rand ] data_declaration
    ///     | function_declaration
    ///     | checker_declaration
    ///     | assertion_item_declaration
    ///     | covergroup_declaration
    ///     | overload_declaration
    ///     | genvar_declaration
    ///     | clocking_declaration
    ///     | default clocking clocking_identifier ;
    ///     | default disable iff expression_or_dist ;
    ///     | ;
    /// 
    /// checker_generate_item ::=
    ///       loop_generate_construct
    ///     | conditional_generate_construct
    ///     | generate_region
    ///     | elaboration_system_task
    /// </summary>
    public class Checker : BuildingBlock, IBuildingBlock, IModuleOrInterfaceOrCheckerOrClass
    {
        protected Checker() : base(null, null)
        {
        }

        // Port
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
        /// Default clocking for this checker
        /// </summary>
        public string? DefaultClocking { get; set; }

        /// <summary>
        /// Default disable condition for this checker
        /// </summary>
        public Expressions.Expression? DefaultDisable { get; set; }

        public static Checker? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
        }

        public static Checker? ParseCreate(
            WordScanner word,
            NameSpace nameSpace,
            Attribute? attribute,
            BuildingBlock parent,
            Data.IVerilogRelatedFile file,
            bool protoType
            )
        {
            /*
            checker_declaration ::=
                "checker" checker_identifier [ ( [ checker_port_list ] ) ] ;
                { { attribute_instance } checker_or_generate_item }
                "endchecker" [ : checker_identifier ]
            */

            if (word.Text != "checker") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();

            // checker_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal checker name");
                word.SkipToKeyword("endchecker");
                if (word.Text == "endchecker") word.MoveNext();
                return null;
            }

            Checker checker = new Checker()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                File = file,
                Name = word.Text,
                Parent = parent,
                Project = word.Project
            };
            checker.BuildingBlock = checker;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            checker.NameReference = word.GetReference();
            word.MoveNext();

            // Parse optional port list: ( [ checker_port_list ] )
            if (word.Text == "(")
            {
                word.MoveNext();
                if (word.Text != ")")
                {
                    // Parse checker_port_list
                    while (!word.Eof && word.Text != ")")
                    {
                        if (!parseCheckerPortItem(word, checker))
                        {
                            word.AddError("illegal checker port item");
                            break;
                        }

                        if (word.Text == ",")
                        {
                            word.MoveNext();
                        }
                        else if (word.Text != ")")
                        {
                            word.AddError(") or , expected");
                            break;
                        }
                    }
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

            // Semicolon after port list or checker keyword
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
            }

            // Parse checker items
            parseCheckerItems(word, checker);

            // endchecker keyword
            if (word.Text == "endchecker")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                checker.LastIndexReference = word.CreateIndexReference();

                if (checker.BlockBeginIndexReference != null)
                {
                    word.AppendBlock(checker.BlockBeginIndexReference, checker.LastIndexReference, checker.Name, false);
                }
                word.MoveNext();

                // Optional : checker_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == checker.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("checker name mismatch");
                        word.MoveNext();
                    }
                }
            }
            else
            {
                word.AddError("endchecker expected");
            }

            // Register with parent building block
            bool added = parent.AddOrUpdateBuildingBlock(checker.Name, checker);
            if (!added && protoType)
            {
                word.AddError("duplicated checker name");
            }

            // Also register in namespace
            if (!nameSpace.NamedElements.ContainsKey(checker.Name))
            {
                nameSpace.NamedElements.Add(checker.Name, checker);
            }

            return checker;
        }

        /// <summary>
        /// Parse a checker port item
        /// checker_port_item ::=
        ///     { attribute_instance } [ checker_port_direction ] property_formal_type
        ///     formal_port_identifier {variable_dimension} [ "=" property_actual_arg ]
        /// </summary>
        private static bool parseCheckerPortItem(WordScanner word, Checker checker)
        {
            // [ attribute_instance ]
            Attribute? attr = null;
            if (word.Text == "(*)")
            {
                attr = Attribute.ParseCreate(word, checker);
            }

            // [ checker_port_direction ]
            Port.DirectionEnum direction = Port.DirectionEnum.Input;
            if (word.Text == "input")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                direction = Port.DirectionEnum.Input;
            }
            else if (word.Text == "output")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                direction = Port.DirectionEnum.Output;
            }

            // property_formal_type - can be various types
            // For simplicity, we'll parse basic types like logic, bit, etc.
            DataObjects.DataTypes.IDataType? dataType = null;

            // Handle direction keywords that might follow
            if (word.Text == "input" || word.Text == "output")
            {
                // Direction already parsed, now parse the type
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            // Parse data type
            if (word.Text == "logic" || word.Text == "bit" || word.Text == "reg")
            {
                dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, checker, DataObjects.DataTypes.DataTypeEnum.Logic);
            }
            else if (word.Text == "wire" || word.Text == "tri")
            {
                dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, checker, DataObjects.DataTypes.DataTypeEnum.Logic);
            }
            else if (word.Text == "event")
            {
                dataType = DataObjects.DataTypes.EventType.Create();
            }
            else if (word.Text == "integer" || word.Text == "int")
            {
                dataType = DataObjects.DataTypes.IntType.Create(false);
            }
            else if (word.Text == "byte" || word.Text == "shortint" || word.Text == "longint")
            {
                dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, checker, null);
            }
            else
            {
                // Try to find user-defined type
                var namedElement = checker.GetNamedElementUpward(word.Text);
                if (namedElement is DataObjects.DataTypes.IDataType)
                {
                    dataType = (DataObjects.DataTypes.IDataType)namedElement;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    // Default to logic
                    dataType = DataObjects.DataTypes.LogicType.Create(false, null);
                }
            }

            // formal_port_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal port identifier");
                return false;
            }

            string portName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            // { variable_dimension }
            List<DataObjects.Arrays.UnPackedArray> unpackedArrays = new List<DataObjects.Arrays.UnPackedArray>();
            while (word.Text == "[" && !word.Eof)
            {
                var array = DataObjects.Arrays.UnPackedArray.ParseCreate(word, checker);
                if (array is DataObjects.Arrays.UnPackedArray unpacked)
                {
                    unpackedArrays.Add(unpacked);
                }
                else
                {
                    word.AddError("illegal dimension");
                    break;
                }
            }

            // [ "=" property_actual_arg ]
            Expressions.Expression? defaultValue = null;
            if (word.Text == "=")
            {
                word.MoveNext();
                // Parse property actual argument (expression)
                defaultValue = Expressions.Expression.ParseCreate(word, checker);
            }

            // Create port
            Port port = new Port()
            {
                Name = portName,
                Direction = direction,
                //DataType = dataType,
                DefinitionReference = word.GetReference()
            };

            foreach (var arr in unpackedArrays)
            {
                port.DataObject?.UnpackedArrays.Add(arr);
            }

            checker.Ports.Add(portName, port);
            checker.PortsList.Add(port);

            return true;
        }

        /// <summary>
        /// Parse checker items (body of the checker)
        /// </summary>
        private static void parseCheckerItems(WordScanner word, Checker checker)
        {
            while (!word.Eof && word.Text != "endchecker")
            {
                word.CheckCancelToken();

                switch (word.Text)
                {
                    // checker_or_generate_item_declaration
                    case "function":
                    case "task":
                        FunctionOrTask.Parse(word, checker);
                        break;

                    // initial_construct
                    case "initial":
                        Verilog.Items.InitialConstruct.ParseCreate(word, checker);
                        break;

                    // always_construct
                    case "always":
                        Verilog.Items.AlwaysConstruct.ParseCreate(word, checker);
                        break;

                    // final_construct
                    case "final":
                        Verilog.Items.FinalConstruct.ParseCreate(word, checker);
                        break;

                    // assertion_item or assertion_item_declaration
                    case "assert":
                    case "assume":
                    case "cover":
                    case "restrict":
                        // Check if it's a concurrent assertion (with property keyword)
                        if (word.NextText == "property")
                        {
                            Statements.ProceduralAssertionStatement.ParseCreate(word, checker, null);
                        }
                        else
                        {
                            Statements.ImmidiateAssertionStatement.ParseCreate(word, checker, null);
                        }
                        break;


                    // continuous_assign
                    case "assign":
                        Verilog.Items.ContinuousAssign.ParseCreate(word, checker);
                        break;

                    // default clocking
                    case "default":
                        if (word.NextText == "clocking")
                        {
                            word.MoveNext(); // default
                            word.MoveNext(); // clocking
                            if (General.IsIdentifier(word.Text))
                            {
                                checker.DefaultClocking = word.Text;
                                word.Color(CodeDrawStyle.ColorType.Identifier);
                                word.MoveNext();
                                if (word.Text == ";")
                                {
                                    word.Color(CodeDrawStyle.ColorType.Keyword);
                                    word.MoveNext();
                                }
                            }
                        }
                        else if (word.NextText == "disable")
                        {
                            // default disable iff expression_or_dist ;
                            word.MoveNext(); // default
                            word.MoveNext(); // disable
                            if (word.Text == "iff")
                            {
                                word.Color(CodeDrawStyle.ColorType.Keyword);
                                word.MoveNext();
                                checker.DefaultDisable = Expressions.Expression.ParseCreate(word, checker);
                                if (word.Text == ";")
                                {
                                    word.Color(CodeDrawStyle.ColorType.Keyword);
                                    word.MoveNext();
                                }
                            }
                        }
                        else
                        {
                            word.AddError("illegal statement");
                            word.MoveNext();
                        }
                        break;

                    // clocking_declaration
                    case "clocking":
                    case "global":
                        var clocking = Clocking.ParseCreate(word, checker, null);
                        if (clocking != null && !string.IsNullOrEmpty(clocking.Name))
                        {
                            if (!checker.NamedElements.ContainsKey(clocking.Name))
                            {
                                checker.NamedElements.Add(clocking.Name, clocking);
                            }
                        }
                        break;

                    // covergroup_declaration
                    case "covergroup":
                        var covergroup = Coverage.CovergroupDeclaration.ParseCreate(word, checker);
                        if (covergroup != null && !string.IsNullOrEmpty(covergroup.Name))
                        {
                            if (!checker.NamedElements.ContainsKey(covergroup.Name))
                            {
                                checker.NamedElements.Add(covergroup.Name, covergroup);
                            }
                        }
                        break;

                    // genvar_declaration
                    case "genvar":
                        DataObjects.Variables.Genvar.ParseCreateFromDeclaration(word, checker);
                        break;

                    // data_declaration or variable declaration
                    case "const":
                    case "var":
                    case "static":
                    case "virtual":
                    case "rand":
                    case "randc":
                    case "byte":
                    case "shortint":
                    case "int":
                    case "longint":
                    case "integer":
                    case "time":
                    case "bit":
                    case "logic":
                    case "reg":
                    case "wire":
                    case "tri":
                    case "string":
                    case "event":
                    case "real":
                    case "realtime":
                        DataObjects.Variables.Variable.ParseDeclaration(word, checker);
                        break;

                    // property_declaration
                    case "property":
                        var property = Property.PropertyDeclaration.ParseCreate(word, checker);
                        if (property != null && !string.IsNullOrEmpty(property.Name))
                        {
                            if (!checker.NamedElements.ContainsKey(property.Name))
                            {
                                checker.NamedElements.Add(property.Name, property);
                            }
                        }
                        break;

                    // sequence_declaration
                    case "sequence":
                        var sequence = Sequence.SequenceDeclaration.ParseCreate(word, checker);
                        if (sequence != null && !string.IsNullOrEmpty(sequence.Name))
                        {
                            if (!checker.NamedElements.ContainsKey(sequence.Name))
                            {
                                checker.NamedElements.Add(sequence.Name, sequence);
                            }
                        }
                        break;

                    // let_declaration
                    case "let":
                        var letDecl = DataObjects.LetDeclaration.ParseCreate(word, checker);
                        if (letDecl != null && !string.IsNullOrEmpty(letDecl.Name))
                        {
                            if (!checker.NamedElements.ContainsKey(letDecl.Name))
                            {
                                checker.NamedElements.Add(letDecl.Name, letDecl);
                            }
                        }
                        break;

                    // checker_generate_item - loop/conditional generate
                    case "for":
                    case "if":
                    case "case":
                        word.AddError("generate constructs in checker not fully implemented");
                        word.SkipToKeyword(";");
                        if (word.Text == ";") word.MoveNext();
                        break;

                    // end of declaration
                    case ";":
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        break;

                    // attribute instance
                    case "(*":
                        var attr = Attribute.ParseCreate(word, checker);
                        // Continue to next token
                        break;

                    default:
                        if (word.Text == "endchecker")
                        {
                            break;
                        }
                        // Unknown item, skip to semicolon
                        word.AddError("illegal checker item");
                        if (!word.SkipToKeyword(";"))
                        {
                            word.MoveNext();
                        }
                        else if (word.Text == ";")
                        {
                            word.MoveNext();
                        }
                        break;
                }

                // Check for cancel token
                word.CheckCancelToken();
            }
        }

        public override List<string> GetExitKeywords()
        {
            return new List<string> { "endchecker" };
        }

        public override void AppendAutoCompleteItem(List<AutocompleteItem> items)
        {
            base.AppendAutoCompleteItem(items);

            // Add ports as autocomplete items
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

    /// <summary>
    /// Helper class for parsing functions and tasks in checker
    /// </summary>
    internal static class FunctionOrTask
    {
        public static void Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "function":
                    Verilog.Function.ParseFunctionOrConstructor(word, nameSpace);
                    break;
                case "task":
                    Verilog.Task.Parse(word, nameSpace);
                    break;
            }
        }
    }
}
