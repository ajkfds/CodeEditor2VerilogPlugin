using System.Collections.Generic;
using SystemVerilogCore;
using SystemVerilogCore.Documents;
using SystemVerilogCore.Diagnostics;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that exposes a plugin-side <see cref="Verilog.ParsedDocument"/>
    /// through the UI-agnostic <see cref="ISystemVerilogDocument"/> surface.
    /// The diagnostics list is wired to the parser's accumulated
    /// <c>Messages</c>; everything else (members, building blocks,
    /// element-at-index) is currently empty in this first cut and will be
    /// expanded in follow-up changes.
    /// </summary>
    public sealed class SystemVerilogDocumentAdapter : ISystemVerilogDocument
    {
        public SystemVerilogDocumentAdapter(SystemVerilogFileAdapter file, Verilog.ParsedDocument parsed)
        {
            File = file;
            ParsedDocument = parsed;
            Root = new RootBlockAdapter(file);
            Diagnostics = new List<ISystemVerilogDiagnostic>(BuildDiagnostics(parsed));
        }

        public new SystemVerilogFileAdapter File { get; }
        public Verilog.ParsedDocument ParsedDocument { get; }
        public ISystemVerilogBuildingBlock Root { get; }
        public IReadOnlyList<ISystemVerilogDiagnostic> Diagnostics { get; }

        public ISystemVerilogNamedElement? FindElementAt(int index) => null;

        ISystemVerilogFile ISystemVerilogDocument.File => File;

        private static IEnumerable<ISystemVerilogDiagnostic> BuildDiagnostics(Verilog.ParsedDocument parsed)
        {
            if (parsed.Messages == null) yield break;
            foreach (Verilog.ParsedDocument.Message message in parsed.Messages)
            {
                yield return new DiagnosticAdapter(message);
            }
        }
    }

    internal sealed class DiagnosticAdapter : ISystemVerilogDiagnostic
    {
        public DiagnosticAdapter(Verilog.ParsedDocument.Message message)
        {
            Message = message.Text ?? string.Empty;
            Severity = message.Type switch
            {
                Verilog.ParsedDocument.Message.MessageType.Error => SystemVerilogSeverity.Error,
                Verilog.ParsedDocument.Message.MessageType.Warning => SystemVerilogSeverity.Warning,
                Verilog.ParsedDocument.Message.MessageType.Notice => SystemVerilogSeverity.Information,
                Verilog.ParsedDocument.Message.MessageType.Hint => SystemVerilogSeverity.Hint,
                _ => SystemVerilogSeverity.Hint,
            };
            Code = string.Empty;
            Range = new SystemVerilogRange(message.Index, message.Length);
        }

        public SystemVerilogSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public SystemVerilogRange Range { get; }
    }

    internal sealed class RootBlockAdapter : ISystemVerilogBuildingBlock
    {
        public RootBlockAdapter(SystemVerilogFileAdapter file)
        {
            File = file;
        }

        public string Name => "$root";
        public SystemVerilogBuildingBlockKind Kind => SystemVerilogBuildingBlockKind.Root;
        public SystemVerilogRange? DefinitionRange => null;
        public IReadOnlyDictionary<string, ISystemVerilogBuildingBlock> BuildingBlocks { get; }
            = new Dictionary<string, ISystemVerilogBuildingBlock>();
        public IReadOnlyList<ISystemVerilogNamedElement> Members { get; }
            = System.Array.Empty<ISystemVerilogNamedElement>();
        public ISystemVerilogBuildingBlock? Owner => null;
        public new SystemVerilogFileAdapter File { get; }

        SystemVerilogNamedElementKind ISystemVerilogNamedElement.Kind => SystemVerilogNamedElementKind.Unknown;
        ISystemVerilogFile? ISystemVerilogNamedElement.File => File;
    }
}
