using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog.Expressions
{
    public class FunctionCall : Primary
    {
        protected FunctionCall() { }

        public required string FunctionName { get; init; }
        public Dictionary<string, Expressions.Expression> PortConnection { get; set; } = new Dictionary<string, Expressions.Expression>();

        [JsonIgnore]
        public required NameSpace DefinedNameSpace { init; get; }
        [JsonIgnore]
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
        public static new FunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, nameSpace);
        }

        public static FunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace, NameSpace functionDefinedNameSpace)
        {
            if (word.RootParsedDocument.ProjectProperty == null) throw new Exception();

            FunctionCall functionCall = new FunctionCall() { FunctionName = word.Text, DefinedNameSpace = functionDefinedNameSpace, ProjectProperty = word.ProjectProperty };
            functionCall.Reference = word.GetReference();

            Function? function = null;
            if (functionDefinedNameSpace.BuildingBlock.NamedElements.ContainsFunction(functionCall.FunctionName))
            {
                function = (Function)functionDefinedNameSpace.BuildingBlock.NamedElements[functionCall.FunctionName];
            }
            else if (word.RootParsedDocument.ProjectProperty.SystemFunctions.ContainsKey(word.Text))
            {
                //
            }
            else
            {
                INamedElement? namedElement = functionDefinedNameSpace.GetNamedElementUpward(functionCall.FunctionName);
                if (namedElement is Function)
                {
                    function = (Function)namedElement;
                }
                else
                {
                    word.AddError("undefined");
                }
            }
            functionCall.BitWidth = function?.ReturnVariable?.BitWidth;

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
                if (function != null && function.Ports.Count != 0)
                {
                    word.AddError("illegal function call");
                    return null;
                }
                else
                {
                    return functionCall;
                }
            }

            // Use common ListOfArguments parser
            ListOfArguments.ParseListOfArguments(word, nameSpace, function, functionCall.PortConnection, out bool returnConstant);

            // Check if function call ended properly
            functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference, word.GetReference());
            functionCall.Constant = returnConstant;

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
