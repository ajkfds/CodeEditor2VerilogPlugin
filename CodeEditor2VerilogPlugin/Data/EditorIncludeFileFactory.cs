using SystemVerilogCore.Documents;
using SystemVerilogCore.Projects;

namespace pluginVerilog.Data
{
    /// <summary>
    /// Creates the editor's include instances on behalf of the parser.
    ///
    /// <para>
    /// The parser records one <see cref="IVerilogIncludeFile"/> per
    /// <c>include</c> directive, but the concrete type
    /// (<see cref="VerilogHeaderInstance"/>) is an item-tree node with a project
    /// and a parent file, so the UI-free assembly cannot create it. This factory
    /// is installed on <c>CodeEditor2.Data.Project</c> by
    /// <see cref="Plugin"/> and handed to the parser through
    /// <see cref="IVerilogProject.IncludeFileFactory"/>.
    /// </para>
    /// </summary>
    internal sealed class EditorIncludeFileFactory : IVerilogIncludeFileFactory
    {
        private readonly CodeEditor2.Data.Project project;

        public EditorIncludeFileFactory(CodeEditor2.Data.Project project)
        {
            this.project = project;
        }

        /// <summary>
        /// Creates the include instance, or null when the included file is not
        /// in the project's item tree (the parser reports that as "file not
        /// found").
        /// </summary>
        public IVerilogIncludeFile? Create(
            string relativePath,
            string name,
            IVerilogSourceFile parentFile,
            object? instancedReference,
            string id)
        {
            if (parentFile is not IVerilogRelatedFile editorParentFile) return null;

            // The parser owns the reference token; this factory is the one place
            // that knows it is the editor's IndexReference.
            if (instancedReference is not Verilog.IndexReference reference) return null;

            return VerilogHeaderInstance.Create(
                relativePath,
                name,
                reference,
                editorParentFile,
                project,
                id);
        }
    }
}
