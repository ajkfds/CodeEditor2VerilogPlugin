using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Property
{
    /// <summary>
    /// Represents a property instance
    /// property_instance ::=
    ///     ps_or_hierarchical_property_identifier [ ( [ property_list_of_arguments ] ) ]
    /// 
    /// property_list_of_arguments ::=
    ///     [property_actual_arg] { , [property_actual_arg] } { , . identifier ( [property_actual_arg] ) }
    ///     | . identifier ( [property_actual_arg] ) { , . identifier ( [property_actual_arg] ) }
    /// 
    /// property_actual_arg ::=
    ///     property_expr
    ///     | sequence_actual_arg
    /// </summary>
    public class PropertyInstance : Expressions.Expression
    {
        /// <summary>
        /// The property identifier (possibly hierarchical)
        /// </summary>
        public string PropertyIdentifier { get; set; } = "";

        /// <summary>
        /// List of property/sequence actual arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new List<Expression>();

        /// <summary>
        /// Named arguments (.identifier(value))
        /// </summary>
        public Dictionary<string, Expression> NamedArguments { get; set; } = new Dictionary<string, Expression>();

        /// <summary>
        /// Reference to the property declaration (if found)
        /// </summary>
        public PropertyDeclaration? ReferencedProperty { get; set; }

        /// <summary>
        /// Parse a property instance
        /// property_instance ::= ps_or_hierarchical_property_identifier [ ( [ property_list_of_arguments ] ) ]
        /// </summary>
        public static PropertyInstance? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (!General.IsIdentifier(word.Text) && word.Text != "$root")
            {
                return null;
            }

            PropertyInstance instance = new PropertyInstance();

            // Parse property identifier (may be hierarchical with :: or . separators)
            string identifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Handle hierarchical identifiers (e.g., package::property_name or top.mod.prop)
            while (word.Text == "::" || word.Text == ".")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                identifier += word.Text;
                word.MoveNext();

                if (!General.IsIdentifier(word.Text))
                {
                    // Invalid hierarchical identifier
                    break;
                }

                identifier += word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            instance.PropertyIdentifier = identifier;

            // Parse optional argument list
            if (word.Text == "(")
            {
                word.MoveNext();

                while (!word.Eof && word.Text != ")")
                {
                    // Check for named argument (.identifier(...))
                    if (word.Text == ".")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        string namedArgName = word.Text;
                        if (!General.IsIdentifier(namedArgName))
                        {
                            word.AddError("identifier expected for named argument");
                            break;
                        }
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();

                        if (word.Text == "(")
                        {
                            word.MoveNext();
                            Expression? argExpr = Expression.ParseCreate(word, nameSpace);
                            if (argExpr != null)
                            {
                                instance.NamedArguments[namedArgName] = argExpr;
                            }
                            if (word.Text == ")")
                            {
                                word.MoveNext();
                            }
                        }
                    }
                    else
                    {
                        // Parse positional argument (property_actual_arg)
                        Expression? argExpr = Expression.ParseCreate(word, nameSpace);
                        if (argExpr != null)
                        {
                            instance.Arguments.Add(argExpr);
                        }
                    }

                    // Handle comma separator
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                    }
                    else if (word.Text == ")")
                    {
                        break;
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            return instance;
        }
    }
}
