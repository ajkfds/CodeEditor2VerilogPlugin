using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog
{
    public class NamedItem
    {
        public required string Name { get; init; }
        public Attribute? Attribute { get; set; }
        public required WordReference? DefinitionReference { get; init; }

        /// <summary>
        /// The project this item belongs to.
        ///
        /// <para>
        /// Typed as the UI-free <see cref="SystemVerilogCore.Projects.IVerilogProject"/>
        /// so the parser does not depend on the editor's project type. The
        /// editor's <c>CodeEditor2.Data.Project</c> and the language server's
        /// per-workspace project both implement it.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public required SystemVerilogCore.Projects.IVerilogProject Project { get; init; }

        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        /// <summary>
        /// The settings of this item's project, as the parser-facing contract.
        ///
        /// <para>
        /// Typed as <see cref="IVerilogParserSettings"/> rather than the plugin's
        /// concrete <c>ProjectProperty</c> so parse sites that read it (the
        /// completion candidates and the comment annotations) do not name the
        /// plugin type.
        /// </para>
        /// </summary>
        [JsonIgnore]
        public IVerilogParserSettings ProjectProperty
        {
            get
            {
                IVerilogParserSettings? projectProperty = Project.Settings as IVerilogParserSettings;
                if (projectProperty == null) throw new Exception();
                return projectProperty;
            }
        }
    }
}
