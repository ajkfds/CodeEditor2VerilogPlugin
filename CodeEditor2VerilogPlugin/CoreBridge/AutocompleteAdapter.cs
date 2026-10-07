using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.CoreBridge
{
    /// <summary>
    /// Converts plugin-side named elements (the editor parser's symbol model)
    /// into UI-agnostic <see cref="ISystemVerilogAutocompleteItem"/> data that
    /// hosts such as the LSP server can consume without referencing UI types.
    /// </summary>
    public static class AutocompleteAdapter
    {
        /// <summary>
        /// Builds an autocomplete candidate for the given element, or returns
        /// <c>null</c> when the element has no meaningful candidate
        /// representation (unnamed, synthetic wrapper, ...).
        /// </summary>
        public static ISystemVerilogAutocompleteItem? TryCreate(pluginVerilog.Verilog.INamedElement element)
        {
            if (element == null) return null;
            if (string.IsNullOrEmpty(element.Name)) return null;

            SystemVerilogAutocompleteItemKind kind = MapKind(element);
            return new SystemVerilogAutocompleteItem(element.Name, kind);
        }

        /// <summary>
        /// Builds candidates for all named elements directly declared in the
        /// given building block. Unnamed / synthetic elements are skipped.
        /// </summary>
        public static List<ISystemVerilogAutocompleteItem> CreateMembers(
            pluginVerilog.Verilog.BuildingBlocks.BuildingBlock buildingBlock)
        {
            List<ISystemVerilogAutocompleteItem> items = new List<ISystemVerilogAutocompleteItem>();
            if (buildingBlock == null) return items;

            foreach (pluginVerilog.Verilog.INamedElement element in buildingBlock.NamedElements)
            {
                ISystemVerilogAutocompleteItem? item = TryCreate(element);
                if (item != null && !items.Contains(item)) items.Add(item);
            }
            return items;
        }

        private static SystemVerilogAutocompleteItemKind MapKind(pluginVerilog.Verilog.INamedElement element)
        {
            if (element is pluginVerilog.Verilog.DataObjects.DataObject dataObject)
            {
                if (dataObject is pluginVerilog.Verilog.DataObjects.Nets.Net) return SystemVerilogAutocompleteItemKind.Net;
                if (dataObject is pluginVerilog.Verilog.DataObjects.Port) return SystemVerilogAutocompleteItemKind.Port;
                if (dataObject is pluginVerilog.Verilog.DataObjects.Constants.Parameter) return SystemVerilogAutocompleteItemKind.Parameter;
                if (dataObject is pluginVerilog.Verilog.DataObjects.Constants.Localparam) return SystemVerilogAutocompleteItemKind.LocalParameter;
                return SystemVerilogAutocompleteItemKind.Variable;
            }
            if (element is pluginVerilog.Verilog.DataObjects.Typedef) return SystemVerilogAutocompleteItemKind.Typedef;
            if (element is pluginVerilog.Verilog.Function) return SystemVerilogAutocompleteItemKind.Function;
            if (element is pluginVerilog.Verilog.Task_) return SystemVerilogAutocompleteItemKind.Task;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Module) return SystemVerilogAutocompleteItemKind.Module;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Interface) return SystemVerilogAutocompleteItemKind.Interface;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Program) return SystemVerilogAutocompleteItemKind.Program;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Checker) return SystemVerilogAutocompleteItemKind.Checker;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Primitive) return SystemVerilogAutocompleteItemKind.Primitive;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Package) return SystemVerilogAutocompleteItemKind.Package;
            if (element is pluginVerilog.Verilog.BuildingBlocks.Class) return SystemVerilogAutocompleteItemKind.Class;
            return SystemVerilogAutocompleteItemKind.Unknown;
        }
    }
}
