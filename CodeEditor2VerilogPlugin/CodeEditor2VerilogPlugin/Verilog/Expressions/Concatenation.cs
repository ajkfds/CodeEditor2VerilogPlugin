using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    public class Concatenation : Primary
    {
        internal Concatenation() { }
        public List<Expression> Expressions = new List<Expression>();

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("{ ");
            bool first = true;
            foreach (Expression expression in Expressions)
            {
                if (!first)
                {
                    label.AppendText(", ");
                }
                label.AppendLabel(expression.GetLabel());
                first = false;
            }
            label.AppendText(" }");
        }

        public static Primary? ParseCreateConcatenationOrMultipleConcatenation(WordScanner word, NameSpace nameSpace, bool lValue, bool acceptImplicitNet)
        {
            WordReference reference = word.GetReference();
            word.MoveNext(); // {

            // Check for empty unpacked array concatenation: { }
            if (word.GetCharAt(0) == '}')
            {
                // empty_unpacked_array_concatenation ::= "{" "}"
                EmptyUnpackedArrayConcatenation empty = new EmptyUnpackedArrayConcatenation()
                {
                    Reference = WordReference.CreateReferenceRange(reference, word.GetReference())
                };
                word.MoveNext(); // }
                return empty;
            }

            Expression? exp1;
            if (lValue)
            {
                exp1 = Expression.ParseCreateVariableLValue(word, nameSpace, acceptImplicitNet);
            }
            else
            {
                if (acceptImplicitNet)
                {
                    exp1 = Expression.ParseCreateAcceptImplicitNet(word, nameSpace, false);
                }
                else
                {
                    exp1 = Expression.ParseCreate(word, nameSpace);
                }
            }
            if (exp1 == null)
            {
                word.AddError("illegal concatenation");
                word.SkipToKeyword(";");
                return null;
            }

            // Check for streaming concatenation: { >> or << ... }
            if (word.Text == ">>" || word.Text == "<<")
            {
                return StreamingConcatenation.ParseCreate(word, nameSpace, reference, lValue, acceptImplicitNet);
            }

            if (word.GetCharAt(0) == '{')
            {
                return MultipleConcatenation.ParseCreate(word, nameSpace, exp1, reference);
            }
            Concatenation concatenation = new Concatenation();
            concatenation.Expressions.Add(exp1);
            concatenation.Constant = exp1.Constant;
            concatenation.BitWidth = exp1.BitWidth;

            while (word.GetCharAt(0) != '}')
            {
                if (word.GetCharAt(0) != ',')
                {
                    return null;
                }
                word.MoveNext();

                if (word.Eof)
                {
                    word.AddError("illegal concatenation");
                    return null;
                }
                if (lValue)
                {
                    exp1 = Expression.ParseCreateVariableLValue(word, nameSpace, acceptImplicitNet);
                }
                else
                {
                    if (acceptImplicitNet)
                    {
                        exp1 = Expression.ParseCreateAcceptImplicitNet(word, nameSpace, false);
                    }
                    else
                    {
                        exp1 = Expression.ParseCreate(word, nameSpace);
                    }
                }
                if (exp1 != null)
                {
                    concatenation.Expressions.Add(exp1);
                    concatenation.Constant = concatenation.Constant & exp1.Constant;
                    concatenation.BitWidth = concatenation.BitWidth + exp1.BitWidth;
                }
                if (exp1 == null)
                {
                    word.AddError("illegal concatenation");
                    return null;
                }
            }
            concatenation.Reference = WordReference.CreateReferenceRange(reference, word.GetReference());
            word.MoveNext(); // }
            return concatenation;
        }

        public override void AssertAssigned()
        {
            foreach (var exp in Expressions)
            {
                exp.AssertAssigned();
            }
        }

        /* SystemVerilog 2017
        concatenation           ::= "{" expression { "," expression } "}"
        constant_concatenation  ::= "{" constant_expression { "," constant_expression } "}"
        constant_multiple_concatenation     ::= "{" constant_expression constant_concatenation "}"
        module_path_concatenation ::= "{" module_path_expression { "," module_path_expression } "}"
        module_path_multiple_concatenation ::= "{" constant_expression module_path_concatenation "}"
        multiple_concatenation ::= "{" expression concatenation "}"
        streaming_concatenation ::= "{" stream_operator [ slice_size ] stream_concatenation "}"
        stream_operator ::= ">>" | "<<"
        slice_size ::= simple_type | constant_expression
        stream_concatenation ::= "{" stream_expression { "," stream_expression } "}"
        stream_expression ::= expression [ "with" "[" array_range_expression "]" ]
        array_range_expression ::=
                expression
                | expression ":" expression
                | expression "+:" expression
                | expression "-:" expression
        empty_unpacked_array_concatenation ::= "{" "}"         
         */


        /* Verilog
        concatenation           ::= { expression { , expression } }
        multiple_concatenation  ::= { constant_expression concatenation }

        constant_concatenation  ::= { constant_expression { , constant_expression } }
        constant_multiple_concatenation ::= { constant_expression constant_concatenation }

        module_path_concatenation ::= { module_path_expression { , module_path_expression } }
        module_path_multiple_concatenation ::= { constant_expression module_path_concatenation }


        net_concatenation       ::= { net_concatenation_value { , net_concatenation_value } }
        net_concatenation_value ::= hierarchical_net_identifier
                                    | hierarchical_net_identifier [ expression ] { [ expression ] }
                                    | hierarchical_net_identifier [ expression ] { [ expression ] } [ range_expression ]
                                    | hierarchical_net_identifier [ range_expression ]
                                    | net_concatenation  

        variable_concatenation ::= { variable_concatenation_value { , variable_concatenation_value } }  
        variable_concatenation_value    ::= hierarchical_variable_identifier
                                        | hierarchical_variable_identifier [ expression ] { [ expression ] }
                                        | hierarchical_variable_identifier [ expression ] { [ expression ] } [ range_expression ]
                                        | hierarchical_variable_identifier [ range_expression ]          
                                        | variable_concatenation  
        */
    }

    /// <summary>
    /// Empty unpacked array concatenation: { }
    /// </summary>
    public class EmptyUnpackedArrayConcatenation : Primary
    {
        internal EmptyUnpackedArrayConcatenation() { }
        

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("{ }");
        }
    }

    /// <summary>
    /// Streaming concatenation: { >> or << [slice_size] stream_concatenation }
    /// </summary>
    public class StreamingConcatenation : Primary
    {
        internal StreamingConcatenation() { }

        /// <summary>
        /// Stream operator: ">>" (right shift) or "<<" (left shift)
        /// </summary>
        public string StreamOperator { get; protected set; } = "";

        /// <summary>
        /// Optional slice size (simple_type or constant_expression)
        /// </summary>
        public Expression? SliceSize { get; protected set; }

        /// <summary>
        /// List of stream expressions
        /// </summary>
        public List<StreamExpression> StreamExpressions { get; set; } = new List<StreamExpression>();

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            SliceSize?.DisposeSubReference(false);
            foreach (var expr in StreamExpressions)
            {
                expr.DisposeSubReference(false);
            }
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("{ ");
            label.AppendText(StreamOperator);
            if (SliceSize != null)
            {
                label.AppendText(" ");
                label.AppendLabel(SliceSize.GetLabel());
            }
            label.AppendText(" ");
            bool first = true;
            label.AppendText("{ ");
            foreach (var streamExpr in StreamExpressions)
            {
                if (!first)
                {
                    label.AppendText(", ");
                }
                label.AppendLabel(streamExpr.GetLabel());
                first = false;
            }
            label.AppendText(" }");
            label.AppendText(" }");
        }

        public static Primary? ParseCreate(WordScanner word, NameSpace nameSpace, WordReference reference, bool lValue, bool acceptImplicitNet)
        {
            if (word.Text != ">>" && word.Text != "<<")
            {
                return null;
            }

            StreamingConcatenation streaming = new StreamingConcatenation();
            streaming.StreamOperator = word.Text;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse optional slice_size
            // slice_size ::= simple_type | constant_expression
            // Try to parse as data type (simple_type) first, then as expression
            var dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
            if (dataType != null)
            {
                // It's a type, create a reference for it
                var sliceExpr = new Expression();
                sliceExpr.Reference = word.GetReference();
                sliceExpr.Primary = new DataTypeReference() { IDataType = dataType };
                streaming.SliceSize = sliceExpr;
            }
            else
            {
                // Try to parse as constant_expression
                var sliceExp = Expression.ParseCreate(word, nameSpace);
                if (sliceExp != null)
                {
                    streaming.SliceSize = sliceExp;
                }
            }

            // Parse stream_concatenation: { stream_expression { , stream_expression } }
            if (word.GetCharAt(0) != '{')
            {
                word.AddError("illegal streaming concatenation");
                return null;
            }
            word.MoveNext(); // {

            // Parse stream_expressions
            while (word.GetCharAt(0) != '}' && !word.Eof)
            {
                var streamExpr = StreamExpression.ParseCreate(word, nameSpace);
                if (streamExpr == null)
                {
                    word.AddError("illegal stream expression");
                    break;
                }
                streaming.StreamExpressions.Add(streamExpr);

                if (word.GetCharAt(0) == ',')
                {
                    word.MoveNext();
                }
                else if (word.GetCharAt(0) != '}')
                {
                    break;
                }
            }

            if (word.GetCharAt(0) != '}')
            {
                word.AddError("illegal streaming concatenation");
                word.SkipToKeyword("}");
                return null;
            }

            streaming.Reference = WordReference.CreateReferenceRange(reference, word.GetReference());
            word.MoveNext(); // }

            return streaming;
        }
    }

    /// <summary>
    /// Stream expression: expression [ "with" "[" array_range_expression "]" ]
    /// </summary>
    public class StreamExpression : Primary
    {
        internal StreamExpression() { }

        public Expression? Expression { get; protected set; }

        /// <summary>
        /// Optional "with" clause with array range expression
        /// </summary>
        public ArrayRangeExpression? WithArrayRange { get; protected set; }

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            Expression?.DisposeSubReference(false);
            WithArrayRange?.DisposeSubReference(false);
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (Expression != null)
            {
                label.AppendLabel(Expression.GetLabel());
            }
            if (WithArrayRange != null)
            {
                label.AppendText(" with [");
                label.AppendLabel(WithArrayRange.GetLabel());
                label.AppendText("]");
            }
        }

        public static StreamExpression ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            StreamExpression streamExpr = new StreamExpression();

            // Parse expression
            var exp = Expression.ParseCreate(word, nameSpace);
            streamExpr.Expression = exp;

            // Check for "with" clause
            if (word.Text == "with")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.GetCharAt(0) != '[')
                {
                    word.AddError("illegal stream expression");
                    return streamExpr;
                }
                word.MoveNext(); // [

                // Parse array_range_expression
                var arrayRange = ArrayRangeExpression.ParseCreate(word, nameSpace);
                streamExpr.WithArrayRange = arrayRange;

                if (word.GetCharAt(0) != ']')
                {
                    word.AddError("] expected");
                    word.SkipToKeyword("]");
                }
                else
                {
                    word.MoveNext(); // ]
                }
            }

            return streamExpr;
        }
    }

    /// <summary>
    /// Array range expression for streaming concatenation "with" clause
    /// array_range_expression ::=
    ///         expression
    ///     |   expression ":" expression
    ///     |   expression "+:" expression
    ///     |   expression "-:" expression
    /// </summary>
    public class ArrayRangeExpression : Primary
    {
        internal ArrayRangeExpression() { }

        /// <summary>
        /// First expression (always present)
        /// </summary>
        public Expression? FirstExpression { get; protected set; }

        /// <summary>
        /// Range type: null (single expression), ":", "+:", "-:"
        /// </summary>
        public string? RangeType { get; protected set; }

        /// <summary>
        /// Second expression (for range types)
        /// </summary>
        public Expression? SecondExpression { get; protected set; }

        public override void DisposeSubReference(bool keepThisReference)
        {
            base.DisposeSubReference(keepThisReference);
            FirstExpression?.DisposeSubReference(false);
            SecondExpression?.DisposeSubReference(false);
        }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (FirstExpression != null)
            {
                label.AppendLabel(FirstExpression.GetLabel());
            }
            if (RangeType != null)
            {
                label.AppendText(RangeType);
                if (SecondExpression != null)
                {
                    label.AppendLabel(SecondExpression.GetLabel());
                }
            }
        }

        public static ArrayRangeExpression ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ArrayRangeExpression arrayRange = new ArrayRangeExpression();

            // Parse first expression
            var first = Expression.ParseCreate(word, nameSpace);
            arrayRange.FirstExpression = first;

            // Check for range operators
            if (word.Text == ":")
            {
                arrayRange.RangeType = ":";
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                var second = Expression.ParseCreate(word, nameSpace);
                arrayRange.SecondExpression = second;
            }
            else if (word.Text == "+:")
            {
                arrayRange.RangeType = "+:";
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                var second = Expression.ParseCreate(word, nameSpace);
                arrayRange.SecondExpression = second;
            }
            else if (word.Text == "-:")
            {
                arrayRange.RangeType = "-:";
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                var second = Expression.ParseCreate(word, nameSpace);
                arrayRange.SecondExpression = second;
            }

            return arrayRange;
        }
    }

    public class MultipleConcatenation : Primary
    {
        internal MultipleConcatenation() { }

        public Expression? MultipleExpression { get; protected set; }
        public Expression? Expression { get; protected set; }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("{ ");
            if (MultipleExpression != null) label.AppendLabel(MultipleExpression.GetLabel());
            if (Expression != null)
            {
                label.AppendText(" ");
                label.AppendLabel(Expression.GetLabel());
            }
            label.AppendText(" }");
        }
        public static MultipleConcatenation? ParseCreate(WordScanner word, NameSpace nameSpace, Expression multipleExpression, WordReference reference)
        {
            Expression? exp = Concatenation.ParseCreate(word, nameSpace);

            if (word.Eof || word.GetCharAt(0) != '}')
            {
                word.AddError("illegal multiple concatenation");
                return null;
            }
            MultipleConcatenation multipleConcatenation = new MultipleConcatenation();
            multipleConcatenation.MultipleExpression = multipleExpression;
            multipleConcatenation.Expression = exp;
            multipleConcatenation.Reference = WordReference.CreateReferenceRange(reference, word.GetReference());
            if (exp != null) multipleConcatenation.Constant = exp.Constant & multipleConcatenation.Constant;

            if (exp != null && multipleExpression != null)
            {
                if (exp.BitWidth != null && multipleExpression.Value != null)
                {
                    multipleConcatenation.BitWidth = (int)exp.BitWidth * (int)multipleExpression.Value;
                }
                if (exp.Constant && multipleExpression.Constant)
                {
                    multipleConcatenation.Constant = true;
                }
            }

            word.MoveNext(); // }

            return multipleConcatenation;
        }

    }
}
