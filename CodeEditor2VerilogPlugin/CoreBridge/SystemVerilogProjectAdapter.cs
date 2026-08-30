using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeEditor2.Data;
using SystemVerilogCore;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that bridges a <see cref="CodeEditor2.Data.Project"/> to the
    /// UI-agnostic <see cref="ISystemVerilogProject"/> surface. The plugin
    /// owns the project tree; the adapter just looks the relevant file up
    /// in it and forwards calls. Symbol-level lookups (definition,
    /// references, documentSymbol) are stubbed in this first cut and will
    /// be wired to the real parser in a follow-up change.
    /// </summary>
    public sealed class SystemVerilogProjectAdapter : ISystemVerilogProject
    {
        public SystemVerilogProjectAdapter(Project project)
        {
            Project = project;
        }

        public Project Project { get; }

        public IReadOnlyList<ISystemVerilogFile> Files
        {
            get
            {
                List<ISystemVerilogFile> list = new();
                foreach (Data.IVerilogRelatedFile vf in EnumerateVerilogFiles(Project))
                {
                    list.Add(new SystemVerilogFileAdapter(vf));
                }
                return list;
            }
        }

        public ISystemVerilogFile? FindFile(string id)
        {
            foreach (Data.IVerilogRelatedFile vf in EnumerateVerilogFiles(Project))
            {
                string candidate = vf.AbsolutePath ?? vf.RelativePath;
                if (string.Equals(candidate, id, System.StringComparison.OrdinalIgnoreCase))
                {
                    return new SystemVerilogFileAdapter(vf);
                }
            }
            return null;
        }

        public Task<ISystemVerilogDocument?> GetDocumentAsync(ISystemVerilogFile file, CancellationToken cancellationToken = default)
        {
            if (file is SystemVerilogFileAdapter adapter)
            {
                Data.IVerilogRelatedFile verilogFile = adapter.File;
                Verilog.ParsedDocument? parsed = verilogFile.VerilogParsedDocument;
                if (parsed != null)
                {
                    return Task.FromResult<ISystemVerilogDocument?>(new SystemVerilogDocumentAdapter(adapter, parsed));
                }
            }
            return Task.FromResult<ISystemVerilogDocument?>(null);
        }

        public Task<ISystemVerilogNamedElement?> FindDefinitionAsync(ISystemVerilogFile file, int index, CancellationToken cancellationToken = default)
        {
            if (file is not SystemVerilogFileAdapter adapter) return Task.FromResult<ISystemVerilogNamedElement?>(null);
            pluginVerilog.Data.IVerilogRelatedFile verilogFile = adapter.File;
            pluginVerilog.Verilog.ParsedDocument? parsed = verilogFile.VerilogParsedDocument;
            if (parsed == null) return Task.FromResult<ISystemVerilogNamedElement?>(null);
            return Task.FromResult(SymbolResolver.FindDefinition(parsed, adapter, index));
        }

        public Task<IReadOnlyList<ISystemVerilogNamedElement>> FindReferencesAsync(ISystemVerilogFile file, int index, CancellationToken cancellationToken = default)
        {
            if (file is not SystemVerilogFileAdapter adapter) return Task.FromResult<IReadOnlyList<ISystemVerilogNamedElement>>(System.Array.Empty<ISystemVerilogNamedElement>());
            pluginVerilog.Data.IVerilogRelatedFile verilogFile = adapter.File;
            pluginVerilog.Verilog.ParsedDocument? parsed = verilogFile.VerilogParsedDocument;
            if (parsed == null) return Task.FromResult<IReadOnlyList<ISystemVerilogNamedElement>>(System.Array.Empty<ISystemVerilogNamedElement>());
            return Task.FromResult(SymbolResolver.FindReferences(parsed, adapter, index));
        }

        private static IEnumerable<Data.IVerilogRelatedFile> EnumerateVerilogFiles(Project project)
        {
            foreach (CodeEditor2.Data.Item item in project.Items)
            {
                foreach (CodeEditor2.Data.Item descendant in EnumerateDescendants(item))
                {
                    if (descendant is Data.IVerilogRelatedFile vf)
                    {
                        yield return vf;
                    }
                }
            }
        }

        private static IEnumerable<CodeEditor2.Data.Item> EnumerateDescendants(CodeEditor2.Data.Item root)
        {
            yield return root;
            foreach (CodeEditor2.Data.Item child in root.Items)
            {
                foreach (CodeEditor2.Data.Item descendant in EnumerateDescendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
