using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Verilog.DataObjects
{
    public class AssignmentPattern : Expressions.Expression
    {
        /*
        assignment_pattern ::=
              '{ expression { , expression } }
            | '{ structure_pattern_key : expression { , structure_pattern_key : expression } }
            | '{ array_pattern_key : expression { , array_pattern_key : expression } }
            | '{ constant_expression { expression { , expression } } }
        
        structure_pattern_key ::= member_identifier | assignment_pattern_key
        
        array_pattern_key ::= constant_expression | assignment_pattern_key
        
        assignment_pattern_key ::= simple_type | "default"
        */

        /*
        constant_assignment_pattern_expression ::= assignment_pattern_expression

        assignment_pattern_expression ::=
            [ assignment_pattern_expression_type ] assignment_pattern

        assignment_pattern_expression_type ::=
              ps_type_identifier
            | ps_parameter_identifier
            | integer_atom_type
            | type_reference

        simple_type ::= integer_type | non_integer_type | ps_type_identifier | ps_parameter_identifier
        */

        /* Expression left-side values
        
        net_lvalue ::=
            ps_or_hierarchical_net_identifier constant_select
            | { net_lvalue { , net_lvalue } }
            | [ assignment_pattern_expression_type ] assignment_pattern_net_lvalue
        
        variable_lvalue ::=
            [ implicit_class_handle . | package_scope ] hierarchical_variable_identifier select46
            | { variable_lvalue { , variable_lvalue } }
            | [ assignment_pattern_expression_type ] assignment_pattern_variable_lvalue
            | streaming_concatenation

        nonrange_variable_lvalue ::=
            [ implicit_class_handle . | package_scope ] hierarchical_variable_identifier nonrange_select  
        
        assignment_pattern_variable_lvalue ::=
            '{ variable_lvalue {, variable_lvalue } }

        左辺（Lvalue）として使用する際の重要な制約ルール
        代入パターンを左辺で使用する場合、IEEE Std 1800-2017 では以下の制約が定められています。
        位置指定表記（Positional Notation）が必須:
        左辺では、member:value や default: などのキー指定構文は使用できず、要素の並び順による位置指定表記（'{a, b, c}）のみが許可されます。
        ビットストリーム型とビット数の一致:
        左辺の各要素（a, b, c）は、右辺（または指定された型）の対応する要素と代入互換性があり、かつ同じビット数を持つ bit-stream data type である必要があります。
        */


        protected AssignmentPattern()
        {

        }
        public static new AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, false);
        }
        public static new AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue)
        {
            return ParseCreate(word, nameSpace, lValue, false);
        }

        /// <summary>
        /// patternMode : accept pattern elements (".v" pattern variables / nested tagged patterns)
        /// used in case ... matches patterns (SystemVerilog 12.6)
        /// </summary>
        public static AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue, bool patternMode)
        {
            return ParseCreate(word, nameSpace, lValue, patternMode, null);
        }

        /// <summary>
        /// Parse an assignment pattern.
        /// <paramref name="lhs"/> is the (already resolved) data object of the
        /// assignment target. The detailed syntax check against it is performed
        /// here, while parsing, so that the check is part of the parse instead of
        /// a separate pass over the finished pattern
        /// (IEEE 1800-2017 10.9, 7.9.11, 7.2.2, 5.10).
        /// Pass null when no target data object is available at parse time
        /// (typed pattern via cast, case pattern, nested pattern, ...).
        /// </summary>
        public static AssignmentPattern ParseCreate(WordScanner word, NameSpace nameSpace, bool lValue, bool patternMode, DataObject? lhs)
        {
            AssignmentPattern assignmentPattern;
            WordReference beginReference = word.GetReference();

            // Called either at "'{..." (identifier path) or at "{..." (after assignment_pattern_expression_type
            // consumed by Cast.ParseCreate : e.g. int'{1, 2, 3} / type_t'{a:1, b:2})
            if (word.Text == "'")
            {
                word.MoveNext();
            }
            if (word.Text != "{")
            {
                word.AddError("illegal assignment pattern");
                return new AssignmentPatternWithoutKey();
            }
            word.MoveNext();

            if (word.NextText == ":")
            {
                if (lValue == true)
                {
                    word.AddError("assignment pattern cannot used for left side of assignment");
                }
                assignmentPattern = AssignmentPatternWithKey.parseCreate(word, nameSpace, patternMode);
            }
            else
            {
                assignmentPattern = AssignmentPatternWithoutKey.parseCreate(word, nameSpace, patternMode);
            }

            if (word.Text == "}")
            {
                // whole pattern range ("'{ ... }"), used as the diagnostic
                // position of the detailed check below
                assignmentPattern.Reference = WordReference.CreateReferenceRange(beginReference, word.GetReference());
                word.MoveNext();
            }
            else
            {
                word.AddError("} required");
            }

            // detailed check against the assignment target, performed here while
            // parsing (not as a later pass over the finished pattern).
            // Nested patterns are parsed without a lhs (null) and are therefore
            // validated only through their outermost pattern.
            if (!patternMode)
            {
                assignmentPattern.CheckPattern(word, lhs);
            }

            return assignmentPattern;
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("'{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            if (this is AssignmentPatternWithKey withKey)
            {
                bool first = true;
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    if (!first) label.AppendText(", ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    first = false;
                    if (keyExpression.Key == "default") label.AppendText("default", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    else label.AppendText(keyExpression.Key, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
                    label.AppendText(" : ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    keyExpression.Expression?.AppendLabel(label);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                bool first = true;
                foreach (var expression in withoutKey.Expressions)
                {
                    if (!first) label.AppendText(", ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
                    first = false;
                    expression?.AppendLabel(label);
                }
            }
            label.AppendText("}", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            AppendString(sb);
            return sb.ToString();
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append("'{");
            if (this is AssignmentPatternWithKey withKey)
            {
                bool first = true;
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    if (!first) stringBuilder.Append(", ");
                    first = false;
                    stringBuilder.Append(keyExpression.Key + " : ");
                    keyExpression.Expression?.AppendString(stringBuilder);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                bool first = true;
                foreach (var expression in withoutKey.Expressions)
                {
                    if (!first) stringBuilder.Append(", ");
                    first = false;
                    expression?.AppendString(stringBuilder);
                }
            }
            stringBuilder.Append("}");
        }

    /// <summary>
    /// repetition element of assignment pattern : '{ count { element } }
    /// e.g. '{4{1'b0}}
    /// </summary>
    public class RepeatedExpression : Expressions.Expression
    {
        public required Expressions.Expression Count;
        public required Expressions.Expression Element;

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("'{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            Count?.AppendLabel(label);
            label.AppendText("{", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
            Element?.AppendLabel(label);
            label.AppendText("}", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Normal));
        }

        public override string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            AppendString(sb);
            return sb.ToString();
        }

        public override void AppendString(StringBuilder stringBuilder)
        {
            stringBuilder.Append("'{");
            Count?.AppendString(stringBuilder);
            stringBuilder.Append("{");
            Element?.AppendString(stringBuilder);
            stringBuilder.Append("}");
        }

        public override void AppendRefrencedDataObjects(List<DataObject> referencedObjects)
        {
            Count?.AppendRefrencedDataObjects(referencedObjects);
            Element?.AppendRefrencedDataObjects(referencedObjects);
        }
    }

        public override void AppendRefrencedDataObjects(List<DataObject> referencedObjects)
        {
            if (this is AssignmentPatternWithKey withKey)
            {
                foreach (var keyExpression in withKey.KeyExpressions)
                {
                    keyExpression.Expression?.AppendRefrencedDataObjects(referencedObjects);
                }
            }
            else if (this is AssignmentPatternWithoutKey withoutKey)
            {
                foreach (var expression in withoutKey.Expressions)
                {
                    expression?.AppendRefrencedDataObjects(referencedObjects);
                }
            }
        }

        /// <summary>
        /// Detailed check of the assignment pattern against the resolved
        /// assignment target data object / data type
        /// (IEEE 1800-2017 10.9, 7.9.11, 7.2.2, 5.10):
        ///   - a positional pattern for a structure supplies one element per member,
        ///   - a keyed pattern uses structure member names (or "default"),
        ///   - a pattern for an array supplies one element per array element; an
        ///     associative array pattern uses index / "default" keys, and
        ///   - a scalar / vector target accepts only a single element.
        /// This is called from <see cref="ParseCreate(WordScanner, NameSpace, bool, bool, DataObject?)"/>
        /// while the pattern is being parsed, with the already resolved target
        /// data object (blocking / nonblocking assignment target, declared
        /// variable). It is not a post-parse pass over the finished pattern.
        /// </summary>
        private void CheckPattern(WordScanner word, DataObject? lhs)
        {
            if (word.Prototype || word.CompletionContext != null) return;
            if (lhs == null) return;

            DataTypes.IDataType? lhsType = lhs.DataType;
            int guard = 0;
            while (lhsType is DataTypes.UserDefinedType userDefinedType && guard++ < 32)
            {
                lhsType = userDefinedType.OriginalDataType;
            }

            bool hasUnpackedArray = lhs.UnpackedArrays.Count != 0;
            bool isAssociativeArray = lhs is Arrays.AssociativeArray;
            bool isDynamicArray = lhs is Arrays.DynamicArray;
            bool isQueue = lhs is Arrays.Queue;

            // structure (or union) target, not an array of structures
            if (lhsType is DataTypes.StructType structType && !hasUnpackedArray)
            {
                if (this is AssignmentPatternWithKey structWithKey)
                {
                    checkStructurePatternKeys(structWithKey, structType);
                }
                else if (this is AssignmentPatternWithoutKey structWithoutKey)
                {
                    if (!containsRepetition(structWithoutKey.Expressions)
                        && structWithoutKey.Expressions.Count != structType.Members.Count)
                    {
                        addPatternError(word,
                            "assignment pattern element count (" + structWithoutKey.Expressions.Count +
                            ") does not match the structure member count (" + structType.Members.Count + ")");
                    }
                }
                return;
            }

            // unpacked array / associative array / dynamic array / queue target
            if (hasUnpackedArray || isAssociativeArray || isDynamicArray || isQueue)
            {
                if (this is AssignmentPatternWithKey)
                {
                    // A keyed array pattern may use constant_expression index keys
                    // or assignment_pattern_key ("default" / simple_type)
                    // (IEEE 1800-2017 10.9), so the keys are not restrited here.
                }
                else if (this is AssignmentPatternWithoutKey arrayWithoutKey)
                {
                    if (containsRepetition(arrayWithoutKey.Expressions)) return;

                    if (isAssociativeArray)
                    {
                        // an associative array assignment pattern is built from
                        // index / "default" keys, not from positional elements
                        // (IEEE 1800-2017 7.9.11)
                        if (arrayWithoutKey.Expressions.Count != 0)
                        {
                            addPatternError(word,
                                "an associative array assignment pattern requires a 'default' or index key");
                        }
                        return;
                    }

                    // An array of structures requires a (nested) assignment
                    // pattern per element. The C-like flattened form
                    // ("ms_t ms[1:0] = '{0, 0, 1, 1};") supplies more elements
                    // than a single structure has members and is illegal
                    // (IEEE 1800-2017 5.10 / 10.9).
                    if (lhsType is DataTypes.StructType structElementType && hasUnpackedArray)
                    {
                        int memberCount = getFlattenedMemberCount(structElementType);
                        if (arrayWithoutKey.Expressions.Count > memberCount)
                        {
                            addPatternError(word,
                                "an array of structs shall be assigned with a nested assignment pattern per element");
                        }
                        return;
                    }

                    int? size = null;
                    if (hasUnpackedArray) size = lhs.UnpackedArrays[0].Size;
                    else if (lhs is Arrays.Queue queue) size = queue.Size;

                    if (size != null && arrayWithoutKey.Expressions.Count != size)
                    {
                        addPatternError(word,
                            "assignment pattern element count (" + arrayWithoutKey.Expressions.Count +
                            ") does not match the array size (" + size + ")");
                    }
                }
                return;
            }

            // scalar / vector target
            if (this is AssignmentPatternWithoutKey scalarWithoutKey)
            {
                if (!containsRepetition(scalarWithoutKey.Expressions) && scalarWithoutKey.Expressions.Count > 1)
                {
                    addPatternError(word,
                        "too many elements in an assignment pattern for a scalar / vector type");
                }
            }
            else if (this is AssignmentPatternWithKey scalarWithKey)
            {
                foreach (var keyExpression in scalarWithKey.KeyExpressions)
                {
                    if (!IsStructurePatternKeyCandidate(keyExpression.Key)) continue;
                    keyExpression.KeyReference.AddError(
                        "illegal assignment pattern key for a scalar / vector type");
                    break;
                }
            }
        }

        /// <summary>
        /// assignment_pattern_key ::= simple_type | "default"
        /// simple_type ::= integer_type | non_integer_type | ps_type_identifier
        ///               | ps_parameter_identifier
        /// These keys name a data type rather than a structure member.
        /// </summary>
        private static readonly HashSet<string> simpleTypeKeywords = new HashSet<string>()
        {
            // integer_vector_type
            "bit", "logic", "reg",
            // integer_atom_type
            "byte", "shortint", "int", "longint", "integer", "time",
            // non_integer_type
            "shortreal", "real", "realtime",
        };

        /// <summary>
        /// True when the pattern key is a plain member identifier that may serve
        /// as a structure_pattern_key (member_identifier). Number / bracket index
        /// keys, "default" and the assignment_pattern_key simple types
        /// ("int", "bit", ...) are not member identifiers
        /// (IEEE 1800-2017 10.9).
        /// </summary>
        private static bool IsStructurePatternKeyCandidate(string key)
        {
            if (key == "default") return false;
            if (!General.IsSimpleIdentifier(key)) return false;
            if (General.ListOfKeywords.Contains(key)) return false;
            if (simpleTypeKeywords.Contains(key)) return false;
            return true;
        }

        /// <summary>
        /// Number of members of a structure, counting the members of directly
        /// nested structures (the C-like flattened element count of a struct).
        /// </summary>
        private static int getFlattenedMemberCount(DataTypes.StructType structType)
        {
            int memberCount = 0;
            foreach (DataTypes.StructType.Member member in structType.Members.Values)
            {
                memberCount += member.DatType is DataTypes.StructType nested
                    ? nested.Members.Count
                    : 1;
            }
            return memberCount;
        }

        /// <summary>
        /// Returns true when the element list contains a repetition form
        /// ('{ count { element } }), whose element count is intentionally not
        /// fixed (IEEE 1800-2017 10.9).
        /// </summary>
        private static bool containsRepetition(List<Expressions.Expression> expressions)
        {
            foreach (Expressions.Expression expression in expressions)
            {
                if (expression is RepeatedExpression) return true;
            }
            return false;
        }

        /// <summary>
        /// A structure_pattern_key shall be a member_identifier or an
        /// assignment_pattern_key ("default" / simple_type)
        /// (IEEE 1800-2017 10.9.1).
        /// </summary>
        private static void checkStructurePatternKeys(AssignmentPatternWithKey withKey, DataTypes.StructType structType)
        {
            foreach (var keyExpression in withKey.KeyExpressions)
            {
                // only plain member identifiers are checked; array index keys,
                // "default" and assignment_pattern_key simple types are legal
                if (!IsStructurePatternKeyCandidate(keyExpression.Key)) continue;
                if (structType.Members.ContainsKey(keyExpression.Key)) continue;
                keyExpression.KeyReference.AddError(
                    "'" + keyExpression.Key + "' is not a member of the structure type");
            }
        }

        private void addPatternError(WordScanner word, string message)
        {
            if (Reference != null) Reference.AddError(message);
            else word.AddError(message);
        }

    }

    public class AssignmentPatternWithKey : AssignmentPattern
    {
        public List<KeyExpression> KeyExpressions = new List<KeyExpression>();
        public class KeyExpression
        {
            public required string Key;
            public required WordReference KeyReference;
            public required Expressions.Expression Expression;
        }
        public static AssignmentPatternWithKey parseCreate(WordScanner word, NameSpace nameSpace, bool patternMode = false)
        {
            AssignmentPatternWithKey assignmentPattern = new AssignmentPatternWithKey();

            while (!word.Eof & word.Text != "}")
            {
                string key = word.Text;
                WordReference keyReference = word.GetReference();
                if (key == "default") word.Color(CodeDrawStyle.ColorType.Keyword);

               // array_pattern_key ::= constant_expression | assignment_pattern_key
               // bracket index key : e.g. '{[2]:1, [0]:0}
               if (word.Text == "[")
               {
                   word.MoveNext();
                   Expressions.Expression? indexExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                   if (indexExpression == null)
                   {
                       word.AddError("illegal array pattern key");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   if (word.Text != "]")
                   {
                       word.AddError("] required");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   word.MoveNext();
                   key = "[" + indexExpression.CreateString() + "]";
                   keyReference = WordReference.CreateReferenceRange(keyReference, word.GetReference());
               }

                word.MoveNext();

                if (word.Text != ":")
                {
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }
                word.MoveNext();

                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);

               if (expression == null && patternMode)
               {
                   // key : pattern_value form in case pattern (e.g. default : .v)
                   expression = Verilog.Expressions.CasePatternParser.ParsePattern(word, nameSpace);
               }

                if (expression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }
                KeyExpression keyExpression = new KeyExpression() { Key = key, KeyReference = keyReference, Expression = expression };

                assignmentPattern.KeyExpressions.Add(keyExpression);

                if (word.Text != ",") break;
                word.MoveNext();
            }

            return assignmentPattern;
        }

    }

    public class AssignmentPatternWithoutKey : AssignmentPattern
    {
        public List<Expressions.Expression> Expressions = new List<Expressions.Expression>();

        public static AssignmentPatternWithoutKey parseCreate(WordScanner word, NameSpace nameSpace, bool patternMode = false)
        {
            AssignmentPatternWithoutKey assignmentPattern = new AssignmentPatternWithoutKey();

            while (!word.Eof & word.Text != "}")
            {
                string key = word.Text;
                WordReference keyReference = word.GetReference();

                Expressions.Expression? expression = null;
               if (patternMode)
               {
                   // case pattern element : .v (pattern variable) / tagged ... / constant expression
                   expression = Verilog.Expressions.CasePatternParser.ParsePattern(word, nameSpace);
               }
               else
               {
                   expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
               }

               // assignment_pattern ::= '{ constant_expression { expression { , expression } } }
               // repetition form : e.g. '{4{1'b0}} / '{N{default}} / '{2{'{3{4, 5}}}}
               // the constant_expression before "{" is the repeat count, and the
               // braced list holds one or more (possibly nested assignment
               // pattern) elements.
               if (!patternMode && expression != null && word.Text == "{")
               {
                   word.MoveNext();
                   Expressions.Expression count = expression;
                   Expressions.Expression? element = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                   if (element == null)
                   {
                       word.AddError("illegal repetition element expression");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   // allow a comma separated element list inside the repetition
                   while (!word.Eof && word.GetCharAt(0) == ',')
                   {
                       word.MoveNext();
                       Expressions.Expression? extra = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                       if (extra == null)
                       {
                           word.AddError("illegal repetition element expression");
                           word.SkipToKeyword("}");
                           return assignmentPattern;
                       }
                   }
                   if (word.Text != "}")
                   {
                       word.AddError("} required");
                       word.SkipToKeyword("}");
                       return assignmentPattern;
                   }
                   word.MoveNext();
                   expression = new RepeatedExpression() { Count = count, Element = element };
               }

                if (expression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword("}");
                    return assignmentPattern;
                }

                assignmentPattern.Expressions.Add(expression);

                if (word.Text != ",") break;
                word.MoveNext();
            }

            return assignmentPattern;
        }

    }
}
