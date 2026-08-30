using System.Collections.Generic;
using System.Collections.Concurrent;
using SystemVerilogCore;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that exposes a plugin-side
    /// <see cref="pluginVerilog.Verilog.BuildingBlocks.BuildingBlock"/> (and
    /// therefore <see cref="pluginVerilog.Verilog.BuildingBlocks.Module"/>,
    /// <see cref="pluginVerilog.Verilog.BuildingBlocks.Package"/>, ...) through
    /// the UI-agnostic <see cref="ISystemVerilogBuildingBlock"/> surface.
    ///
    /// The adapter is intentionally read-only: it exposes the block's
    /// <c>Name</c>, <c>Kind</c> and the set of nested building blocks and
    /// directly-declared named elements. Mutation is still performed by the
    /// plugin's parser and only the result is reflected here.
    /// </summary>
    internal sealed class BuildingBlockAdapter : ISystemVerilogBuildingBlock
    {
        public BuildingBlockAdapter(
            pluginVerilog.Verilog.BuildingBlocks.BuildingBlock buildingBlock,
            SystemVerilogFileAdapter file,
            SystemVerilogBuildingBlockKind kind)
        {
            BuildingBlock = buildingBlock;
            File = file;
            Kind = kind;
        }

        public pluginVerilog.Verilog.BuildingBlocks.BuildingBlock BuildingBlock { get; }

        public string Name => BuildingBlock.Name ?? string.Empty;

        public SystemVerilogBuildingBlockKind Kind { get; }

        public SystemVerilogRange? DefinitionRange
        {
            get
            {
                pluginVerilog.Verilog.WordReference? def = BuildingBlock.DefinitionReference;
                if (def == null) return null;
                int length = def.Length > 0 ? def.Length : Name.Length;
                return new SystemVerilogRange(def.Index, length);
            }
        }

        public SystemVerilogFileAdapter File { get; }

        ISystemVerilogFile? ISystemVerilogNamedElement.File => File;

        public ISystemVerilogBuildingBlock? Owner => null; // owner of a top-level BuildingBlock is the Root

        SystemVerilogNamedElementKind ISystemVerilogNamedElement.Kind => MapKind(Kind);

        public IReadOnlyDictionary<string, ISystemVerilogBuildingBlock> BuildingBlocks
        {
            get
            {
                ConcurrentDictionary<string, pluginVerilog.Verilog.BuildingBlocks.BuildingBlock> source =
                    BuildingBlock.BuildingBlocks;
                Dictionary<string, ISystemVerilogBuildingBlock> dict =
                    new Dictionary<string, ISystemVerilogBuildingBlock>(source.Count);
                foreach (KeyValuePair<string, pluginVerilog.Verilog.BuildingBlocks.BuildingBlock> kvp in source)
                {
                    dict[kvp.Key] = new BuildingBlockAdapter(kvp.Value, File, MapBuildingBlockKind(kvp.Value));
                }
                return dict;
            }
        }

        public IReadOnlyList<ISystemVerilogNamedElement> Members
        {
            get
            {
                pluginVerilog.Verilog.NamedElements source = BuildingBlock.NamedElements;
                List<ISystemVerilogNamedElement> list = new List<ISystemVerilogNamedElement>();
                foreach (pluginVerilog.Verilog.INamedElement element in source)
                {
                    // Skip nested namespaces / building blocks; they are exposed
                    // through BuildingBlocks above.
                    if (element is pluginVerilog.Verilog.BuildingBlocks.BuildingBlock) continue;
                    if (element is pluginVerilog.Verilog.NameSpace) continue;
                    ISystemVerilogNamedElement? adapted = NamedElementAdapter.TryCreate(element, File);
                    if (adapted != null) list.Add(adapted);
                }
                return list;
            }
        }

        internal static SystemVerilogBuildingBlockKind MapBuildingBlockKind(
            pluginVerilog.Verilog.BuildingBlocks.BuildingBlock block)
        {
            if (block is pluginVerilog.Verilog.BuildingBlocks.Module) return SystemVerilogBuildingBlockKind.Module;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Interface) return SystemVerilogBuildingBlockKind.Interface;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Package) return SystemVerilogBuildingBlockKind.Package;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Program) return SystemVerilogBuildingBlockKind.Program;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Checker) return SystemVerilogBuildingBlockKind.Checker;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Primitive) return SystemVerilogBuildingBlockKind.Primitive;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Class) return SystemVerilogBuildingBlockKind.Class;
            if (block is pluginVerilog.Verilog.BuildingBlocks.Root) return SystemVerilogBuildingBlockKind.Root;
            return SystemVerilogBuildingBlockKind.Unknown;
        }

        internal static SystemVerilogNamedElementKind MapKind(SystemVerilogBuildingBlockKind kind)
        {
            return kind switch
            {
                SystemVerilogBuildingBlockKind.Module => SystemVerilogNamedElementKind.Module,
                SystemVerilogBuildingBlockKind.Interface => SystemVerilogNamedElementKind.Interface,
                SystemVerilogBuildingBlockKind.Package => SystemVerilogNamedElementKind.Package,
                SystemVerilogBuildingBlockKind.Program => SystemVerilogNamedElementKind.Program,
                SystemVerilogBuildingBlockKind.Checker => SystemVerilogNamedElementKind.Checker,
                SystemVerilogBuildingBlockKind.Primitive => SystemVerilogNamedElementKind.Primitive,
                SystemVerilogBuildingBlockKind.Class => SystemVerilogNamedElementKind.Class,
                SystemVerilogBuildingBlockKind.GenerateBlock => SystemVerilogNamedElementKind.GenerateBlock,
                _ => SystemVerilogNamedElementKind.Unknown,
            };
        }
    }
}
