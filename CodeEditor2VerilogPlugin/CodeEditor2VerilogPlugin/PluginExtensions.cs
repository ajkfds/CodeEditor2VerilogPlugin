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
