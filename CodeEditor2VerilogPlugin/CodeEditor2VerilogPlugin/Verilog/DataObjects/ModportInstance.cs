using AjkAvaloniaLibs.Controls;
using CodeEditor2.Data;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    internal class ModportInstance : DataObject, INamedElement
    {
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public static ModportInstance Create(string identifier,Interface interface_, ModPort modPort)
        {
            ModportInstance modportInstance = new ModportInstance() { Name = identifier, InterfaceName=interface_.Name, ModportName=modPort.Name };

            foreach (var element in modPort.NamedElements.Values)
            {
                modportInstance.NamedElements.Add(element.Name, element);
            }
            return modportInstance;
        }

        public required string InterfaceName { init; get; }
        public required string ModportName { init; get; }
        public override DataObject Clone()
        {
            ModportInstance modportInstance = new ModportInstance() { Name = Name, InterfaceName = InterfaceName, ModportName = ModportName };
            return modportInstance;
        }
    }
}
