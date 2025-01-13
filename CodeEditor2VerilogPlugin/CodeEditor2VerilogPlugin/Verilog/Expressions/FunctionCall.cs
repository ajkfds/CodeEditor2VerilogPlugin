using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class FunctionCall : Primary
    {
        protected FunctionCall() { }
        public List<Expression> Expressions = new List<Expression>();
        public required string FunctionName { get; init; }

        public new static FunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            throw new Exception();
        }


        public new static FunctionCall? ParseCreate(WordScanner word,NameSpace definedNameSpace, NameSpace nameSpace)
        {
            FunctionCall functionCall = new FunctionCall() { FunctionName = word.Text };
            functionCall.Reference = word.GetReference();
            bool returnConstant = true;

            Function? function = null;
            if (definedNameSpace.BuildingBlock.NamedElements.ContainsFunction(functionCall.FunctionName))
            {
                function = (Function)definedNameSpace.BuildingBlock.NamedElements[functionCall.FunctionName];
            }
            else if (word.RootParsedDocument.ProjectProperty.SystemFunctions.ContainsKey(word.Text))
            {
            //
            }
            else
            {
                word.AddError("undefined");
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.GetCharAt(0) != '(')
            {
                if(function != null && function.Ports.Count != 0)
                {
                    word.AddError("illegal function call");
                    return null;
                }
                else
                {
                    return functionCall;
                }
            }
            word.MoveNext();

            if (word.Text == ")")
            {
                functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference, word.GetReference());
                word.MoveNext();
                return functionCall;
            }

            int i = 0;
            while (!word.Eof)
            {
                Expression? expression = Expression.ParseCreate(word, nameSpace);
                if(expression == null)
                {
                    return null;
                }
                if (!expression.Constant) returnConstant = false;
                functionCall.Expressions.Add(expression);
                if(function != null)
                {
                    if (i >= function.Ports.Count)
                    {
                        word.AddError("illegal argument");
                    }
                    else
                    {
                        if (function.PortsList[i] != null 
                            && expression != null 
                            && function.PortsList[i].Range != null
                        ){
                            PackedArray? range = function.PortsList[i].Range;
                            if(range != null && range.Size != expression.BitWidth)
                            {
                                word.AddWarning("bitwidth mismatch");
                            }
                        }
                    }
                }


                if(word.Text == ")")
                {
                    functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference,word.GetReference());
                    functionCall.Constant = returnConstant;
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
            return functionCall;
        }
    }
}
