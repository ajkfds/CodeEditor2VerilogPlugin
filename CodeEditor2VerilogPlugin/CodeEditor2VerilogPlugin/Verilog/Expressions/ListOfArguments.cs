using pluginVerilog;
using pluginVerilog.Verilog;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public static class ListOfArguments
    {
        public static void ParseListOfArguments(WordScanner word, NameSpace usedNameSpace,
            IPortNameSpace? portNameSpace,
            Dictionary<string, Expressions.Expression> portConnection
            )
        {
            ParseListOfArguments(word, usedNameSpace,
            portNameSpace,
            portConnection,
            out _
            );
        }
        public static void ParseListOfArguments(WordScanner word, NameSpace usedNameSpace,
            IPortNameSpace? portNameSpace,
            Dictionary<string, Expressions.Expression> portConnection,
            out bool constantConnected
            )
        {
            /*
            list_of_arguments       ::= [ expression ] { , [ expression ] } { , "." identifier ( [ expression ] ) }
                                        | "." identifier ( [ expression ] ) { , "." identifier ( [ expression ] ) }
             */

            constantConnected = true;

            if (word.Text != "(")
            {
                if (portNameSpace != null && portNameSpace.Ports.Count != 0)
                {
                    word.AddError("illegal arguments");
                    return;
                }
                else
                {
                    return;
                }
            }
            word.MoveNext();

            if (word.Text == ")")
            {
                if (portNameSpace != null && portNameSpace.Ports.Count != 0)
                {
                    word.AddError("too few arguments");
                }
                word.MoveNext();
                return;
            }

            int i = 0;
            while (!word.Eof)
            {
                if (word.Text == ")" || word.Text == "." || word.Text == ";") break;

                if (portNameSpace == null)
                {
                    var index = word.CreateIndexReference();
                    parseUndefinedPort(word, usedNameSpace);
                    if (word.CreateIndexReference().IsSameAs(index)) break;
                    continue;
                }

                if (i >= portNameSpace.PortsList.Count)
                {
                    word.AddError("illegal argument");
                    word.SkipToKeyword(";");
                    return;
                }

                DataObjects.Port port = portNameSpace.PortsList[i];

                if (word.Text == ",")
                {
                    if (port.DefaultArgument != null)
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

                Expression? expression = Expression.ParseCreate(word, usedNameSpace);
                if (expression == null)
                {
                    word.SkipToKeyword(";");
                    return;
                }

                if (!expression.Constant) constantConnected = false;

                if (!portConnection.ContainsKey(port.Name))
                {
                    portConnection.Add(port.Name, expression);
                }

                if (port.BitWidth != expression.BitWidth)
                {
                    word.AddWarning("bitwidth mismatch");
                }
                i++;

                if (word.Text == ")") break;

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }

                word.AddError("illegal function call");
                word.SkipToKeywords(new List<string> { ";" });
                return;
            }

            while (!word.Eof & word.Text == ".")
            {
                if (word.Text == ")") break;

                if (portNameSpace == null)
                {
                    parseUndefinedNamedPort(word, usedNameSpace);
                    continue;
                }

                word.MoveNext();
                if (!portNameSpace.Ports.ContainsKey(word.Text))
                {
                    word.AddError("unudefined port");
                    word.SkipToKeyword(";");
                    return;
                }

                DataObjects.Port port = portNameSpace.Ports[word.Text];
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (portConnection.ContainsKey(port.Name))
                {
                    word.AddError("duplicated port");
                }

                word.MoveNext();

                if (word.Text != "(")
                {
                    word.AddError("( required");
                    word.SkipToKeyword(";");
                    return;
                }
                word.MoveNext();
                Expression? expression = Expression.ParseCreate(word, (NameSpace)portNameSpace);
                if (expression == null)
                {
                    word.AddError("illegal port expression");
                    word.SkipToKeyword(";");
                    return;
                }

                if (!portConnection.ContainsKey(port.Name))
                {
                    portConnection.Add(port.Name, expression);
                }

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
                    continue;
                }
            }


            if (word.Text == ")")
            {
                if (portNameSpace != null && i < portNameSpace.Ports.Count)
                {
                    word.AddError("too few arguments");
                }
//                functionCall.Reference = WordReference.CreateReferenceRange(functionCall.Reference, word.GetReference());
//                functionCall.Constant = returnConstant;
                word.MoveNext();
            }
            else
            {
                word.AddError("illegal arguments end");
            }

            return;
        }

        private static void parseUndefinedPort(WordScanner word, NameSpace usedNameSpace)
        {
            if (word.Text == ")" || word.Text == ".") throw new Exception();
            if (word.Text == ",") // blank argument
            {
                word.MoveNext();
                return;
            }
            Expression? expression = Expression.ParseCreate(word, usedNameSpace);
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
        private static void parseUndefinedNamedPort(WordScanner word, NameSpace usedNameSpace)
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

            Expression? expression = Expression.ParseCreate(word, usedNameSpace);

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
