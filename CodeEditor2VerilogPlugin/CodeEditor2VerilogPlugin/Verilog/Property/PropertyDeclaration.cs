using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Property
{
    /// <summary>
    /// Represents a property declaration
    /// property_declaration ::=
    ///     "property" property_identifier [ "(" [ property_port_list ] ")" ] ";"
    ///     { assertion_variable_declaration }
    ///     property_spec [ ";" ]
    ///     "endproperty" [ ":" property_identifier ]
    /// 
    /// property_port_list ::=
    ///     property_port_item {"," property_port_item}
    /// 
    /// property_port_item ::=
    ///     { attribute_instance } [ "local" [ property_lvar_port_direction ] ] property_formal_type
    ///     formal_port_identifier {variable_dimension} [ "=" property_actual_arg ]
    /// 
    /// property_lvar_port_direction ::= "input"
    /// 
    /// property_formal_type ::=
    ///     sequence_formal_type
    ///     | "property"
    /// 
    /// sequence_formal_type ::=
    ///     data_type_or_implicit
    ///     | sequence
    ///     | untyped
    /// 
    /// property_actual_arg ::=
    ///     property_expr
    ///     | sequence_actual_arg
    /// 
    /// assertion_variable_declaration ::=
    ///     var_data_type list_of_variable_decl_assignments ;
    /// </summary>
    public class PropertyDeclaration : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Property port list (optional)
        /// </summary>
        public List<PropertyPortItem> Ports { get; set; } = new List<PropertyPortItem>();

        /// <summary>
        /// Assertion variable declarations inside the property
        /// </summary>
        public List<DataObjects.Variables.Variable> Variables { get; set; } = new List<DataObjects.Variables.Variable>();

        /// <summary>
        /// The property specification
        /// </summary>
        public Assertion.PropertySpec? PropertySpec { get; set; }

        /// <summary>
        /// Optional end label
        /// </summary>
        public string? EndLabel { get; set; }

        /// <summary>
        /// Parse a property declaration
        /// </summary>
        public static async Task<PropertyDeclaration?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "property")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // property

            // property_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("property identifier expected");
                word.SkipToKeyword("endproperty");
                if (word.Text == "endproperty") word.MoveNext();
                return null;
            }

            PropertyDeclaration propertyDecl = new PropertyDeclaration
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
                    PropertyPortItem? portItem = PropertyPortItem.ParseCreate(word, nameSpace);
                    if (portItem != null)
                    {
                        propertyDecl.Ports.Add(portItem);
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

            // Parse assertion_variable_declarations and property_spec
            // We need to parse until we hit 'endproperty'
            while (!word.Eof && word.Text != "endproperty")
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
                // This is simplified - a full implementation would need proper var_data_type parsing
                //if (DataObjects.Variables.Variable.IsVariableDeclarationStart(word))
                {
                    // Try to parse variable declaration
                    var variables = DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace,
                        (obj) =>
                        {
                            DataObjects.Variables.Variable? variable = obj as DataObjects.Variables.Variable;
                            if(variable != null) propertyDecl.Variables.Add(variable);
                        }
                    );
                }

                // Parse property_spec
                if (word.Text == "@" || word.Text == "disable" ||
                    word.Text == "strong" || word.Text == "weak" ||
                    word.Text == "not" || word.Text == "(" ||
                    word.Text == "if" || word.Text == "case" ||
                    word.Text == "accept_on" || word.Text == "reject_on" ||
                    word.Text == "sync_accept_on" || word.Text == "sync_reject_on" ||
                    word.Text == "nexttime" || word.Text == "s_nexttime" ||
                    word.Text == "always" || word.Text == "s_always" ||
                    word.Text == "eventually" || word.Text == "s_eventually" ||
                    General.IsIdentifier(word.Text))
                {
                    propertyDecl.PropertySpec = await Assertion.PropertySpec.ParseCreate(word, nameSpace);
                    break;
                }

                // Move to next token if we haven't parsed anything
                word.MoveNext();
            }

            // Optional semicolon after property_spec
            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // endproperty
            if (word.Text == "endproperty")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Check for optional : property_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == propertyDecl.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        propertyDecl.EndLabel = word.Text;
                        word.MoveNext();
                    }
                }
            }

            return propertyDecl;
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
    /// Represents a property port item
    /// property_port_item ::=
    ///     { attribute_instance } [ "local" [ property_lvar_port_direction ] ] property_formal_type
    ///     formal_port_identifier {variable_dimension} [ "=" property_actual_arg ]
    /// </summary>
    public class PropertyPortItem
    {
        /// <summary>
        /// Whether this port is declared as local
        /// </summary>
        public bool IsLocal { get; set; }

        /// <summary>
        /// Port direction (only "input" is valid for property)
        /// </summary>
        public string Direction { get; set; } = "input";

        /// <summary>
        /// Formal type name (if using "sequence" or "property" keywords)
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
        /// Default value expression (property_actual_arg)
        /// </summary>
        public Expression? DefaultValue { get; set; }

        public static PropertyPortItem? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            PropertyPortItem portItem = new PropertyPortItem();

            // Parse optional attributes
            while (word.Text == "(*")
            {
                Attribute.ParseCreate(word, nameSpace);
            }

            // Parse optional "local" keyword
            if (word.Text == "local")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                portItem.IsLocal = true;
                word.MoveNext();

                // Parse optional direction (only "input" is valid)
                if (word.Text == "input")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    portItem.Direction = word.Text;
                    word.MoveNext();
                }
            }

            // Parse formal type: sequence | property | data_type_or_implicit
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

            // Parse optional default value
            if (word.Text == "=")
            {
                word.MoveNext();
                portItem.DefaultValue = Expression.ParseCreate(word, nameSpace);
            }

            return portItem;
        }
    }
}
