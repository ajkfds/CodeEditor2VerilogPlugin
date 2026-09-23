using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Expressions
{
    public static class ListOfArguments
    {
        public static void ParseListOfArguments(WordScanner word, NameSpace usedNameSpace,
            IPortNameSpace? portNameSpace,
            Dictionary<string, Expressions.Expression> portConnection,
            CompletionContext? completionContext = null
            )
        {
            ParseListOfArguments(word, usedNameSpace,
            portNameSpace,
            portConnection,
            out _,
            completionContext
            );
        }
        public static void ParseListOfArguments(WordScanner word, NameSpace usedNameSpace,
            IPortNameSpace? portNameSpace,
            Dictionary<string, Expressions.Expression> portConnection,
            out bool constantConnected,
            CompletionContext? completionContext = null
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

            // (EOF just after "("): hint for the first argument
            if (completionContext != null && word.Eof)
            {
                AppendArgumentPopupItems(completionContext, portNameSpace, 0);
                return;
            }

            if (word.Text == ")")
            {
                // 空の括弧の場合：すべてのポートにデフォルト値があるかチェック
                if (portNameSpace != null && portNameSpace.Ports.Count != 0)
                {
                    foreach (var port in portNameSpace.PortsList)
                    {
                        if (port.DefaultArgument == null)
                        {
                            word.AddError("too few arguments");
                            break;
                        }
                    }
                }
                word.MoveNext();
                return;
            }

            int i = 0;
            HashSet<string> connectedPorts = new HashSet<string>();

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

                    word.AddError("must have default argument");
                    i++;
                    word.MoveNext();
                    continue;
                }

                Expression? expression = Expression.ParseCreate(word, usedNameSpace, completionContext);
                if (expression == null)
                {
                    // EOF while typing an argument expression (e.g. "func(arg1"):
                    // hint for the argument currently being typed
                    if (completionContext != null && word.Eof)
                    {
                        AppendArgumentPopupItems(completionContext, portNameSpace, i);
                    }
                    word.SkipToKeyword(";");
                    return;
                }

                // EOF just after an argument expression (e.g. "func(arg1"):
                // show the hint for the current argument and return without side effects
                // (partial-parse expressions must not be registered into the parsed document)
                if (completionContext != null && word.Eof)
                {
                    AppendArgumentPopupItems(completionContext, portNameSpace, i);
                    return;
                }

                if (!expression.Constant) constantConnected = false;

                if (!portConnection.ContainsKey(port.Name))
                {
                    portConnection.Add(port.Name, expression);
                }
                connectedPorts.Add(port.Name);

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

            // (EOF after comma or at the end of positional arguments, e.g. "func(arg1, "):
            // hint for the next argument
            if (completionContext != null && word.Eof)
            {
                AppendArgumentPopupItems(completionContext, portNameSpace, i);
                return;
            }

            // Named port connections (after positional arguments)
            while (!word.Eof & word.Text == ".")
            {
                if (word.Text == ")") break;

                if (portNameSpace == null)
                {
                    parseUndefinedNamedPort(word, usedNameSpace);
                    continue;
                }

                word.MoveNext();

                // (EOF just after "."): suggest unconnected argument names
                if (completionContext != null && word.Eof)
                {
                    appendNamedArgumentCandidates(completionContext, portNameSpace, connectedPorts);
                    return;
                }

                if (!portNameSpace.Ports.ContainsKey(word.Text))
                {
                    word.AddError("undefined port");
                    word.SkipToKeyword(";");
                    return;
                }

                DataObjects.Port port = portNameSpace.Ports[word.Text];
                word.Color(CodeDrawStyle.ColorType.Identifier);

                if (connectedPorts.Contains(port.Name))
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

                // (EOF just after ".name("): hint for this argument
                if (completionContext != null && word.Eof)
                {
                    AppendArgumentPopupItems(completionContext, portNameSpace, portNameSpace.PortsList.IndexOf(port));
                    return;
                }

                // Check for empty expression (use default)
                Expression? expression = null;
                if (word.Text != ")")
                {
                    expression = Expression.ParseCreate(word, (NameSpace)portNameSpace, completionContext);
                    if (expression == null)
                    {
                        // EOF while typing the named argument expression (e.g. "func(.p("):
                        // hint for this argument
                        if (completionContext != null && word.Eof)
                        {
                            AppendArgumentPopupItems(completionContext, portNameSpace, portNameSpace.PortsList.IndexOf(port));
                        }
                        word.SkipToKeyword(";");
                        return;
                    }

                    // EOF just after the named argument expression (e.g. "func(.p(x"):
                    // show the hint for this argument and return without side effects
                    if (completionContext != null && word.Eof)
                    {
                        AppendArgumentPopupItems(completionContext, portNameSpace, portNameSpace.PortsList.IndexOf(port));
                        return;
                    }

                    if (!expression.Constant) constantConnected = false;
                }
                else
                {
                    // Empty expression: check if port has default
                    if (port.DefaultArgument == null)
                    {
                        word.AddError("argument required for port " + port.Name);
                    }
                    // else: use default argument (expression remains null)
                }

                if (!portConnection.ContainsKey(port.Name))
                {
                    if (expression != null)
                    {
                        portConnection.Add(port.Name, expression);
                    }
                    else if (port.DefaultArgument != null)
                    {
                        portConnection.Add(port.Name, port.DefaultArgument);
                        if (!port.DefaultArgument.Constant) constantConnected = false;
                    }
                }
                connectedPorts.Add(port.Name);

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
                // Check for missing required arguments (those without defaults)
                if (portNameSpace != null)
                {
                    foreach (var port in portNameSpace.PortsList)
                    {
                        if (!connectedPorts.Contains(port.Name) && port.DefaultArgument == null)
                        {
                            word.AddError("too few arguments");
                            break;
                        }
                    }
                }
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

        /// <summary>
        /// Append the label of the argument port at the given index to the input-time hint popup items.
        /// (function call / task enable argument position)
        /// </summary>
        internal static void AppendArgumentPopupItems(
            CompletionContext completionContext, IPortNameSpace? portNameSpace, int index)
        {
            if (portNameSpace == null) return;
            if (index < 0) return;
            if (index >= portNameSpace.PortsList.Count) return;   // all arguments are already given
            DataObjects.Port port = portNameSpace.PortsList[index];
            completionContext.CarletPopupItems.Add(
                new CodeEditor2.CodeEditor.PopupHint.PopupItem(port.GetLabel()));
        }

        /// <summary>
        /// Append unconnected argument names as autocomplete candidates (named argument "." position).
        /// </summary>
        private static void appendNamedArgumentCandidates(
            CompletionContext completionContext, IPortNameSpace? portNameSpace, HashSet<string> connectedPorts)
        {
            if (portNameSpace == null) return;
            foreach (DataObjects.Port port in portNameSpace.PortsList)
            {
                if (connectedPorts.Contains(port.Name)) continue;   // already connected (positional)
                if (!port.Name.StartsWith(completionContext.CandidateWord)) continue;

                Data.VerilogCommon.AutoCompleteItem acItem = new Data.VerilogCommon.AutoCompleteItem(
                    Data.VerilogCommon.AutoCompleteItem.CompleteType.DataObject,
                    port.Name,
                    CodeDrawStyle.ColorIndex(CodeDrawStyle.ColorType.Variable),
                    Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Variable),
                    "CodeEditor2/Assets/Icons/tag.svg"
                    );
                completionContext.AutoCompleteItems.Add(acItem);
            }
        }
    }
}
