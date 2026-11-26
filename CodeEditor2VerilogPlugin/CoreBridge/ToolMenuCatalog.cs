using System.Collections.Generic;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// The editor-side name for the tool menu catalog.
    ///
    /// <para>
    /// The catalog itself lives in
    /// <see cref="SystemVerilogCore.Snippets.ToolMenuCatalog"/>: the list is plain
    /// data, but it sat in this assembly, so the language server had to reference
    /// the editor stack to know which tools exist. This forwards every member so
    /// the editor's call sites keep their shape.
    /// </para>
    /// </summary>
    public static class ToolMenuCatalog
    {
        /// <summary>Stable tool identifiers (see the provider's catalog).</summary>
        public static class ToolId
        {
            /// <summary>An always block with the clock / reset from the ports.</summary>
            public const string AlwaysFF = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.AlwaysFF;

            /// <summary>Open the port list of the instantiation at the caret.</summary>
            public const string AutoConnect = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.AutoConnect;

            /// <summary>Reformat the instantiation at the caret.</summary>
            public const string AutoFormat = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.AutoFormat;

            /// <summary>Insert a module instance.</summary>
            public const string ModuleInstance = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.ModuleInstance;

            /// <summary>Fill the port connections of the instantiation at the caret.</summary>
            public const string PortConnectionCreate = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.PortConnectionCreate;

            /// <summary>Invert the direction / suffix of the selected ports.</summary>
            public const string PortInvert = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.PortInvert;

            /// <summary>Generate a modport from the enclosing interface.</summary>
            public const string ModPort = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.ModPort;

            /// <summary>Trace where a signal is connected (desktop only).</summary>
            public const string ConnectionCheck = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.ConnectionCheck;

            /// <summary>Upper-case the selection.</summary>
            public const string ToUpper = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.ToUpper;

            /// <summary>Lower-case the selection.</summary>
            public const string ToLower = SystemVerilogCore.Snippets.ToolMenuCatalog.ToolId.ToLower;
        }

        /// <summary>Every tool, in menu order.</summary>
        public static IReadOnlyList<SystemVerilogCore.Snippets.ToolMenuCatalog.Entry> All =>
            SystemVerilogCore.Snippets.ToolMenuCatalog.All;

        /// <summary>The tools the editor puts in its own list.</summary>
        public static IReadOnlyList<SystemVerilogCore.Snippets.ToolMenuCatalog.Entry> EditorTools =>
            SystemVerilogCore.Snippets.ToolMenuCatalog.EditorTools;

        /// <summary>The catalog entry for <paramref name="id"/>, or null.</summary>
        public static SystemVerilogCore.Snippets.ToolMenuCatalog.Entry? Find(string id) =>
            SystemVerilogCore.Snippets.ToolMenuCatalog.Find(id);
    }
}
