using pluginVerilog.Verilog;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Sequence
{
    /// <summary>
    /// Represents a sequence instance reference (ps_or_hierarchical_sequence_identifier)
    /// sequence_instance ::=
    ///     ps_or_hierarchical_sequence_identifier [ ( [ sequence_list_of_arguments ] ) ]
    /// sequence_list_of_arguments ::=
    ///     [sequence_actual_arg] { , [sequence_actual_arg] } { , . identifier ( [sequence_actual_arg] ) }
    ///     | . identifier ( [sequence_actual_arg] ) { , . identifier ( [sequence_actual_arg] ) }
    /// sequence_actual_arg ::=
    ///     event_expression
    ///     | sequence_expr
    /// </summary>
    public class SequenceInstance : SequencePrimary
    {
        public string Identifier { get; set; } = "";
        public string? PackageScope { get; set; }
        public List<SequenceActualArg> Arguments { get; set; } = new List<SequenceActualArg>();
        
        /// <summary>
        /// Reference to the sequence identifier in the source code
        /// </summary>
        public WordReference? SequenceReference { get; set; }

        public static new SequenceInstance? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();

            // Check for package scope (package::identifier)
            string? packageScope = null;
            string identifier = "";

            // Try to parse hierarchical reference (e.g., inst.my_seq)
            if (word.NextText == "." || word.Text == ".")
            {
                // This is handled as hierarchical identifier
                // For now, we parse a simple identifier and let the caller handle hierarchy
            }

            // Parse package scope (::)
            if (word.Text == "::" || word.NextText == "::")
            {
                // We're at ::, which means we need to look back for package name
                word.AddError("package scope should precede identifier");
                word.MoveNext();
                return null;
            }

            // Parse identifier
            if (!General.IsIdentifier(word.Text) || General.ListOfKeywords.Contains(word.Text))
            {
                return null;
            }

            identifier = word.Text;
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
                    word.AddError("expecting sequence_identifier after ::");
                    return null;
                }

                identifier = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            List<SequenceActualArg> arguments = new List<SequenceActualArg>();
            // Parse optional arguments: ( [sequence_list_of_arguments] )
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
                        SequenceActualArg? arg = ParseSequenceActualArg(word, nameSpace);
                        if (arg != null)
                        {
                            arguments.Add(arg);
                        }
                        else
                        {
                            // Try to parse as expression if sequence arg fails
                            Expression? expr = Expression.ParseCreate(word, nameSpace);
                            if (expr != null)
                            {
                                arguments.Add(new SequenceActualArg { Expression = expr });
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

            return new SequenceInstance
            {
                Identifier = identifier,
                PackageScope = packageScope,
                SequenceReference = WordReference.CreateReferenceRange(beginRef, word.GetReference()),
                Arguments = arguments
            };
        }

        /// <summary>
        /// Try to parse a sequence actual argument
        /// sequence_actual_arg ::=
        ///     event_expression
        ///     | sequence_expr
        /// </summary>
        private static SequenceActualArg? ParseSequenceActualArg(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();

            // Try to parse as sequence expression first
            var seqExpr = SequenceExpr.ParseCreate(word, nameSpace);
            if (seqExpr != null)
            {
                return new SequenceActualArg { SequenceExpression = seqExpr };
            }

            return null;
        }
    }

    /// <summary>
    /// Represents an actual argument in a sequence instance call
    /// </summary>
    public class SequenceActualArg
    {
        public Expressions.Expression? Expression { get; set; }
        public SequenceExpr? SequenceExpression { get; set; }
    }
}
