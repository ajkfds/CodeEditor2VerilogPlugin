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
    /// <c>Messages</c>; the root building block and <c>FindElementAt</c> are
    /// built on top of the parser's namespace tree.
    /// </summary>
    public sealed class SystemVerilogDocumentAdapter : ISystemVerilogDocument
    {
        public SystemVerilogDocumentAdapter(SystemVerilogFileAdapter file, Verilog.ParsedDocument parsed)
        {
            File = file;
            ParsedDocument = parsed;
            Root = parsed.Root != null
                ? new BuildingBlockAdapter(parsed.Root, file, SystemVerilogBuildingBlockKind.Root)
                : new EmptyRootBlockAdapter(file);
            Diagnostics = new List<ISystemVerilogDiagnostic>(BuildDiagnostics(parsed));
        }

        public new SystemVerilogFileAdapter File { get; }
        public Verilog.ParsedDocument ParsedDocument { get; }
        public ISystemVerilogBuildingBlock Root { get; }
        public IReadOnlyList<ISystemVerilogDiagnostic> Diagnostics { get; }

        public ISystemVerilogNamedElement? FindElementAt(int index)
        {
            pluginVerilog.Verilog.BuildingBlocks.Root? root = ParsedDocument.Root;
            if (root == null) return null;

            pluginVerilog.Verilog.IndexReference iref =
                pluginVerilog.Verilog.IndexReference.Create(ParsedDocument, ParsedDocument.CodeDocument, index);

            // Resolve the deepest namespace that contains the index.
            pluginVerilog.Verilog.NameSpace? ns = root.GetHierarchyNameSpace(iref);
            if (ns == null) return null;

            // Find the smallest item in that namespace that covers the index.
            pluginVerilog.Verilog.Items.IItem? item = null;
            pluginVerilog.Verilog.IndexReference? foundBegin = null;
            pluginVerilog.Verilog.IndexReference? foundLast = null;
            foreach (pluginVerilog.Verilog.Items.IItem candidate in ns.Items)
            {
                if (candidate.BeginIndexReference == null) continue;
                if (candidate.LastIndexReference == null) continue;
                if (iref.IsSmallerThan(candidate.BeginIndexReference)) continue;
                if (iref.IsGreaterThan(candidate.LastIndexReference)) continue;

                if (foundBegin != null && foundLast != null)
                {
                    if (candidate.BeginIndexReference.IsSmallerThan(foundBegin)) continue;
                    if (candidate.LastIndexReference.IsGreaterThan(foundLast)) continue;
                }

                item = candidate;
                foundBegin = candidate.BeginIndexReference;
                foundLast = candidate.LastIndexReference;
            }

            pluginVerilog.Verilog.INamedElement? element = item as pluginVerilog.Verilog.INamedElement
                ?? (pluginVerilog.Verilog.INamedElement?)item;
            if (element == null) return null;
            return NamedElementAdapter.TryCreate(element, File);
        }

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

    /// <summary>
    /// Minimal stand-in used when the parser has not produced a
    /// <c>Root</c> yet (e.g. the document has not been parsed).
    /// </summary>
    internal sealed class EmptyRootBlockAdapter : ISystemVerilogBuildingBlock
    {
        public EmptyRootBlockAdapter(SystemVerilogFileAdapter file)
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
        public SystemVerilogFileAdapter File { get; }

        SystemVerilogNamedElementKind ISystemVerilogNamedElement.Kind => SystemVerilogNamedElementKind.Unknown;
        ISystemVerilogFile? ISystemVerilogNamedElement.File => File;
    }
}
