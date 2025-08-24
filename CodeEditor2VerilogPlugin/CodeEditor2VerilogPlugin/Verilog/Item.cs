﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class Item
    {
        public required string Name { get; init;  }
        public Attribute? Attribute { get; set; }
        public required WordReference? DefinitionReference { get; init; }
        public required CodeEditor2.Data.Project Project { get; init; }

        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        public ProjectProperty ProjectProperty
        {
            get
            {
                ProjectProperty? projectPropery = Project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
                if (projectPropery == null) throw new Exception();
                return projectPropery;
            }
        }
    }
}
