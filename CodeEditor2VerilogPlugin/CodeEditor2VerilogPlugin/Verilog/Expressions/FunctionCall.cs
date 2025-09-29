using CodeEditor2.Data;
using ExCSS;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Arrays;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.Verilog.Expressions
{
    public class FunctionCall : Primary
    {
        protected FunctionCall() { }

        public required string FunctionName { get; init; }
        public new static FunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            throw new Exception();
        }
        public Dictionary<string, Expressions.Expression> PortConnection { get; set; } = new Dictionary<string, Expressions.Expression>();

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

        public static FunctionCall? ParseCreate(WordScanner word,NameSpace usedNameSpace, NameSpace definedNameSpace)
        {
            if (word.RootParsedDocument.ProjectProperty == null) throw new Exception();

            FunctionCall functionCall = new FunctionCall() { FunctionName = word.Text, DefinedNameSpace = definedNameSpace,ProjectProperty=word.ProjectProperty };
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
               INamedElement? namedElement = usedNameSpace.GetNamedElementUpward(functionCall.FunctionName);
                if(namedElement is Function)
                {
                    function = (Function)namedElement;
                }
                else
                {
                    word.AddError("undefined");
                }
            }

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            /*
            tf_call ::= ps_or_hierarchical_tf_identifier { attribute_instance } [ "(" list_of_arguments ")" ]

            list_of_arguments       ::= [ expression ] { , [ expression ] } { , "." identifier ( [ expression ] ) }
                                        | "." identifier ( [ expression ] ) { , "." identifier ( [ expression ] ) }
            ps_or_hierarchical_tf_identifier    ::= [ package_scope ] tf_identifier
             */

            if (word.Text != "(")
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
                if (function != null && function.Ports.Count !=0)
                {
                    word.AddError("too few arguments");
                }
                functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference, word.GetReference());
                word.MoveNext();
                return functionCall;
            }

            int i = 0;
            while (!word.Eof)
            {
                if (word.Text == ")" || word.Text == "." || word.Text==";") break;

                if(function == null)
                {
                    var index = word.CreateIndexReference();
                    parseUndefinedPort(word, usedNameSpace,definedNameSpace);
                    if (word.CreateIndexReference().IsSameAs(index)) break;
                    continue;
                }

                if (i >= function.PortsList.Count)
                {
                    word.AddError("illegal argument");
                    word.SkipToKeyword(";");
                    return functionCall;
                }

                Port port = function.PortsList[i];

                if (word.Text == ",")
                {
                    if(port.DefaultArgument != null)
                    {
                        word.MoveNext();
                        i++;
                        continue;
                    }

                    word.AddError("must have defult argument");
                    i++;
                    word.MoveNext();
                    continue;
                }

                Expression? expression = Expression.ParseCreate(word, definedNameSpace);
                if(expression == null)
                {
                    word.SkipToKeyword(";");
                    return functionCall;
                }

                if (!expression.Constant) returnConstant = false;

                if (!functionCall.PortConnection.ContainsKey(port.Name))
                {
                    functionCall.PortConnection.Add(port.Name, expression);
                }

                if (port.Range != null
                ){
                    PackedArray? range = port.Range;
                    if(range != null && range.Size != expression.BitWidth)
                    {
                        word.AddWarning("bitwidth mismatch");
                    }
                }
                i++;

                if (word.Text == ")") break;

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }

                word.AddError("illegal function call");
                word.SkipToKeywords(new List<string> {";"});
                return null;
            }

            while (!word.Eof & word.Text==".")
            {
                if (word.Text == ")") break;

                if(function == null)
                {
                    parseUndefinedNamedPort(word, usedNameSpace, definedNameSpace);
                    continue;
                }

                word.MoveNext();
                if (!function.Ports.ContainsKey(word.Text))
                {
                    word.AddError("unudefined port");
                    word.SkipToKeyword(";");
                    return functionCall;
                }

                Port port = function.Ports[word.Text];
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (functionCall.PortConnection.ContainsKey(port.Name))
                {
                    word.AddError("duplicated port");
                }

                word.MoveNext();

                if (word.Text != "(")
                {
                    word.AddError("( required");
                    word.SkipToKeyword(";");
                    return functionCall;
                }
                word.MoveNext();
                Expression? expression = Expression.ParseCreate(word, definedNameSpace);
                if( expression == null)
                {
                    word.AddError("illegal port expression");
                    word.SkipToKeyword(";");
                    return functionCall;
                }

                if (!functionCall.PortConnection.ContainsKey(port.Name))
                {
                    functionCall.PortConnection.Add(port.Name, expression);
                }

                if (word.Text != ")")
                {
                    word.AddError("( required");
                    word.SkipToKeyword(";");
                    return functionCall;
                }
                word.MoveNext();

                if(word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
            }


            if (word.Text == ")")
            {
                if (function != null && i < function.Ports.Count - 1)
                {
                    word.AddError("too few arguments");
                }
                functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference, word.GetReference());
                functionCall.Constant = returnConstant;
                word.MoveNext();
            }
            else
            {
                word.AddError("illegal function call end");
            }

            return functionCall;
        }

        private static void parseUndefinedPort(WordScanner word, NameSpace usedNameSpace, NameSpace definedNameSpace)
        {
            if (word.Text == ")" || word.Text == ".") throw new Exception();
            if (word.Text == ",") // blank argument
            {
                word.MoveNext();
                return;
            }
            Expression? expression = Expression.ParseCreate(word, definedNameSpace);
            if (word.Text == ",")
            {
                word.MoveNext();
                return;
            }
            if (word.Text == ")") return;
            if (word.Text == ".") return;

            word.AddError("illegal argument");
            word.SkipToKeyword(";");
        }
        private static void parseUndefinedNamedPort(WordScanner word, NameSpace usedNameSpace, NameSpace definedNameSpace)
        {
            if (word.Text != ".") throw new Exception();

            word.MoveNext();
            string portName = word.Text;

            if (General.IsIdentifier(word.Text))
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            else
            {
                word.SkipToKeyword(";");
                return;
            }

            if (word.Text != "(")
            {
                word.AddError("( required");
                word.SkipToKeyword(";");
                return;
            }
            word.MoveNext();
            
            Expression? expression = Expression.ParseCreate(word, definedNameSpace);
            
            if (word.Text != ")")
            {
                word.AddError("( required");
                word.SkipToKeyword(";");
                return;
            }
            word.MoveNext();

            if (word.Text == ",")
            {
                word.MoveNext();
            }
        }
    }
}
