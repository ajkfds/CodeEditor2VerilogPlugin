using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    /*
     * SystemVerilog 1800-2017 12.6 pattern matching
     *   case_pattern_item  ::= pattern [ &&& expression ] : statement_or_null
     *                        | "default" [ : ] statement_or_null
     *   pattern            ::= constant_expression | assignment_pattern | tagged_pattern
     *   tagged_pattern     ::= tagged [ unique ] union_member_identifier [ pattern ]
     *   assignment_pattern ::= '{ pattern { , pattern } }
     *                        | '{ assignment_pattern_key : pattern { , assignment_pattern_key : pattern } }
     *   pattern variable   ::= . member_identifier  (e.g. '{.v1, .v2})
     */
    /// <summary>
    /// pattern variable : ".v" in a case pattern. The referenced identifier is
    /// implicitly declared (bound) by the pattern match.
    /// </summary>
    public class PatternVariable : Primary
    {
        public string? VariableName { get; protected set; }
        public DataObjects.DataObject? BoundVariable { get; protected set; }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(".", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (VariableName != null) label.AppendText(VariableName, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable));
        }

        public override string CreateString()
        {
            return "." + (VariableName ?? "");
        }

        /// <summary>
        /// Parse ". member_identifier" (pattern variable).
        /// </summary>
        public static PatternVariable? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();
            word.MoveNext(); // "."

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("pattern variable identifier expected");
                return null;
            }

            PatternVariable patternVariable = new PatternVariable()
            {
                VariableName = word.Text,
                Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference()),
            };
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            // bind the pattern variable as an implicit variable so that the
            // display statement after ":" can reference it without "unfound object"
            if (nameSpace != null && patternVariable.VariableName != null && !nameSpace.NamedElements.ContainsKey(patternVariable.VariableName))
            {
                // pattern variable has no explicit type declaration; bind as a 1-bit wide variable
               DataObjects.DataTypes.IDataType dataType = DataObjects.DataTypes.BitType.Create(false, null);
                DataObjects.DataObject dataObject = DataObjects.Variables.Variable.Create(patternVariable.VariableName, dataType);
                {
                    dataObject.Defined = true;
                    dataObject.DefinedReference = WordReference.CreateReferenceRange(beginRef, patternVariable.Reference);
                    if (word.Prototype)
                    {
                        nameSpace.NamedElements.Add(dataObject.Name, dataObject);
                    }
                    else
                    {
                        if (nameSpace.NamedElements.ContainsKey(dataObject.Name))
                        {
                            nameSpace.NamedElements.RemoveKey(dataObject.Name);
                        }
                        nameSpace.NamedElements.Add(dataObject.Name, dataObject);
                    }
                    patternVariable.BoundVariable = dataObject;
                }
            }

            return patternVariable;
        }
    }

    /// <summary>
    /// tagged_pattern ::= tagged [ unique ] union_member_identifier [ pattern ]
    /// e.g. tagged a '{.v, 0} / tagged c '{0, .v} / tagged Invalid / tagged Valid(.v)
    /// </summary>
    public class TaggedPattern : Primary
    {
        public string? MemberIdentifier { get; protected set; }
        public Expressions.Expression? MemberPattern { get; protected set; }
        public List<Expressions.Expression> Expressions = new List<Expressions.Expression>();

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("tagged", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (MemberIdentifier != null) label.AppendText(" " + MemberIdentifier, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            if (MemberPattern != null)
            {
                label.AppendText(" ");
                label.AppendLabel(MemberPattern.GetLabel());
            }
        }

        public override string CreateString()
        {
            string ret = "tagged";
            if (MemberIdentifier != null) ret += " " + MemberIdentifier;
            if (MemberPattern != null) ret += " " + MemberPattern.CreateString();
            return ret;
        }

        /// <summary>
        /// Parse "tagged [ unique ] union_member_identifier [ pattern ]".
        /// </summary>
        public static TaggedPattern? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            WordReference beginRef = word.GetReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // "tagged"

            TaggedPattern taggedPattern = new TaggedPattern();

            if (word.Text == "unique")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("union member identifier expected");
                return null;
            }

            taggedPattern.MemberIdentifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            // [ pattern ] : assignment pattern '{...} / pattern variable ".x" / constant expression
            Expressions.Expression? pattern = CasePatternParser.ParsePattern(word, nameSpace);
            if (pattern != null)
            {
                taggedPattern.MemberPattern = pattern;
            }

            taggedPattern.Reference = WordReference.CreateReferenceRange(beginRef, word.GetReference());
            return taggedPattern;
        }
    }

    /// <summary>
    /// case pattern parsing helpers (SystemVerilog 12.6)
    /// pattern ::= constant_expression | assignment_pattern | tagged_pattern
    /// </summary>
    public static class CasePatternParser
    {
        /// <summary>
        /// Parse a case pattern. Returns null if the next token does not start a pattern.
        /// </summary>
        public static Expressions.Expression? ParsePattern(WordScanner word, NameSpace nameSpace)
        {
            if (word.Eof) return null;

            // assignment pattern : '{ ... }
            if (word.Text == "'" && word.NextText == "{")
            {
                return DataObjects.AssignmentPattern.ParseCreate(word, nameSpace, false, true);
            }

            // tagged_pattern
            if (word.Text == "tagged")
            {
                return TaggedPattern.ParseCreate(word, nameSpace);
            }

            // pattern variable ".v"
            if (word.GetCharAt(0) == '.')
            {
                return PatternVariable.ParseCreate(word, nameSpace);
            }

            // constant_expression
            return Expressions.Expression.ParseCreate(word, nameSpace);
        }

        /// <summary>
        /// Parse "{ pattern { , pattern } }" inside an assignment pattern of a case pattern.
        /// </summary>
        public static List<Expressions.Expression> ParsePatternList(WordScanner word, NameSpace nameSpace)
        {
            List<Expressions.Expression> patterns = new List<Expressions.Expression>();
            while (!word.Eof && word.Text != "}")
            {
                if (word.GetCharAt(0) == ',')
                {
                    word.MoveNext();
                    continue;
                }
                if (word.GetCharAt(0) == ':') break;

                Expressions.Expression? pattern = CasePatternParser.ParsePattern(word, nameSpace);
                if (pattern == null) break;
                patterns.Add(pattern);
            }
            return patterns;
        }
    }
}
