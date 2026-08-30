using System.Collections.Generic;
using CodeEditor2.CodeEditor;
using SystemVerilogCore;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that exposes a plugin-side <see cref="Data.IVerilogRelatedFile"/>
    /// through the UI-agnostic <see cref="ISystemVerilogFile"/> surface.
    /// Only the read-only surface is exposed; consumers go through
    /// <see cref="ISystemVerilogProject"/> for parse results and symbol
    /// lookups.
    /// </summary>
    public sealed class SystemVerilogFileAdapter : ISystemVerilogFile
    {
        public SystemVerilogFileAdapter(Data.IVerilogRelatedFile file)
        {
            File = file;
            CodeDocument? doc = file.CodeDocument;
            if (doc == null)
            {
                // Lazily create the adapter with a stub if the host has not
                // produced a code document yet. This keeps the adapter usable
                // for tools that only need metadata.
                CodeDocument = new SystemVerilogCodeDocumentAdapter(new CodeDocument());
            }
            else
            {
                CodeDocument = new SystemVerilogCodeDocumentAdapter(doc);
            }
        }

        public Data.IVerilogRelatedFile File { get; }

        public string Id => File.AbsolutePath ?? File.RelativePath;

        public string? AbsolutePath => File.AbsolutePath;

        public bool IsSystemVerilog => File.SystemVerilog;

        public ISystemVerilogCodeDocument CodeDocument { get; }

        public IReadOnlyList<ISystemVerilogBuildingBlock> TopLevelBlocks =>
            System.Array.Empty<ISystemVerilogBuildingBlock>();
    }
}
