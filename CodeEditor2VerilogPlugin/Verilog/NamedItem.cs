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

        [JsonIgnore]
        public ProjectProperty ProjectProperty
        {
            get
            {
                // Resolved through the UI-free settings view so this does not
                // depend on the editor's project type.
                ProjectProperty? projectProperty = Project.Settings as ProjectProperty;
                if (projectProperty == null) throw new Exception();
                return projectProperty;
            }
        }
    }
}
