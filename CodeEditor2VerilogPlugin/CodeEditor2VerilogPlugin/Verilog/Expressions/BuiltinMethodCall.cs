using CodeEditor2.Data;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog.Expressions
{
    public class BuiltinMethodCall : Primary
    {
        protected BuiltinMethodCall() { }

        public List<Expression> Expressions = new List<Expression>();
        public required string FunctionName { get; init; }
        public new static BuiltinMethodCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            throw new Exception();
        }

        public required NameSpace DefinedNameSpace { init; get; }
        public required ProjectProperty ProjectProperty { init; get; }
        public Function? Function
        {
            get
            {
                Function? function = null;
                if (DefinedNameSpace.BuildingBlock.NamedElements.ContainsFunction(FunctionName))
                {
                    function = (Function)DefinedNameSpace.BuildingBlock.NamedElements[FunctionName];
                }
                //else if (ProjectProperty.SystemFunctions.ContainsKey(FunctionName))
                //{
                //    function = ProjectProperty.SystemFunctions[FunctionName];
                //}
                return function;
            }
        }


        public static BuiltinMethodCall? ParseCreate(WordScanner word,NameSpace usedNameSpace, NameSpace definedNameSpace)
        {
            if (word.RootParsedDocument.ProjectProperty == null) throw new Exception();
            if (!definedNameSpace.NamedElements.ContainsKey(word.Text)) return null;
            INamedElement element = definedNameSpace.NamedElements[word.Text];
            if (element is not BuiltInMethod) return null;
            var method = (BuiltInMethod)element;

            BuiltinMethodCall methodCall = new BuiltinMethodCall() { FunctionName = word.Text, DefinedNameSpace = definedNameSpace,ProjectProperty=word.ProjectProperty };
            methodCall.Reference = word.GetReference();
            bool returnConstant = true;

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if(word.GetCharAt(0) != '(')
            {
                if(method != null && method.Ports.Count != 0)
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

            if (word.Text == ")")
            {
                if (method != null && method.Ports.Count !=0)
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
                Expression? expression = Expression.ParseCreate(word, definedNameSpace);
                if(expression == null)
                {
                    return null;
                }
                if (!expression.Constant) returnConstant = false;
                methodCall.Expressions.Add(expression);
                if(method != null)
                {
                    if (i >= method.Ports.Count)
                    {
                        expression.Reference.AddError("illegal argument");
                    }
                    else
                    {
                        if (method.PortsList[i] != null 
                            && expression != null 
                            && method.PortsList[i].Range != null
                        ){
                            PackedArray? range = method.PortsList[i].Range;
                            if(range != null && range.Size != expression.BitWidth)
                            {
                                word.AddWarning("bitwidth mismatch");
                            }
                        }
                    }
                }


                if(word.Text == ")")
                {
                    if (method != null && i < method.Ports.Count-1)
                    {
                        word.AddError("too few arguments");
                    }
                    methodCall.Reference = WordReference.CreateReferenceRange(methodCall.Reference,word.GetReference());
                    methodCall.Constant = returnConstant;
                    word.MoveNext();
                    break;
                }
                else if(word.Text == ",")
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
    }
}
