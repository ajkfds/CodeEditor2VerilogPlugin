/*
SystemVerilog 2017 (IEEE 1800-2017)

bind_directive ::=
    bind bind_target_scope [ bind_target_instance_list ] ; [ bind_items ]

bind_target_scope ::=
    hierarchical_identifier
  | wildcard import package_identifier

bind_target_instance_list ::=
    hierarchical_identifier { , hierarchical_identifier }

bind_items ::=
    bind_item { bind_item }

bind_item ::=
    hierarchical_identifier [ ( [ list_of_argument_assignments ] ) ] ;
*/

using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    public class BindDirective
    {
        public IndexReference BeginIndexReference { get; set; }
        public IndexReference? BlockBeginIndexReference { get; set; }
        public IndexReference? LastIndexReference { get; set; }
        public WordReference? DefinitionReference { get; set; }
        public bool Prototype { get; set; }

        /// <summary>
        /// bind_target_scope: hierarchical_identifier or wildcard import package_identifier
        /// </summary>
        public string TargetScope { get; set; }

        /// <summary>
        /// True if wildcard import form (import package_identifier::*)
        /// </summary>
        public bool IsWildcardImport { get; set; }

        /// <summary>
        /// List of bind_target_instance (hierarchical identifiers)
        /// </summary>
        public List<string> TargetInstances { get; set; } = new List<string>();

        /// <summary>
        /// Bind items (instantiations inside the bind directive)
        /// </summary>
        public List<BindItem> BindItems { get; set; } = new List<BindItem>();

        public class BindItem
        {
            public string SourceName { get; set; }
            public string InstanceName { get; set; }
            public Dictionary<string, Expression> ParameterOverrides { get; set; } = new Dictionary<string, Expression>();
            public Dictionary<string, Expression> PortConnections { get; set; } = new Dictionary<string, Expression>();
        }

        public static bool Parse(WordScanner word, NameSpace nameSpace, out BindDirective? bindDirective)
        {
            bindDirective = null;

            // Check for bind keyword
            if (word.Text != "bind")
            {
                return false;
            }

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            BindDirective bind = new BindDirective()
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                Prototype = word.Prototype
            };

            // Parse bind_target_scope
            // Two forms:
            // 1. hierarchical_identifier
            // 2. wildcard import package_identifier

            bool isWildcardImport = false;
            string targetScope = "";

            if (word.Text == "wildcard")
            {
                // wildcard import package_identifier form
                isWildcardImport = true;
                bind.IsWildcardImport = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text != "import")
                {
                    word.AddError("import expected");
                    skipToSemicolon(word);
                    return true;
                }
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal package name");
                    skipToSemicolon(word);
                    return true;
                }

                targetScope = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text == "::")
                {
                    // import package_identifier::*
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if (word.Text != "*")
                    {
                        word.AddError("* expected");
                        skipToSemicolon(word);
                        return true;
                    }
                    targetScope += "::*";
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
            }
            else
            {
                // hierarchical_identifier form
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal identifier");
                    skipToSemicolon(word);
                    return true;
                }

                // Collect the hierarchical identifier
                List<string> parts = new List<string>();
                while (General.IsIdentifier(word.Text) || word.Text == ".")
                {
                    if (word.Text == ".")
                    {
                        parts.Add(".");
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        continue;
                    }
                    parts.Add(word.Text);
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                // Handle :: for package qualified names
                if (word.Text == "::")
                {
                    parts.Add("::");
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    while (General.IsIdentifier(word.Text))
                    {
                        parts.Add(word.Text);
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }

                targetScope = string.Join("", parts);
            }

            bind.TargetScope = targetScope;

            // Parse optional bind_target_instance_list
            // hierarchical_identifier { , hierarchical_identifier }
            while (word.Text != ";")
            {
                if (word.Eof)
                {
                    word.AddError("; expected");
                    return true;
                }

                // Check if this is a hierarchical identifier for instance list
                if (General.IsIdentifier(word.Text) && word.NextText != "(")
                {
                    // Check if we should add to instance list
                    // Instance list entries end before the semicolon
                    // and are separated by commas

                    // Check if this is just before ;
                    if (word.NextText == ";")
                    {
                        // This might be part of bind items
                        break;
                    }

                    // Check for comma (meaning it's part of instance list)
                    // But we need to be careful - the first identifier after scope
                    // might be the start of bind_items

                    // A simple heuristic: if there's no comma and the text doesn't start with
                    // a module/interface/program name, it might be an instance
                    // We need to look ahead

                    // For now, let's assume if we see an identifier followed by ( or ;,
                    // it's the start of bind items
                    if (word.NextText == "(")
                    {
                        // This is the start of bind items
                        break;
                    }

                    // If we see a comma, it's part of instance list
                    // Collect all instances until we see something that looks like a bind item
                }

                if (word.Text == "(")
                {
                    // Start of bind items
                    break;
                }

                word.MoveNext();
            }

            // Parse bind_target_instance_list more carefully
            // This is comma-separated hierarchical identifiers
            // We need to detect when the instance list ends

            // Save current position
            var savedIndex = word.Clone();

            // Collect potential instances
            List<string> instances = new List<string>();
            while (!word.Eof && word.Text != ";")
            {
                if (word.Text == "(")
                {
                    // Start of bind items - restore and break
                    word = savedIndex;
                    break;
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }

                // Collect hierarchical identifier
                List<string> idParts = new List<string>();
                while (General.IsIdentifier(word.Text) || word.Text == ".")
                {
                    if (word.Text == ".")
                    {
                        idParts.Add(".");
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        continue;
                    }
                    idParts.Add(word.Text);
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                if (idParts.Count > 0)
                {
                    instances.Add(string.Join("", idParts));
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
            }

            bind.TargetInstances = instances;

            // Expect semicolon
            if (word.Text != ";")
            {
                word.AddError("; expected");
                skipToSemicolon(word);
                return true;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            bind.BlockBeginIndexReference = word.CreateIndexReference();

            // Parse bind_items (optional)
            // bind_item ::= hierarchical_identifier [ ( [ list_of_argument_assignments ] ) ] ;
            while (!word.Eof && word.Text != "endbind")
            {
                if (!General.IsIdentifier(word.Text))
                {
                    word.MoveNext();
                    continue;
                }

                // Parse bind item
                BindItem item = new BindItem();

                // Source name (module/interface/checker to instantiate)
                string sourceName = word.Text;
                item.SourceName = sourceName;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                // Optional parameter overrides (#(...))
                if (word.Text == "#")
                {
                    word.MoveNext();
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        // Parse parameter assignments
                        while (!word.Eof && word.Text != ")")
                        {
                            if (word.Text == "(")
                            {
                                // Nested parentheses
                                word.AddError("illegal parameter");
                                break;
                            }

                            if (General.IsIdentifier(word.Text) && word.NextText == "=")
                            {
                                // Named parameter assignment
                                string paramName = word.Text;
                                word.MoveNext(); // =
                                word.MoveNext();
                                Expression? expr = Expression.ParseCreate(word, nameSpace);
                                if (expr != null)
                                {
                                    item.ParameterOverrides[paramName] = expr;
                                }
                            }
                            else
                            {
                                // Ordered parameter assignment
                                Expression? expr = Expression.ParseCreate(word, nameSpace);
                            }

                            if (word.Text == ",")
                            {
                                word.MoveNext();
                            }
                        }
                        if (word.Text == ")")
                        {
                            word.MoveNext();
                        }
                    }
                }

                // Instance name
                if (General.IsIdentifier(word.Text))
                {
                    item.InstanceName = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    word.AddError("illegal instance name");
                    break;
                }

                // Optional port connections (...)
                if (word.Text == "(")
                {
                    word.MoveNext();
                    while (!word.Eof && word.Text != ")")
                    {
                        if (word.Text == ".")
                        {
                            // Named port connection
                            word.MoveNext();
                            string portName = word.Text;
                            word.Color(CodeDrawStyle.ColorType.Identifier);
                            word.MoveNext();

                            if (word.Text == "(")
                            {
                                word.MoveNext();
                                Expression? expr = Expression.ParseCreate(word, nameSpace);
                                if (expr != null)
                                {
                                    item.PortConnections[portName] = expr;
                                }
                                if (word.Text == ")")
                                {
                                    word.MoveNext();
                                }
                            }
                        }
                        else
                        {
                            // Ordered port connection
                            Expression? expr = Expression.ParseCreate(word, nameSpace);
                        }

                        if (word.Text == ",")
                        {
                            word.MoveNext();
                        }
                    }
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }

                // Semicolon
                if (word.Text == ";")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                    skipToSemicolon(word);
                }

                bind.BindItems.Add(item);
            }

            bind.LastIndexReference = word.CreateIndexReference();

            if (word.Text == "endbind")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            bindDirective = bind;
            return true;
        }

        private static void skipToSemicolon(WordScanner word)
        {
            while (!word.Eof)
            {
                if (word.Text == ";")
                {
                    word.MoveNext();
                    return;
                }
                word.MoveNext();
            }
        }
    }
}
