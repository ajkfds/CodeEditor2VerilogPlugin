using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Coverage
{
    /// <summary>
    /// Represents a covergroup declaration
    /// covergroup_declaration ::=
    ///     "covergroup" covergroup_identifier [ ( [ covergroup_range_list ] ) ] [ coverage_spec ] ;
    ///     { covergroup_or_generate_item }
    ///     "endgroup" [ : covergroup_identifier ]
    /// 
    /// covergroup_range_list ::= covergroup_range_item { , covergroup_range_item }
    /// covergroup_range_item ::= expression | constant_range
    /// 
    /// coverage_spec ::=
    ///     cover_point_coverage
    ///     | cover_cross_coverage
    /// 
    /// cover_point_coverage ::=
    ///     "coverpoint" coverpoint_identifier [ "iff" ( expression ) ] { bins_or_coverpoint}
    /// 
    /// cover_cross_coverage ::=
    ///     "cross" coverpoint_list [ "iff" ( expression ) ] { cross_body }
    /// 
    /// coverpoint_list ::= coverpoint_or_identifier { , coverpoint_or_identifier }
    /// 
    /// cross_body ::= bins_or_coverpoint | cross_set
    /// 
    /// bins_or_coverpoint ::=
    ///     [ wildcard ] bins_keyword identifier = [ "with" ( with_expression ) ] [ matches integer_number ] [ iff ( expression ) ] covergroup_transition [ ; ]
    ///     | [ wildcard ] bins_keyword identifier = cross_identifier [ [ format_string ] ] [ iff ( expression ) ] [ ; ]
    ///     | bins_keyword identifier [ iff ( expression ) ] covergroup_transition [ ; ]
    ///     | coverpoint_identifier [ iff ( expression ) ]
    /// 
    /// bins_keyword ::= bins | with | cross | ignore_bins | illegal_bins
    /// </summary>
    public class CovergroupDeclaration : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Coverage options (type_option, option)
        /// </summary>
        public CovergroupOption? Option { get; set; }

        /// <summary>
        /// Covergroup range list (constructor arguments)
        /// </summary>
        public List<Expression> RangeList { get; set; } = new List<Expression>();

        /// <summary>
        /// Coverpoints in this covergroup
        /// </summary>
        public List<Coverpoint> Coverpoints { get; set; } = new List<Coverpoint>();

        /// <summary>
        /// Crosses in this covergroup
        /// </summary>
        public List<CoverCross> Crosses { get; set; } = new List<CoverCross>();

        /// <summary>
        /// Optional end label
        /// </summary>
        public string? EndLabel { get; set; }

        /// <summary>
        /// Parse a covergroup declaration
        /// </summary>
        public static async Task<CovergroupDeclaration?> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "covergroup")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // covergroup

            // covergroup_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("covergroup identifier expected");
                word.SkipToKeyword("endgroup");
                if (word.Text == "endgroup") word.MoveNext();
                return null;
            }

            CovergroupDeclaration covergroup = new CovergroupDeclaration
            {
                Name = word.Text
            };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse optional range list (constructor arguments)
            if (word.Text == "(")
            {
                word.MoveNext();

                while (!word.Eof && word.Text != ")")
                {
                    Expression? expr = Expression.ParseCreate(word, nameSpace);
                    if (expr != null)
                    {
                        covergroup.RangeList.Add(expr);
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

            // Optional semicolon after covergroup declaration
            if (word.Text == ";")
            {
                word.MoveNext();
            }

            // Parse covergroup items until we hit 'endgroup'
            while (!word.Eof && word.Text != "endgroup")
            {
                switch (word.Text)
                {
                    // option (coverage_option)
                    case "option":
                        covergroup.Option = CovergroupOption.ParseCreate(word, nameSpace, true);
                        break;

                    case "type_option":
                        covergroup.Option = CovergroupOption.ParseCreate(word, nameSpace, false);
                        break;

                    // coverpoint
                    case "coverpoint":
                        var coverpoint = Coverpoint.ParseCreate(word, nameSpace);
                        if (coverpoint != null)
                        {
                            covergroup.Coverpoints.Add(coverpoint);
                        }
                        break;

                    // cross
                    case "cross":
                        var cross = CoverCross.ParseCreate(word, nameSpace);
                        if (cross != null)
                        {
                            covergroup.Crosses.Add(cross);
                        }
                        break;

                    // Skip other declarations (data, function, etc.) - just skip to semicolon
                    case "function":
                    case "task":
                    case "begin":
                        // Skip to end of statement
                        word.SkipToKeyword(";");
                        if (word.Text == ";") word.MoveNext();
                        break;

                    // Default: skip one token (might be data declaration or other item)
                    default:
                        word.MoveNext();
                        break;
                }
            }

            // endgroup
            if (word.Text == "endgroup")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Check for optional : covergroup_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == covergroup.Name)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        covergroup.EndLabel = word.Text;
                        word.MoveNext();
                    }
                }
            }

            return covergroup;
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
        }
    }

    /// <summary>
    /// Represents covergroup coverage options
    /// </summary>
    public class CovergroupOption
    {
        /// <summary>
        /// True for instance option, false for type_option
        /// </summary>
        public bool IsInstance { get; set; }

        /// <summary>
        /// Name-value pairs for options
        /// </summary>
        public Dictionary<string, Expression> Values { get; set; } = new Dictionary<string, Expression>();

        public static CovergroupOption? ParseCreate(WordScanner word, NameSpace nameSpace, bool isInstance)
        {
            CovergroupOption option = new CovergroupOption
            {
                IsInstance = isInstance
            };

            if (word.Text != "option" && word.Text != "type_option")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse name = expression pairs
            while (!word.Eof && word.Text != ";" && word.Text != "endgroup" && word.Text != "coverpoint" && word.Text != "cross")
            {
                if (General.IsIdentifier(word.Text))
                {
                    string name = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    if (word.Text == "=")
                    {
                        word.MoveNext();
                        Expression? expr = Expression.ParseCreate(word, nameSpace);
                        if (expr != null)
                        {
                            option.Values[name] = expr;
                        }
                    }
                }

                // Handle comma separator
                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }

            return option;
        }
    }

    /// <summary>
    /// Represents a coverpoint
    /// cover_point_coverage ::=
    ///     "coverpoint" coverpoint_identifier [ "iff" ( expression ) ] { bins_or_coverpoint }
    /// </summary>
    public class Coverpoint : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// The expression being covered (null for wildcard coverpoint)
        /// </summary>
        public Expression? Expression { get; set; }

        /// <summary>
        /// Optional iff condition
        /// </summary>
        public Expression? IffCondition { get; set; }

        /// <summary>
        /// Bins in this coverpoint
        /// </summary>
        public List<BinsDeclaration> BinsList { get; set; } = new List<BinsDeclaration>();

        public static Coverpoint? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "coverpoint")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            Coverpoint coverpoint = new Coverpoint();

            // Check for wildcard coverpoint or identifier
            if (word.Text == "wildcard")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                // wildcard coverpoint doesn't have expression
            }
            else
            {
                // Parse the expression being covered
                coverpoint.Expression = Expression.ParseCreate(word, nameSpace);
            }

            // Get coverpoint name from expression or generate one
            if (coverpoint.Expression != null)
            {
                coverpoint.Name = coverpoint.Expression.ToString();
            }
            else
            {
                // Wildcard coverpoint - use identifier as name
                if (General.IsIdentifier(word.Text))
                {
                    coverpoint.Name = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    coverpoint.Name = "_auto_coverpoint_" + coverpoint.GetHashCode();
                }
            }

            // Parse optional iff condition
            if (word.Text == "iff")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext();
                    coverpoint.IffCondition = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }
            }

            // Parse bins declarations
            while (!word.Eof && word.Text != "endgroup" && word.Text != "coverpoint" && word.Text != "cross")
            {
                var bins = BinsDeclaration.ParseCreate(word, nameSpace);
                if (bins != null)
                {
                    coverpoint.BinsList.Add(bins);
                }
                else
                {
                    // No more bins, exit loop
                    break;
                }
            }

            return coverpoint;
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
        }
    }

    /// <summary>
    /// Represents a cross coverage
    /// cover_cross_coverage ::=
    ///     "cross" coverpoint_list [ "iff" ( expression ) ] { cross_body }
    /// </summary>
    public class CoverCross : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// List of coverpoint identifiers being crossed
        /// </summary>
        public List<string> CoverpointIdentifiers { get; set; } = new List<string>();

        /// <summary>
        /// Optional iff condition
        /// </summary>
        public Expression? IffCondition { get; set; }

        /// <summary>
        /// Bins in this cross
        /// </summary>
        public List<BinsDeclaration> BinsList { get; set; } = new List<BinsDeclaration>();

        public static CoverCross? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "cross")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            CoverCross cross = new CoverCross();

            // Parse coverpoint list
            while (!word.Eof && word.Text != ";" && word.Text != "endgroup" && word.Text != "coverpoint" && word.Text != "cross")
            {
                if (General.IsIdentifier(word.Text))
                {
                    cross.CoverpointIdentifiers.Add(word.Text);
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }

            // Generate cross name from coverpoint identifiers
            cross.Name = string.Join("_x_", cross.CoverpointIdentifiers);

            // Parse optional iff condition
            if (word.Text == "iff")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext();
                    cross.IffCondition = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }
            }

            // Parse bins declarations
            while (!word.Eof && word.Text != "endgroup" && word.Text != "coverpoint" && word.Text != "cross")
            {
                var bins = BinsDeclaration.ParseCreate(word, nameSpace);
                if (bins != null)
                {
                    cross.BinsList.Add(bins);
                }
                else
                {
                    // No more bins, exit loop
                    break;
                }
            }

            return cross;
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
        }
    }

    /// <summary>
    /// Represents a bins declaration
    /// bins_or_coverpoint ::=
    ///     [ wildcard ] bins_keyword identifier = [ "with" ( with_expression ) ] [ matches integer_number ] [ iff ( expression ) ] covergroup_transition [ ; ]
    ///     | [ wildcard ] bins_keyword identifier = cross_identifier [ [ format_string ] ] [ iff ( expression ) ] [ ; ]
    ///     | bins_keyword identifier [ iff ( expression ) ] covergroup_transition [ ; ]
    ///     | coverpoint_identifier [ iff ( expression ) ]
    /// 
    /// bins_keyword ::= bins | with | cross | ignore_bins | illegal_bins
    /// 
    /// covergroup_transition ::= sequence_expr | set_covergroup_identifier
    /// </summary>
    public class BinsDeclaration
    {
        /// <summary>
        /// Bins keyword type
        /// </summary>
        public string BinsType { get; set; } = "bins";

        /// <summary>
        /// Whether wildcard is used
        /// </summary>
        public bool IsWildcard { get; set; }

        /// <summary>
        /// Bins identifier
        /// </summary>
        public string Identifier { get; set; } = "";

        /// <summary>
        /// Optional "with" expression
        /// </summary>
        public Expression? WithExpression { get; set; }

        /// <summary>
        /// Optional matches value
        /// </summary>
        public int? Matches { get; set; }

        /// <summary>
        /// Optional iff condition
        /// </summary>
        public Expression? IffCondition { get; set; }

        /// <summary>
        /// Optional transition expression
        /// </summary>
        public Expression? Transition { get; set; }

        /// <summary>
        /// Optional cross reference
        /// </summary>
        public string? CrossReference { get; set; }

        /// <summary>
        /// Check if text is a simple number (unsigned integer)
        /// </summary>
        private static bool IsSimpleNumber(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return int.TryParse(text, out _);
        }

        public static BinsDeclaration? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            BinsDeclaration bins = new BinsDeclaration();

            // Check for wildcard
            if (word.Text == "wildcard")
            {
                bins.IsWildcard = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            // Check for bins keyword
            if (word.Text == "bins" || word.Text == "ignore_bins" || word.Text == "illegal_bins")
            {
                bins.BinsType = word.Text;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (General.IsIdentifier(word.Text))
            {
                // This might be a coverpoint reference (not a bins declaration)
                // Return null to let caller handle it
                if (bins.IsWildcard)
                {
                    word.AddError("identifier expected after wildcard");
                }
                return null;
            }
            else
            {
                // Not a bins declaration
                return null;
            }

            // Bins identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("bins identifier expected");
                return bins;
            }

            bins.Identifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Check for = (assignment)
            if (word.Text == "=")
            {
                word.Color(CodeDrawStyle.ColorType.Normal);
                word.MoveNext();

                // Check for "with" expression
                if (word.Text == "with")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        bins.WithExpression = Expression.ParseCreate(word, nameSpace);
                        if (word.Text == ")")
                        {
                            word.MoveNext();
                        }
                    }
                }
                // Check for cross reference
                else if (General.IsIdentifier(word.Text))
                {
                    bins.CrossReference = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                // Otherwise parse as transition expression
                else
                {
                    bins.Transition = Expression.ParseCreate(word, nameSpace);
                }
            }

            // Parse optional matches value
            if (word.Text == "matches")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (IsSimpleNumber(word.Text))
                {
                    if (int.TryParse(word.Text, out int matchesValue))
                    {
                        bins.Matches = matchesValue;
                    }
                    word.Color(CodeDrawStyle.ColorType.Number);
                    word.MoveNext();
                }
            }

            // Parse optional iff condition
            if (word.Text == "iff")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext();
                    bins.IffCondition = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }
            }

            // Optional semicolon
            if (word.Text == ";")
            {
                word.MoveNext();
            }

            return bins;
        }
    }
}
