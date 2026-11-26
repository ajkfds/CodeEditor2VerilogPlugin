
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Editor-side entry point for the UI-free parse engine.
    ///
    /// <para>
    /// The parse itself lives in <see cref="SystemVerilogCore.Parsing.ParseEngine"/>:
    /// it drives the real <c>VerilogParser</c> on the document contracts and
    /// registers the result into the project name spaces, so a host without an
    /// Avalonia dispatcher (the language server, the tests) parses without the
    /// editor assembly. This class used to hold a second, independent copy of
    /// that logic, which is exactly the kind of duplicate that drifts: the two
    /// engines had already diverged (only one registered the root elements and
    /// the instance key).
    /// </para>
    ///
    /// <para>
    /// What remains here is the convenience of taking the editor's own file type
    /// (<see cref="Data.VerilogFile"/>, which carries a concrete buffer) and
    /// handing the engine the pieces it needs. Callers that already speak the
    /// contracts should call <see cref="SystemVerilogCore.Parsing.ParseEngine"/>
    /// directly.
    /// </para>
    /// </summary>
    public static class ParseEngine
    {
        /// <summary>
        /// Parses <paramref name="text"/> and returns the result.
        /// </summary>
        /// <param name="text">The file text to parse.</param>
        /// <param name="file">The editor file being parsed.</param>
        /// <param name="cancellationToken">Cancellation token, or null.</param>
        public static Task<Verilog.ParsedDocument?> ParseSystemVerilogAsync(
            string text,
            Data.VerilogFile file,
            CancellationToken? cancellationToken = null)
        {
            return SystemVerilogCore.Parsing.ParseEngine.ParseSystemVerilogAsync(
                text, file, cancellationToken);
        }

        /// <summary>
        /// Parses <paramref name="text"/> as one parameter-overridden instance of
        /// <paramref name="moduleName"/>.
        /// </summary>
        /// <param name="text">The file text to parse.</param>
        /// <param name="file">The editor file being parsed.</param>
        /// <param name="moduleName">The instantiated building block.</param>
        /// <param name="parameterOverrides">The overrides written at the instantiation.</param>
        /// <param name="cancellationToken">Cancellation token, or null.</param>
        /// <param name="hostDocument">
        /// The host's buffer, when it has one. A parse drops every message whose
        /// buffer belongs to no file, so a host that reports diagnostics has to
        /// pass its own buffer here.
        /// </param>
        public static Task<Verilog.ParsedDocument?> ParseInstanceAsync(
            string text,
            Data.VerilogFile file,
            string moduleName,
            Dictionary<string, Verilog.Expressions.Expression> parameterOverrides,
            CancellationToken? cancellationToken = null,
            IVerilogTextDocument? hostDocument = null)
        {
            return SystemVerilogCore.Parsing.ParseEngine.ParseInstanceAsync(
                text, file, moduleName, parameterOverrides, cancellationToken, hostDocument);
        }
    }
}
