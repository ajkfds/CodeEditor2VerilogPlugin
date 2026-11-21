using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// One port row of the auto-connect form: the port, its current
    /// connection (if any) and the signals the editor would offer as a
    /// replacement.
    ///
    /// <para>
    /// This is the UI-free description of what the desktop editor's
    /// <c>AutoConnectWindow</c> shows. The window itself only renders these
    /// rows; deciding which signal matches a port lives in
    /// <see cref="AutoConnectAdapter"/>, so the LSP host and the desktop
    /// editor share one implementation.
    /// </para>
    ///
    /// <para>
    /// The model lives in the plugin (not in <c>SystemVerilogCore</c>) on
    /// purpose: the plugin is the only place that knows the parser, and
    /// <c>SystemVerilogCore</c> is the VS Code / LSP contract assembly. Keeping
    /// the model here means the plugin has no dependency on the LSP side at
    /// all - the language server consumes the plugin, never the other way
    /// round.
    /// </para>
    /// </summary>
    public sealed class AutoConnectPort
    {
        /// <summary>Port name as declared on the instantiated module.</summary>
        public string Name { get; init; } = "";

        /// <summary><c>input</c> / <c>output</c> / <c>inout</c> / <c>ref</c>.</summary>
        public string Direction { get; init; } = "";

        /// <summary>
        /// Port group name (the <c>// @group</c> annotation), or null. The
        /// editor prints a comment row whenever this changes.
        /// </summary>
        public string? PortGroup { get; init; }

        /// <summary>
        /// The connection currently written in the instantiation, or null when
        /// the port is not connected yet.
        /// </summary>
        public string? CurrentConnection { get; init; }

        /// <summary>
        /// Signals the editor offers for this port, best match first. The
        /// editor's rule is "a signal whose name equals the port name (or the
        /// port name without an <c>_I</c> / <c>_O</c> / <c>_IO</c> suffix)".
        /// </summary>
        public IReadOnlyList<string> Candidates { get; init; } = System.Array.Empty<string>();

        /// <summary>
        /// True when the port already has a connection and no better candidate
        /// exists, i.e. the row needs no user decision.
        /// </summary>
        public bool IsSettled { get; init; }
    }

    /// <summary>
    /// The whole auto-connect form: the instantiated module, the instance name
    /// and one row per port.
    /// </summary>
    public sealed class AutoConnectModel
    {
        /// <summary>Instantiated module name (the type).</summary>
        public string ModuleName { get; init; } = "";

        /// <summary>Instance name written in the instantiation.</summary>
        public string InstanceName { get; init; } = "";

        /// <summary>One row per port, in declaration order.</summary>
        public IReadOnlyList<AutoConnectPort> Ports { get; init; } = System.Array.Empty<AutoConnectPort>();

        /// <summary>
        /// Why the form could not be built (caret not on an instantiation,
        /// module not resolved, ...). Null when <see cref="Ports"/> is usable.
        /// </summary>
        public string? Message { get; init; }
    }
}
