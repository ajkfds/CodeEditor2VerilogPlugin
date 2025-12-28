using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog
{
    public class BuiltInMethod : INamedElement
    {
        protected BuiltInMethod() { }

        public static BuiltInMethod Create(string name, Variable? returnValue, List<Port> ports)
        {
            BuiltInMethod builtInMethod = new BuiltInMethod() { Name=name, ReturnVariable=returnValue };
            foreach(Port port in ports)
            {
                builtInMethod.Ports.Add(port.Name,port);
                builtInMethod.PortsList.Add(port);
            }
            return builtInMethod;
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        private Dictionary<string, DataObjects.Port> ports = new Dictionary<string, DataObjects.Port>();
        public Dictionary<string, DataObjects.Port> Ports { get { return ports; } }
        private List<DataObjects.Port> portsList = new List<DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get { return portsList; } }

        public required string Name { init; get; }

        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Identifier; } }

        public required DataObjects.Variables.Variable? ReturnVariable { init; get; }

        NamedElements INamedElement.NamedElements { get; } = new NamedElements();

    }
}
