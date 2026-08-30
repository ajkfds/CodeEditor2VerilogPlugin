using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CodeEditor2.Data;
using SystemVerilogCore;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Plugin-side <see cref="ISystemVerilogCore"/> that hands out adapter
    /// views of the editor's <see cref="Project"/> objects. The plugins
    /// remain the source of truth for parsing and symbol resolution; this
    /// class only exposes them through the LSP-friendly interface so the
    /// language server (or any other tool) can consume them.
    /// </summary>
    public sealed class VerilogSystemVerilogCore : ISystemVerilogCore
    {
        private readonly ConcurrentDictionary<string, SystemVerilogProjectAdapter> _projects = new();

        public Task<ISystemVerilogProject> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
        {
            // In this first cut the language server hands us a synthetic
            // "default" id. If the caller knows the real <see cref="Project"/>
            // (e.g. an embedded mode), it can use <see cref="Wrap"/> to obtain
            // a real adapter.
            SystemVerilogProjectAdapter adapter = _projects.GetOrAdd(projectId, _ => new SystemVerilogProjectAdapter(null!));
            return Task.FromResult<ISystemVerilogProject>(adapter);
        }

        /// <summary>
        /// Wraps a plugin-side <see cref="Project"/> so that the language
        /// server can drive it. The same instance is returned on subsequent
        /// calls with the same <paramref name="projectId"/>.
        /// </summary>
        public SystemVerilogProjectAdapter Wrap(Project project, string projectId)
        {
            return _projects.GetOrAdd(projectId, _ => new SystemVerilogProjectAdapter(project));
        }
    }
}
