using System.Collections.Generic;
using System.Text;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter-specific <see cref="IHoverContentProvider"/> implementation
    /// that augments the default <see cref="HoverContent"/> output with
    /// detail the <see cref="SystemVerilogCore"/> interface does not
    /// expose:
    /// <list type="bullet">
    /// <item><see cref="DataObject"/>: <c>DataType</c> name and <c>BitWidth</c>.</item>
    /// <item><see cref="Port"/>: <c>Direction</c> and the port's own bit width.</item>
    /// <item><see cref="BuildingBlock"/>: list of declared ports.</item>
    /// </list>
    /// The provider is installed by <see cref="PluginHoverInstaller"/>.
    /// </summary>
    internal sealed class PluginHoverContentProvider : IHoverContentProvider
    {
        public string? Build(ISystemVerilogNamedElement element)
        {
            pluginVerilog.Verilog.INamedElement? underlying = ExtractUnderlying(element);
            if (underlying == null) return null;

            // Build the signature from the default helper, then splice the
            // extra detail block in. The result is identical to the
            // built-in hover text but with the type/width/direction/ports
            // hint added after the symbol.
            string? fallback = HoverContent.Build(element);
            if (fallback == null) return null;

            string extra = underlying switch
            {
                DataObject dataObject => FormatDataObject(dataObject),
                Port port => FormatPort(port),
                BuildingBlock block => FormatBuildingBlock(block),
                _ => string.Empty,
            };
            if (string.IsNullOrEmpty(extra)) return null;

            int fenceEnd = fallback.IndexOf("```\n", System.StringComparison.Ordinal);
            if (fenceEnd < 0) return fallback;
            int insertAt = fenceEnd + "```\n".Length;
            return fallback.Insert(insertAt, extra);
        }

        private static string FormatDataObject(DataObject dataObject)
        {
            StringBuilder sb = new StringBuilder();
            if (dataObject.DataType is IDataType dataType)
            {
                string typeName = dataType.GetType().Name;
                if (!string.IsNullOrEmpty(typeName))
                {
                    sb.Append("  // type: ");
                    sb.Append(typeName);
                }
            }
            if (dataObject.BitWidth.HasValue)
            {
                if (sb.Length > 0) sb.Append(", ");
                else sb.Append("  // ");
                sb.Append("width: ");
                sb.Append(dataObject.BitWidth.Value);
            }
            return sb.ToString();
        }

        private static string FormatPort(Port port)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("  // direction: ");
            sb.Append(port.Direction.ToString().ToLowerInvariant());
            int width = port.BitWidth;
            if (width > 0)
            {
                sb.Append(", width: ");
                sb.Append(width);
            }
            return sb.ToString();
        }

        private static string FormatBuildingBlock(BuildingBlock block)
        {
            List<string> portNames = new List<string>();
            foreach (pluginVerilog.Verilog.INamedElement element in block.NamedElements)
            {
                if (element is Port port && !string.IsNullOrEmpty(port.Name))
                {
                    portNames.Add(port.Name);
                }
            }
            if (portNames.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("  // ports: ");
            for (int i = 0; i < portNames.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(portNames[i]);
                if (i >= 4 && portNames.Count > 5)
                {
                    sb.Append(", … (+");
                    sb.Append(portNames.Count - 5);
                    sb.Append(" more)");
                    break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Pulls the original plugin element out of the adapter that
        /// <see cref="NamedElementAdapter"/> created. Returns <c>null</c>
        /// for adapters that do not expose a single underlying element
        /// (e.g. <see cref="SymbolResolver.ReferenceAdapter"/> which is
        /// just a <c>(name, range)</c> pair for a use site).
        /// </summary>
        private static pluginVerilog.Verilog.INamedElement? ExtractUnderlying(ISystemVerilogNamedElement element)
        {
            return element switch
            {
                NamedElementAdapter named => named.Element,
                BuildingBlockAdapter block => block.BuildingBlock,
                _ => null,
            };
        }
    }

    /// <summary>
    /// Entry point that registers <see cref="PluginHoverContentProvider"/>
    /// with <see cref="HoverContent"/>. Hosts that host the language
    /// server in-process (for example, an embedded LSP over stdin/stdout
    /// from the editor) can call this once at startup to install the
    /// richer hover descriptions.
    /// </summary>
    public static class PluginHoverInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            HoverContent.RegisterProvider(new PluginHoverContentProvider());
            _installed = true;
        }
    }
}
