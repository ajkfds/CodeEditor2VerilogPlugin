using pluginVerilog.Verilog.BuildingBlocks;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects
{
    internal class ModportInstance : DataObject, INamedElement
    {
        public override VerilogColorType ColorType { get { return VerilogColorType.Variable; } }
        public static ModportInstance Create(string identifier, Interface interface_, ModPort modPort)
        {
            ModportInstance modportInstance = new ModportInstance() { Name = identifier, ModPort = modPort, InterfaceName = interface_.Name, ModportName = modPort.Name };

            foreach (var element in modPort.NamedElements.Values)
            {
                modportInstance.NamedElements.Add(element.Name, element);
            }
            return modportInstance;
        }

        public required ModPort ModPort { init; get; }
        public required string InterfaceName { init; get; }
        public required string ModportName { init; get; }

        // substrの戻り値がStringを持つため、遅延評価される必要がある。
        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;
                namedElements = new NamedElements();

                foreach (var element in ModPort.NamedElements.Values)
                {
                    namedElements.Add(element.Name, element);
                }
                return namedElements;
            }
        }

        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText(InterfaceName, VerilogColorType.Keyword);
            label.AppendText(".");
            label.AppendText(ModportName, VerilogColorType.Keyword);
            label.AppendText(" ");
        }
        public override void AppendLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            AppendTypeLabel(label);
            label.AppendText(Name, VerilogColorType.Variable);

            foreach (Arrays.VariableArray dimension in UnpackedArrays)
            {
                if (dimension == null) continue;
                label.AppendText(" ");
                label.AppendLabel(dimension.GetLabel());
            }

            if (Comment != "")
            {
                label.AppendText(" ");
                label.AppendText(Comment.Trim(new char[] { '\r', '\n', '\t', ' ' }), VerilogColorType.Comment);
            }
            label.AppendText("\r\n");

            SyncContext.AppendLabel(label);
        }
        public override DataObject Clone()
        {
            return Clone(Name);
        }
        public override DataObject Clone(string name)
        {
            ModportInstance modportInstance = new ModportInstance() { Name = name, InterfaceName = InterfaceName, ModPort = ModPort, ModportName = ModportName, Defined = Defined };
            return modportInstance;
        }
    }
}
