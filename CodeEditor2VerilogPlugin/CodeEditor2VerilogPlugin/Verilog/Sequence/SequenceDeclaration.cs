using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Sequence
{
    /// <summary>
    /// Represents a sequence declaration
    /// sequence_declaration ::=
    ///     "sequence" sequence_identifier [ "(" [ sequence_port_list ] ")" ] ";"
    ///     { assertion_variable_declaration }
    ///     sequence_expr [ ";" ]
    ///     "endsequence" [ ":" sequence_identifier ]
    /// 
    /// sequence_port_list ::=
    ///     sequence_port_item {, sequence_port_item}
    /// 
    /// sequence_port_item ::=
    ///     { attribute_instance } [ local [ sequence_lvar_port_direction ] ] sequence_formal_type
    ///     formal_port_identifier {variable_dimension} [ = sequence_actual_arg ]
    /// 
    /// sequence_lvar_port_direction ::= input | inout | output
    /// 
    /// sequence_formal_type ::=
    ///     data_type_or_implicit
    ///     | sequence
    ///     | untyped
    /// 
    /// sequence_actual_arg ::=
    ///     event_expression
    ///     | sequence_expr
    /// 
    /// assertion_variable_declaration ::=
    ///     var_data_type list_of_variable_decl_assignments ;
    /// </summary>
    public class SequenceDeclaration : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Sequence port list (optional)
        /// </summary>
        public List<SequencePortItem> Ports { get; set; } = new List<SequencePortItem>();

        /// <summary>
        /// Assertion variable declarations inside the sequence
        /// </summary>
        public List<DataObjects.Variables.Variable> Variables { get; set; } = new List<DataObjects.Variables.Variable>();

        /// <summary>
        /// The sequence expression body
        /// </summary>
        public SequenceExpr? SequenceExpression { get; set; }

        /// <summary>
        /// Optional end label
        /// </summary>
        public string? EndLabel { get; set; }

        /// <summary>
        /// Parse a sequence declaration
        /// </summary>
        public static async Task<SequenceDeclaration?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "sequence")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // sequence

            // sequence_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("sequence identifier expected");
                word.SkipToKeyword("endsequence");
                if (word.Text == "endsequence") word.MoveNext();
                return null;
            }

            SequenceDeclaration sequenceDecl = new SequenceDeclaration
            {
                Name = word.Text
            };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse optional port list
            if (word.Text == "(")
            {
                word.MoveNext();

                while (!word.Eof && word.Text != ")")
                {
                    SequencePortItem? portItem = SequencePortItem.ParseCreate(word, nameSpace);
                    if (portItem != null)
                    {
                        sequenceDecl.Ports.Add(portItem);
                    }
                    else if (word.Text == ")")
                    {
                        break;
                    }
                    else
                    {
                        // Skip invalid token
                        word.MoveNext();
                    }

                    // Handle comma separator
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            // Expect semicolon after port list
            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // Parse assertion_variable_declarations
            // assertion_variable_declaration ::= var_data_type list_of_variable_decl_assignments ;
            while (!word.Eof && word.Text != "endsequence")
            {
                // Check for assertion_variable_declaration
                // assertion_variable_declaration ::= var_data_type list_of_variable_decl_assignments ;
                // var_data_type ::= data_type | var [ data_type_or_implicit ]

                if (word.Text == "var")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }

                // Try to parse as data declaration (for assertion variables)
                //if (DataObjects.Variables.Variable.IsVariableDeclarationStart(word))
                {
                    // Try to parse variable declaration
                    var variables = DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace,
                        (obj) =>
                        {
                            DataObjects.Variables.Variable? variable = obj as DataObjects.Variables.Variable;
                            if (variable != null) sequenceDecl.Variables.Add(variable);
                        }
                    );

                }

                // Try to parse sequence expression
                // sequence_expr starts with: @, ##, first_match, (, expression, identifier (for sequence_instance)
                if (word.Text == "@" || word.Text == "##" ||
                    word.Text == "first_match" || word.Text == "(" ||
                    word.Text == "and" || word.Text == "or" || word.Text == "intersect" ||
                    word.Text == "throughout" || word.Text == "within" ||
                    word.Text == "strong" || word.Text == "weak" ||
                    General.IsIdentifier(word.Text))
                {
                    sequenceDecl.SequenceExpression = SequenceExpr.ParseCreate(word, nameSpace);
                    break;
                }

                // Move to next token if we haven't parsed anything
                word.MoveNext();
            }

            // Optional semicolon after sequence_expr
            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // endsequence
            if (word.Text == "endsequence")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Check for optional : sequence_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == sequenceDecl.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        sequenceDecl.EndLabel = word.Text;
                        word.MoveNext();
                    }
                }
            }

            return sequenceDecl;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }

        public void DisposeSubReference()
        {
            //foreach (var v in Variables)
            //{
            //    v.DisposeSubReference();
            //}
        }
    }

    /// <summary>
    /// Represents a sequence port item
    /// sequence_port_item ::=
    ///     { attribute_instance } [ local [ sequence_lvar_port_direction ] ] sequence_formal_type
    ///     formal_port_identifier {variable_dimension} [ = sequence_actual_arg ]
    /// 
    /// sequence_lvar_port_direction ::= input | inout | output
    /// 
    /// sequence_formal_type ::=
    ///     data_type_or_implicit
    ///     | sequence
    ///     | untyped
    /// </summary>
    public class SequencePortItem
    {
        /// <summary>
        /// Whether this port is declared as local
        /// </summary>
        public bool IsLocal { get; set; }

        /// <summary>
        /// Port direction (input, inout, or output)
        /// </summary>
        public string Direction { get; set; } = "input";

        /// <summary>
        /// Formal type name (if using "sequence", "property", or "untyped" keywords)
        /// </summary>
        public string? FormalTypeName { get; set; }

        /// <summary>
        /// Data type (for data_type_or_implicit)
        /// </summary>
        public DataObjects.DataTypes.IDataType? DataType { get; set; }

        /// <summary>
        /// Port identifier
        /// </summary>
        public string Identifier { get; set; } = "";

        /// <summary>
        /// Variable dimensions (for arrays)
        /// </summary>
        public List<Expression> Dimensions { get; set; } = new List<Expression>();

        /// <summary>
        /// Default value expression (sequence_actual_arg)
        /// Can be event_expression or sequence_expr
        /// </summary>
        public Expression? DefaultValue { get; set; }

        public static SequencePortItem? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SequencePortItem portItem = new SequencePortItem();

            // Parse optional attributes
            while (word.Text == "(*)")
            {
                Attribute.ParseCreate(word, nameSpace);
            }

            // Parse optional "local" keyword
            if (word.Text == "local")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                portItem.IsLocal = true;
                word.MoveNext();

                // Parse optional direction (input, inout, output)
                if (word.Text == "input" || word.Text == "inout" || word.Text == "output")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    portItem.Direction = word.Text;
                    word.MoveNext();
                }
            }

            // Parse formal type: sequence | property | untyped | data_type_or_implicit
            if (word.Text == "sequence" || word.Text == "property" || word.Text == "untyped")
            {
                portItem.FormalTypeName = word.Text;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                // Parse data_type_or_implicit
                portItem.DataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace,null);
            }

            // Port identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("formal port identifier expected");
                return null;
            }

            portItem.Identifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse variable dimensions
            while (word.Text == "[")
            {
                word.MoveNext();
                Expression? dimExpr = Expression.ParseCreate(word, nameSpace);
                if (dimExpr != null)
                {
                    portItem.Dimensions.Add(dimExpr);
                }
                if (word.Text == "]")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("] expected");
                    break;
                }
            }

            // Parse optional default value (= sequence_actual_arg)
            if (word.Text == "=")
            {
                word.MoveNext();
                portItem.DefaultValue = Expression.ParseCreate(word, nameSpace);
            }

            return portItem;
        }
    }
}
