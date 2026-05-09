using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Assertion
{
    /// <summary>
    /// Represents a property instance reference (ps_or_hierarchical_property_identifier)
    /// property_instance ::=
    ///     ps_or_hierarchical_property_identifier [ ( [ property_list_of_arguments ] ) ]
    /// property_list_of_arguments ::=
    ///     [property_actual_arg] { , [property_actual_arg] } { , . identifier ( [property_actual_arg] ) }
    ///     | . identifier ( [property_actual_arg] ) { , . identifier ( [property_actual_arg] ) }
    /// property_actual_arg ::=
    ///     property_expr
    ///     | sequence_actual_arg
    /// </summary>
    public class PropertyInstance : PropertyPrimary
    {
        public string Identifier { get; set; } = "";
        public string? PackageScope { get; set; }
        public List<PropertyActualArg> Arguments { get; set; } = new List<PropertyActualArg>();

        /// <summary>
        /// Reference to the property identifier in the source code
        /// </summary>
        public WordReference? PropertyReference { get; set; }

        public static new PropertyInstance? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();

            // Parse identifier
            if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
            {
                return null;
            }

            string identifier = word.Text;
            string? packageScope = null;

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Check for :: (package scope)
            if (word.Text == "::")
            {
                packageScope = identifier;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
                {
                    word.AddError("expecting property_identifier after ::");
                    return null;
                }

                identifier = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            List<PropertyActualArg> arguments = new List<PropertyActualArg>();

            // Parse optional arguments: ( [property_list_of_arguments] )
            if (word.Text == "(")
            {
                word.MoveNext();

                // Empty argument list
                if (word.Text == ")")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    // Parse arguments
                    while (!word.Eof && word.Text != ")")
                    {
                        PropertyActualArg? arg = ParsePropertyActualArg(word, nameSpace);
                        if (arg != null)
                        {
                            arguments.Add(arg);
                        }
                        else
                        {
                            // Try to parse as property expression if argument parsing fails
                            var propExpr = PropertyExpression.ParseCreate(word, nameSpace);
                            if (propExpr != null)
                            {
                                arguments.Add(new PropertyActualArg { PropertyExpression = propExpr });
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (word.Text == ",")
                        {
                            word.MoveNext();
                            continue;
                        }
                        else if (word.Text != ")")
                        {
                            break;
                        }
                    }

                    if (word.Text == ")")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                }
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            return new PropertyInstance
            {
                Identifier = identifier,
                PackageScope = packageScope,
                PropertyReference = WordReference.CreateReferenceRange(beginRef, word.GetReference()),
                Arguments = arguments
            };
        }

        /// <summary>
        /// Try to parse a property actual argument
        /// property_actual_arg ::=
        ///     property_expr
        ///     | sequence_actual_arg
        /// </summary>
        private static PropertyActualArg? ParsePropertyActualArg(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();

            // Try to parse as property expression first
            var propExpr = PropertyExpression.ParseCreate(word, nameSpace);
            if (propExpr != null)
            {
                return new PropertyActualArg { PropertyExpression = propExpr };
            }

            // Try to parse as sequence expression
            var seqExpr = Sequence.SequenceExpr.ParseCreate(word, nameSpace);
            if (seqExpr != null)
            {
                return new PropertyActualArg { SequenceExpression = seqExpr };
            }

            return null;
        }
    }

    /// <summary>
    /// Represents an actual argument in a property instance call
    /// </summary>
    public class PropertyActualArg
    {
        public PropertyExpression? PropertyExpression { get; set; }
        public Sequence.SequenceExpr? SequenceExpression { get; set; }
    }
}
