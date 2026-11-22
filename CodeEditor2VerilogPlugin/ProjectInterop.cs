using System;

namespace pluginVerilog
{
    /// <summary>
    /// Bridges the UI-free <see cref="SystemVerilogCore.Projects.IVerilogProject"/>
    /// back to the editor's concrete project type.
    ///
    /// <para>
    /// The parser works against the interface so it does not depend on the
    /// editor assembly. A few editor-side code paths (the navigate-panel
    /// updater, the completion popup) still need the concrete
    /// <c>CodeEditor2.Data.Project</c> to reach editor-only services. This
    /// helper centralises that single downcast instead of scattering
    /// <c>(Project)</c> casts around.
    /// </para>
    ///
    /// <para>
    /// The cast always succeeds in the editor: the only implementation of the
    /// interface in this process is <c>CodeEditor2.Data.Project</c> (or a
    /// subclass of it). It returns null when a host supplies some other
    /// implementation, so callers must handle null rather than assume.
    /// </para>
    /// </summary>
    public static class ProjectInterop
    {
        /// <summary>
        /// Returns the editor project behind <paramref name="project"/>, or
        /// null when it is not an editor project (for example in a headless
        /// host that has its own implementation).
        /// </summary>
        public static CodeEditor2.Data.Project? AsEditorProject(
            SystemVerilogCore.Projects.IVerilogProject? project)
        {
            return project as CodeEditor2.Data.Project;
        }
    }
}
