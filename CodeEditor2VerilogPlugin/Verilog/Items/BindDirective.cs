/*
SystemVerilog 2017 (IEEE 1800-2017)

bind_directive ::=
      "bind" bind_target_scope      [":" bind_target_instance_list] bind_instantiation ;
    | "bind" bind_target_instance                                   bind_instantiation ;

bind_target_scope ::=
      module_identifier
    | interface_identifier

bind_target_instance ::=
    hierarchical_identifier constant_bit_select

bind_target_instance_list ::=
    bind_target_instance { "," bind_target_instance }

bind_instantiation ::=
      program_instantiation
    | module_instantiation
    | interface_instantiation
    | checker_instantiation

*/

using Avalonia.Controls;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.Property;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    public class BindDirective : IDocumentRegeion
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
        /*
        bind_directive ::=
              "bind" bind_target_scope [: bind_target_instance_list] bind_instantiation ;
            | "bind" bind_target_instance bind_instantiation ;

        bind_target_scope ::=
              module_identifier
            | interface_identifier

        bind_target_instance ::=
              hierarchical_identifier constant_bit_select

        bind_target_instance_list ::=
              bind_target_instance { , bind_target_instance }

        bind_instantiation ::=
              program_instantiation
            | module_instantiation
            | interface_instantiation
            | checker_instantiation 
         */

        public static bool Parse(WordScanner word, NameSpace? nameSpace, out BindDirective? bindDirective)
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

            // parse bind_target_scope or bind_target_instance (hierarchical identifier)
            // note: identifiers are consumed directly (not via Expression.ParseCreate) so that
            // root-level parse (nameSpace == null) works without NullReferenceException
            string targetScopeOrInstance = parseHierarchicalIdentifier(word);
            if (targetScopeOrInstance == null)
            {
                word.SkipToKeyword(";");
                return true;
            }

            // resolve target scope (module / interface identifier) in DefinitionNameSpace
            string[] targetParts = targetScopeOrInstance.Split('.');
            string targetTopName = targetParts[0];
            BuildingBlocks.BuildingBlock? targetBuildingBlock = word.ProjectProperty.DefinitionNameSpace.Get(targetTopName) as BuildingBlocks.BuildingBlock;
            if (targetBuildingBlock != null)
            {
                // bind target scope / target instance always refers to DefinitionNameSpace
                // (module / interface / program / checker instantiation: class never appears)
                if (!word.RootParsedDocument.ReferencedDefinitionNameSpace.Contains(targetTopName)) word.RootParsedDocument.ReferencedDefinitionNameSpace.Add(targetTopName);
            }
            else
            {
                word.AddError("unfound bind target");
            }
            // bind_target_scope form: [":" bind_target_instance_list]
            // bind_target_instance form: hierarchical_identifier (already includes "." path)
            bind.TargetScope = targetTopName;
            if (targetParts.Length == 1 && word.Text == ":")
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                // bind_target_instance_list ::= bind_target_instance { , bind_target_instance }
                while (true)
                {
                    string targetInstance = parseHierarchicalIdentifier(word);
                    if (targetInstance == null)
                    {
                        word.SkipToKeyword(";");
                        return true;
                    }
                    bind.TargetInstances.Add(targetInstance);
                    if (word.Text == ",")
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        continue;
                    }
                    break;
                }
            }
            else
            {
                // bind_target_instance form
                bind.TargetInstances.Add(targetScopeOrInstance);
            }

            // register hierarchical instance paths for hierarchy analysis
            // (SimulationSetup / ParseHierarchy): full paths of all
            // bind_target_instance entries
            foreach (string targetInstance in bind.TargetInstances)
            {
                if (targetInstance.Contains(".") && !word.RootParsedDocument.BindTargetInstancePaths.Contains(targetInstance))
                {
                    word.RootParsedDocument.BindTargetInstancePaths.Add(targetInstance);
                }
            }

            // bind_instantiation ::= program_instantiation | module_instantiation | interface_instantiation | checker_instantiation
            if (!General.IsSimpleIdentifier(word.Text))
            {
                word.AddError("illegal name");
                word.SkipToKeyword(";");
                return true;
            }

            string instantiationName = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            BuildingBlocks.BuildingBlock? instancedBuildingBlock = word.ProjectProperty.DefinitionNameSpace.Get(instantiationName) as BuildingBlocks.BuildingBlock;
            if (instancedBuildingBlock != null)
            {
                if (!word.RootParsedDocument.ReferencedDefinitionNameSpace.Contains(instantiationName)) word.RootParsedDocument.ReferencedDefinitionNameSpace.Add(instantiationName);

                // bind_instantiation ::= program | module | interface | checker instantiation
                if (!(instancedBuildingBlock is BuildingBlocks.Module ||
                      instancedBuildingBlock is BuildingBlocks.Interface ||
                      instancedBuildingBlock is BuildingBlocks.Program ||
                      instancedBuildingBlock is BuildingBlocks.Checker))
                {
                    word.AddError("bind instantiation requires module / interface / program / checker");
                }
            }
            else
            {
                word.AddError("unfound instanced building block");
            }

            // optional parameter value assignment # ( ... )
            Dictionary<string, Expression> parameterOverrides = new Dictionary<string, Expression>();
            if (word.Text == "#")
            {
                if (nameSpace != null)
                {
                    // full analysis via ParameterValueAssignment (same as ModuleInstantiation)
                    ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, instancedBuildingBlock);
                }
                else
                {
                    // root-level (nameSpace == null) : consume without analysis
                    word.MoveNext();
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        consumeParenBlock(word);
                    }
                }
            }

            // instance list: [ name_of_instance ( ... ) ] { , name_of_instance ( ... ) } ;
            while (true)
            {
                BindItem bindItem = new BindItem()
                {
                    SourceName = instantiationName
                };
                foreach (var kvp in parameterOverrides) bindItem.ParameterOverrides.Add(kvp.Key, kvp.Value);

                if (General.IsSimpleIdentifier(word.Text))
                {
                    bindItem.InstanceName = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    // optional instance array range [ ... ]
                    if (word.Text == "[")
                    {
                        int bracketDepth = 1;
                        word.MoveNext();
                        while (!word.Eof && bracketDepth > 0)
                        {
                            if (word.Text == "[") bracketDepth++;
                            else if (word.Text == "]") bracketDepth--;
                            word.MoveNext();
                        }
                    }
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        parsePortConnections(word, nameSpace, instancedBuildingBlock, bindItem.PortConnections);
                    }
                }
                bind.BindItems.Add(bindItem);

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
                break;
            }

            if (word.Text != ";")
            {
                word.AddError("illegal name");
                word.SkipToKeyword(";");
                return true;
            }
            word.MoveNext();

            bind.LastIndexReference = word.CreateIndexReferenceBefore();
            if (!word.Prototype && word.CompletionContext == null && nameSpace != null) nameSpace.DocumentRegions.Add(bind);
            bindDirective = bind;
            return true;
        }

        /// <summary>
        /// consume "( ... )" block including nested parentheses
        /// </summary>
        private static void consumeParenBlock(WordScanner word)
        {
            int parenDepth = 1;
            while (!word.Eof && parenDepth > 0)
            {
                if (word.Text == "(") parenDepth++;
                else if (word.Text == ")") parenDepth--;
                word.MoveNext();
            }
        }

        /// <summary>
        /// parse port connections "( ... )" and record expressions into portConnections.
        /// supports named (.port(expression)) / ordered (expression { , expression }) forms.
        /// </summary>
        private static void parsePortConnections(
            WordScanner word,
            NameSpace? nameSpace,
            BuildingBlocks.BuildingBlock? instancedBuildingBlock,
            Dictionary<string, Expression> portConnections
            )
        {
            // (EOF just after "("): nothing to analyze
            if (word.Eof) return;

            if (word.Text == ".")
            { // named port connection
                while (!word.Eof && word.Text == ".")
                {
                    word.MoveNext();    // .
                    if (word.Text == "*")
                    { // wildcard: no per-port expression
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        if (word.Text == ",") word.MoveNext();
                        continue;
                    }
                    if (!General.IsSimpleIdentifier(word.Text))
                    {
                        word.AddError("illegal port name");
                        break;
                    }
                    string pinName = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        if (word.Text != ")")
                        {
                            if (nameSpace != null)
                            {
                                Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                                if (expression != null && !portConnections.ContainsKey(pinName)) portConnections.Add(pinName, expression);
                            }
                            else
                            {
                                consumeParenBlock(word);
                            }
                        }
                    }
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                        continue;
                    }
                    break;
                }
            }
            else
            { // ordered port connection: expression { , expression }
                int i = 0;
                while (!word.Eof && word.Text != ")")
                {
                    if (word.Text != ",")
                    {
                        string pinName = null;
                        IPortNameSpace? portNameSpace = instancedBuildingBlock as IPortNameSpace;
                        if (portNameSpace != null && portNameSpace.PortsList != null && i < portNameSpace.PortsList.Count)
                        {
                            pinName = portNameSpace.PortsList[i].Name;
                        }
                        if (nameSpace != null)
                        {
                            Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (expression != null && pinName != null && !portConnections.ContainsKey(pinName)) portConnections.Add(pinName, expression);
                        }
                        else
                        {
                            // root-level: consume expression tokens without analysis (nested parens safe)
                            consumeParenBlock(word);
                        }
                        i++;
                    }
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                        continue;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// parse hierarchical_identifier (identifier { . identifier }) and return it as string.
        /// returns null if the first token is not a simple identifier
        /// </summary>
        private static string parseHierarchicalIdentifier(WordScanner word)
        {
            if (!General.IsSimpleIdentifier(word.Text))
            {
                if (word.Text != ";") word.AddError("illegal name");
                return null;
            }

            string identifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            while (word.Text == ".")
            {
                word.MoveNext();
                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("illegal name");
                    return identifier;
                }
                identifier = identifier + "." + word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            return identifier;
        }

    }
}
