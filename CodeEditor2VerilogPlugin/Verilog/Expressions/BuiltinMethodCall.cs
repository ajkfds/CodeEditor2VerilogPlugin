using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace pluginVerilog.Verilog.Expressions
{
    public class BuiltinMethodCall : Primary
    {
        protected BuiltinMethodCall() { }

        public List<Expression> Expressions = new List<Expression>();
        public required string FunctionName { get; init; }

        /// <summary>
        /// The `with ( expression )` clause of an array locator / ordering method
        /// call (IEEE 1800-2017 section 7.12), when one followed the call.
        /// </summary>
        public Expression? WithExpression { get; private set; }

        //        public required NameSpace DefinedNameSpace { init; get; }

        public required BuiltInMethod BuiltInMethod { init; get; }
        [JsonIgnore]
        public required ProjectProperty ProjectProperty { init; get; }
        //public Function? Function
        //{
        //    get
        //    {
        //        Function? function = null;
        //        if (DefinedNameSpace.BuildingBlock.NamedElements.ContainsFunction(FunctionName))
        //        {
        //            function = (Function)DefinedNameSpace.BuildingBlock.NamedElements[FunctionName];
        //        }
        //        //else if (ProjectProperty.SystemFunctions.ContainsKey(FunctionName))
        //        //{
        //        //    function = ProjectProperty.SystemFunctions[FunctionName];
        //        //}
        //        return function;
        //    }
        //}

        protected new static BuiltinMethodCall? ParseCreate(WordScanner word, NameSpace usedNameSpace)
        {
            throw new NotImplementedException();
        }

        public static BuiltinMethodCall? ParseCreate(WordScanner word, NameSpace usedNameSpace, DataObjects.DataObject dataObject)
        {
            BuiltinMethodCall? methodCall = parseMethodCall(word, usedNameSpace, dataObject);
            if (methodCall == null) return null;
            parseWithClause(word, usedNameSpace, methodCall);
            return methodCall;
        }

        private static BuiltinMethodCall? parseMethodCall(WordScanner word, NameSpace usedNameSpace, DataObjects.DataObject dataObject)
        {
            if (word.RootParsedDocument.ProjectProperty == null) throw new Exception();

            if (!dataObject.NamedElements.ContainsKey(word.Text)) return null;
            INamedElement element = dataObject.NamedElements[word.Text];
            if (element is not Verilog.BuiltInMethod) return null;
            var method = (Verilog.BuiltInMethod)element;

            BuiltinMethodCall methodCall = new BuiltinMethodCall() { FunctionName = word.Text, ProjectProperty = word.ProjectProperty, BuiltInMethod = method };
            methodCall.Reference = word.GetReference();
            methodCall.BitWidth = method.ReturnVariable?.BitWidth;
            bool returnConstant = true;

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.GetCharAt(0) != '(')
            {
                if (method != null && method.Ports.Count != 0)
                {
                    word.AddError("illegal function call");
                    return null;
                }
                else
                {
                    return methodCall;
                }
            }
            word.MoveNext();

            // EOF just after "(": hint for the first argument (e.g. "obj.randomize(|")
            if (word.CompletionContext != null && word.Eof)
            {
                appendArgumentPopupItems(word.CompletionContext, method, 0);
                return methodCall;
            }

            if (word.Text == ")")
            {
                // IEEE 1800-2017 e.g. "function enum next( int unsigned N = 1 )":
                // when every remaining argument has a default value, calling the
                // method with no arguments (next() / prev()) is legal.
                if (method != null && HasUnprovidedRequiredArgument(method, 0))
                {
                    word.AddError("too few arguments");
                }
                methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference, word.GetReference());
                word.MoveNext();
                return methodCall;
            }

            int i = 0;
            while (!word.Eof)
            {
                Expression? expression = Expression.ParseCreate(word, usedNameSpace);
                if (expression == null)
                {
                    return null;
                }
                if (!expression.Constant) returnConstant = false;
                methodCall.Expressions.Add(expression);

                // EOF just after an argument expression: hint for the next argument
                if (word.CompletionContext != null && word.Eof)
                {
                    appendArgumentPopupItems(word.CompletionContext, method, i + 1);
                    return methodCall;
                }
                if (method != null)
                {
                    if (i >= method.Ports.Count)
                    {
                        expression.Reference.AddError("illegal argument");
                    }
                    else
                    {
                        if (method.PortsList[i] != null
                            && expression != null
                            && method.PortsList[i] != null
                        )
                        {
                            if (method.PortsList[i].BitWidth != expression.BitWidth)
                            {
                                word.AddWarning("bitwidth mismatch");
                            }
                        }
                    }
                }


                if (word.Text == ")")
                {
                    // i arguments were provided; the remaining ports are the ones
                    // from index (i + 1). Report only when a remaining port has no
                    // default value (IEEE 1800-2017 default-argument semantics).
                    if (method != null && HasUnprovidedRequiredArgument(method, i + 1))
                    {
                        word.AddError("too few arguments");
                    }
                    methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference, word.GetReference());
                    methodCall.Constant = returnConstant;
                    word.MoveNext();
                    break;
                }
                else if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("illegal function call");
                    return null;
                }
                i++;
            }
            return methodCall;
        }

        /// <summary>
        /// Returns true when at least one argument from <paramref name="startIndex"/>
        /// onward has no default value, i.e. the call is missing a required argument.
        /// Ports that carry a DefaultArgument (IEEE 1800-2017 default-argument
        /// semantics, e.g. "function enum next( int unsigned N = 1 )") may be omitted.
        /// </summary>
        private static bool HasUnprovidedRequiredArgument(BuiltInMethod method, int startIndex)
        {
            for (int k = startIndex; k < method.PortsList.Count; k++)
            {
                if (method.PortsList[k].DefaultArgument == null) return true;
            }
            return false;
        }

        /// <summary>
        /// Parse the optional `with ( expression )` clause of an array locator /
        /// ordering method call (IEEE 1800-2017 7.12): qs = s.find with ( item == "sad" );
        /// </summary>
        private static void parseWithClause(WordScanner word, NameSpace usedNameSpace, BuiltinMethodCall methodCall)
        {
            if (word.Text != "with") return;
            bool isRandomize = (methodCall.FunctionName == "randomize");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // randomize() with { constraint_block }
            // (randomize_call ::= "randomize" ... [ "with" [ "(" identifier_list ")" ] constraint_block ])
            if (isRandomize && word.Text == "{")
            {
                skipConstraintBlock(word);
                methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference, word.GetReference());
                return;
            }

            // randomize() with ( identifier_list ) { constraint_block }
            if (isRandomize && word.Text == "(")
            {
                word.MoveNext();
                // skip identifier_list (or "null")
                int guard = 0;
                while (!word.Eof && word.Text != ")" && word.Text != ";" && guard++ < 1000)
                {
                    word.MoveNext();
                }
                if (word.Text == ")") word.MoveNext();
                if (word.Text == "{")
                {
                    skipConstraintBlock(word);
                }
                methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference, word.GetReference());
                return;
            }

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( required");
                return;
            }
            word.MoveNext();

            bindLocatorVariable(word, usedNameSpace);

            Expression? expression = Expression.ParseCreate(word, usedNameSpace);
            if (expression == null)
            {
                word.AddError("missed expression");
                return;
            }
            if (word.GetCharAt(0) != ')')
            {
                word.AddError(") required");
                return;
            }
            word.MoveNext();

            methodCall.WithExpression = expression;
            methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference, word.GetReference());
        }

        /// <summary>
        /// Skip a randomize() inline constraint block: "{ { constraint_set_item } }".
        /// The nested braces / bracket groups are consumed by matching bracket depth.
        /// </summary>
        private static void skipConstraintBlock(WordScanner word)
        {
            if (word.Text != "{") return;
            int depth = 0;
            while (!word.Eof)
            {
                if (word.Text == "{") depth++;
                else if (word.Text == "}")
                {
                    depth--;
                    if (depth == 0)
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        return;
                    }
                }
                word.MoveNext();
            }
        }

        /// <summary>
        /// An array locator `with` clause references the implicit loop variable
        /// `item`. Bind it in the enclosing name space before the clause's
        /// expression is parsed so the comparison does not resolve as an
        /// unfound object. PatternVariable.ParseCreate uses the same binding.
        /// A prototype / partial parse discards its results, so it must not
        /// pollute the shared name space with the synthetic loop variable.
        /// </summary>
        private static void bindLocatorVariable(WordScanner word, NameSpace? usedNameSpace)
        {
            if (usedNameSpace == null || word.Prototype || word.CompletionContext != null) return;
            if (usedNameSpace.NamedElements.ContainsKey("item")) return;
            DataObjects.DataTypes.IDataType dataType = DataObjects.DataTypes.StringType.Create(null);
            DataObjects.DataObject dataObject = DataObjects.Variables.String.Create("item", dataType);
            dataObject.Defined = true;

            // iterator index querying methods (IEEE 1800-2017 7.12.4):
            // item.index / item.first / item.last / item.prev / item.next.
            // The synthetic loop variable is registered as a String, whose
            // NamedElements is built lazily; adding these methods here lets
            // "item.index" resolve inside the locator with-clause.
            List<DataObjects.Port> indexMethodPorts = new List<DataObjects.Port>();
            DataObjects.Variables.Variable indexReturn = DataObjects.Variables.Int.Create("index", DataObjects.DataTypes.IntType.Create(false));
            dataObject.NamedElements.Add("index", BuiltInMethod.Create("index", indexReturn, indexMethodPorts));
            dataObject.NamedElements.Add("first", BuiltInMethod.Create("first", indexReturn, indexMethodPorts));
            dataObject.NamedElements.Add("last", BuiltInMethod.Create("last", indexReturn, indexMethodPorts));
            dataObject.NamedElements.Add("next", BuiltInMethod.Create("next", indexReturn, indexMethodPorts));
            dataObject.NamedElements.Add("prev", BuiltInMethod.Create("prev", indexReturn, indexMethodPorts));

            usedNameSpace.NamedElements.Add(dataObject.Name, dataObject);
        }

        /// <summary>
        /// Append the label of the method argument port at the given index to the input-time hint popup items.
        /// (built-in method call argument position, e.g. "obj.randomize(|")
        /// </summary>
        private static void appendArgumentPopupItems(
            CompletionContext completionContext, Verilog.BuiltInMethod method, int index)
        {
            if (method == null) return;
            if (index < 0) return;
            if (index >= method.PortsList.Count) return;   // all arguments are already given
            DataObjects.Port port = method.PortsList[index];
            completionContext.CarletPopupItems.Add(
                new CodeEditor2.CodeEditor.PopupHint.PopupItem(port.GetLabel()));
        }
    }
}
