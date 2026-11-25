
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog
{
    public class BuiltInMethod : INamedElement
    {
        protected BuiltInMethod() { }

        public static BuiltInMethod Create(string name, Variable? returnValue, List<Port> ports)
        {
            BuiltInMethod builtInMethod = new BuiltInMethod() { Name = name, ReturnVariable = returnValue };
            foreach (Port port in ports)
            {
                builtInMethod.Ports.Add(port.Name, port);
                builtInMethod.PortsList.Add(port);
            }
            return builtInMethod;
        }



        private Dictionary<string, DataObjects.Port> ports = new Dictionary<string, DataObjects.Port>();
        public Dictionary<string, DataObjects.Port> Ports { get { return ports; } }
        private List<DataObjects.Port> portsList = new List<DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get { return portsList; } }

        public required string Name { init; get; }

        public VerilogColorType ColorType { get { return VerilogColorType.Identifier; } }

        public required DataObjects.Variables.Variable? ReturnVariable { init; get; }

        NamedElements INamedElement.NamedElements { get; } = new NamedElements();

    }
}
