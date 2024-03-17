using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    static class PluginExtensions
    {
        public static ProjectProperty GetPluginProperty(this CodeEditor2.Data.Project project)
        {
            return project.ProjectProperties[Plugin.StaticID] as ProjectProperty;
        }

    }
}
