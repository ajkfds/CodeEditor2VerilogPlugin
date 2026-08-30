using SystemVerilogCore;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Adapter that exposes a plugin-side
    /// <see cref="pluginVerilog.Verilog.INamedElement"/> through the
    /// UI-agnostic <see cref="ISystemVerilogNamedElement"/> surface.
    ///
    /// The factory <see cref="TryCreate"/> inspects the concrete type and
    /// returns a tailored adapter so that <see cref="Kind"/> and the
    /// <see cref="ISystemVerilogNamedElement.Owner"/> information stay
    /// accurate.
    /// </summary>
    internal abstract class NamedElementAdapter : ISystemVerilogNamedElement
    {
        protected NamedElementAdapter(pluginVerilog.Verilog.INamedElement element, SystemVerilogFileAdapter file)
        {
            Element = element;
            File = file;
        }

        public pluginVerilog.Verilog.INamedElement Element { get; }

        public SystemVerilogFileAdapter File { get; }

        ISystemVerilogFile? ISystemVerilogNamedElement.File => File;

        public string Name => Element.Name ?? string.Empty;

        public abstract SystemVerilogNamedElementKind Kind { get; }

        public virtual SystemVerilogRange? DefinitionRange
        {
            get
            {
                if (Element is pluginVerilog.Verilog.NamedItem namedItem && namedItem.DefinitionReference != null)
                {
                    pluginVerilog.Verilog.WordReference def = namedItem.DefinitionReference;
                    int length = def.Length > 0 ? def.Length : Name.Length;
                    return new SystemVerilogRange(def.Index, length);
                }
                return null;
            }
        }

        public virtual ISystemVerilogBuildingBlock? Owner => null;

        /// <summary>
        /// Builds an adapter for the given element, or returns <c>null</c> when
        /// the element does not have a meaningful LSP representation (e.g. an
        /// anonymous scope or a generated wrapper).
        /// </summary>
        public static ISystemVerilogNamedElement? TryCreate(
            pluginVerilog.Verilog.INamedElement element,
            SystemVerilogFileAdapter file)
        {
            if (element == null) return null;
            if (string.IsNullOrEmpty(element.Name)) return null;

            return element switch
            {
                pluginVerilog.Verilog.DataObjects.DataObject dataObject => new DataObjectNamedElementAdapter(dataObject, file),
                pluginVerilog.Verilog.DataObjects.Typedef typedef => new TypedefNamedElementAdapter(typedef, file),
                pluginVerilog.Verilog.Function function => new FunctionNamedElementAdapter(function, file),
                pluginVerilog.Verilog.Task_ task => new TaskNamedElementAdapter(task, file),
                pluginVerilog.Verilog.BuildingBlocks.BuildingBlock block => new BuildingBlockAdapter(block, file, BuildingBlockAdapter.MapBuildingBlockKind(block)),
                _ => new GenericNamedElementAdapter(element, file),
            };
        }
    }

    internal sealed class GenericNamedElementAdapter : NamedElementAdapter
    {
        public GenericNamedElementAdapter(pluginVerilog.Verilog.INamedElement element, SystemVerilogFileAdapter file)
            : base(element, file) { }

        public override SystemVerilogNamedElementKind Kind => SystemVerilogNamedElementKind.Unknown;
    }

    internal sealed class DataObjectNamedElementAdapter : NamedElementAdapter
    {
        public DataObjectNamedElementAdapter(pluginVerilog.Verilog.DataObjects.DataObject element, SystemVerilogFileAdapter file)
            : base(element, file) { }

        private pluginVerilog.Verilog.DataObjects.DataObject DataObject =>
            (pluginVerilog.Verilog.DataObjects.DataObject)Element;

        public override SystemVerilogNamedElementKind Kind
        {
            get
            {
                if (DataObject is pluginVerilog.Verilog.DataObjects.Nets.Net) return SystemVerilogNamedElementKind.Net;
                if (DataObject is pluginVerilog.Verilog.DataObjects.Port) return SystemVerilogNamedElementKind.Port;
                if (DataObject is pluginVerilog.Verilog.DataObjects.Constants.Parameter) return SystemVerilogNamedElementKind.Parameter;
                if (DataObject is pluginVerilog.Verilog.DataObjects.Constants.Localparam) return SystemVerilogNamedElementKind.LocalParameter;
                return SystemVerilogNamedElementKind.Variable;
            }
        }
    }

    internal sealed class TypedefNamedElementAdapter : NamedElementAdapter
    {
        public TypedefNamedElementAdapter(pluginVerilog.Verilog.DataObjects.Typedef element, SystemVerilogFileAdapter file)
            : base(element, file) { }

        public override SystemVerilogNamedElementKind Kind => SystemVerilogNamedElementKind.Typedef;
    }

    internal sealed class FunctionNamedElementAdapter : NamedElementAdapter
    {
        public FunctionNamedElementAdapter(pluginVerilog.Verilog.Function element, SystemVerilogFileAdapter file)
            : base(element, file) { }

        public override SystemVerilogNamedElementKind Kind => SystemVerilogNamedElementKind.Function;
    }

    internal sealed class TaskNamedElementAdapter : NamedElementAdapter
    {
        public TaskNamedElementAdapter(pluginVerilog.Verilog.Task_ element, SystemVerilogFileAdapter file)
            : base(element, file) { }

        public override SystemVerilogNamedElementKind Kind => SystemVerilogNamedElementKind.Task;
    }
}
