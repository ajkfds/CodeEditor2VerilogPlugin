using System;

namespace pluginVerilog.Data
{
    /// <summary>
    /// Host-side lookup for a <see cref="SystemVerilogCore.Verilog.VerilogRelatedFileReference"/>.
    ///
    /// <para>
    /// The reference itself is plain data (project name + relative path + id) and
    /// lives in the UI-free parser assembly. Resolving it back to a live file
    /// needs the editor's project registry (<c>CodeEditor2.Global.Projects</c>),
    /// which that assembly cannot reach, so the lookup stays here as an extension
    /// method. The language server has its own registry and does not use this.
    /// </para>
    /// </summary>
    public static class VerilogRelatedFileReferenceExtensions
    {
        /// <summary>
        /// The live file this reference points at, or null when its project or
        /// the file is no longer loaded.
        /// </summary>
        public static IVerilogRelatedFile? GetTargetFile(
            this SystemVerilogCore.Verilog.VerilogRelatedFileReference reference)
        {
            if (reference == null) return null;
            if (!CodeEditor2.Global.Projects.TryGetValue(reference.ProjectName, out CodeEditor2.Data.Project? project))
            {
                return null;
            }
            return project.GetItemHierarchy(reference.RelativeFilePath) as IVerilogRelatedFile;
        }
    }
}
