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

            string target = word.Text;
            BuildingBlocks.BuildingBlock? targetBuildingBlock = word.ProjectProperty.GetBuildingBlock(target);
            if(targetBuildingBlock != null)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text == ":")
                {
                    word.MoveNext();

                    // TODO: implement
                }
            }
            else
            {
                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace,true);

                targetBuildingBlock = word.ProjectProperty.GetBuildingBlock(word.Text);
                if(targetBuildingBlock == null)
                {
                    word.AddError("unfound");
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("illegal name");
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text != "(")
                {
                    return true;
                }
                word.MoveNext();

                if(word.Text != ")")
                {
                    return true;
                }
                word.MoveNext();

                if (word.Text != ";")
                {
                    return true;
                }
                word.MoveNext();

            }

            bindDirective = bind;
            return true;
        }

    }
}
