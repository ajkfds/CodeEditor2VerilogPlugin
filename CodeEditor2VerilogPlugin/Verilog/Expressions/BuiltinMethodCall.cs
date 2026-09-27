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
                if (method != null && method.Ports.Count != 0)
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
                    if (method != null && i < method.Ports.Count - 1)
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
