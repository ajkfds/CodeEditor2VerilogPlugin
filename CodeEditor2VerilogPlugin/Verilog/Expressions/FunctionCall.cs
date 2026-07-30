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

        /// <summary>
        /// Get the Function if this call refers to a function
        /// </summary>
        public Function? Function
        {
            get
            {
                if (DefinedNameSpace.BuildingBlock.NamedElements.ContainsFunction(FunctionName))
                {
                    return (Function)DefinedNameSpace.BuildingBlock.NamedElements[FunctionName];
                }
                return null;
            }
        }

        /// <summary>
        /// Get the LetDeclaration if this call refers to a let declaration
        /// </summary>
        public DataObjects.LetDeclaration? LetDeclaration
        {
            get
            {
                // First check in current namespace
                if (DefinedNameSpace.BuildingBlock.NamedElements.TryGetValue(FunctionName, out var element))
                {
                    return element as DataObjects.LetDeclaration;
                }
                // Then search upward through namespaces
                INamedElement? namedElement = DefinedNameSpace.GetNamedElementUpward(FunctionName);
                return namedElement as DataObjects.LetDeclaration;
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
            DataObjects.LetDeclaration? letDecl = null;
            bool found = false;

            // First, check in the current namespace (check both Functions and LetDeclarations)
            if (functionDefinedNameSpace.BuildingBlock.NamedElements.TryGetValue(functionCall.FunctionName, out var element))
            {
                function = element as Function;
                if (function != null)
                {
                    found = true;
                }
                else
                {
                    // Check for LetDeclaration
                    letDecl = element as DataObjects.LetDeclaration;
                    if (letDecl != null)
                    {
                        found = true;
                    }
                }
            }

            // If not found, search upward through namespaces
            if (!found)
            {
                INamedElement? namedElement = functionDefinedNameSpace.GetNamedElementUpward(functionCall.FunctionName);
                if (namedElement is Function)
                {
                    function = (Function)namedElement;
                    found = true;
                }
                else if (namedElement is DataObjects.LetDeclaration)
                {
                    letDecl = (DataObjects.LetDeclaration)namedElement;
                    found = true;
                }
            }

            // Check system functions
            if (!found && word.RootParsedDocument.ProjectProperty.SystemFunctions.ContainsKey(word.Text))
            {
                // System functions are allowed
                found = true;
            }

            if (!found)
            {
                word.AddError("undefined");
            }

            // Set BitWidth based on function or let declaration
            if (function != null)
            {
                functionCall.BitWidth = function.ReturnVariable?.BitWidth;
            }
            else if (letDecl != null && letDecl.Expression != null)
            {
                functionCall.BitWidth = letDecl.Expression.BitWidth;
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
                if (function != null && function.Ports.Count != 0)
                {
                    // Check if all ports have default arguments
                    bool allHaveDefaults = true;
                    foreach (var port in function.PortsList)
                    {
                        if (port.DefaultArgument == null)
                        {
                            allHaveDefaults = false;
                            break;
                        }
                    }
                    if (!allHaveDefaults)
                    {
                        word.AddError("illegal function call");
                        return null;
                    }
                }
                else if (letDecl != null && letDecl.Ports.Count != 0)
                {
                    // Check if all let ports have default values
                    bool allHaveDefaults = true;
                    foreach (var port in letDecl.PortsList)
                    {
                        if (port.DefaultArgument == null)
                        {
                            allHaveDefaults = false;
                            break;
                        }
                    }
                    if (!allHaveDefaults)
                    {
                        word.AddError("illegal let call (argument required)");
                        return null;
                    }
                }
                return functionCall;
            }

            // Use common ListOfArguments parser
            // Pass letDecl (which implements IPortNameSpace) so that port checks work
            // for let calls as well as function calls.
            IPortNameSpace? portNameSpace = function != null
                ? (IPortNameSpace)function
                : (letDecl != null ? (IPortNameSpace)letDecl : null);
            ListOfArguments.ParseListOfArguments(word, nameSpace, portNameSpace, functionCall.PortConnection, out bool returnConstant);

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
