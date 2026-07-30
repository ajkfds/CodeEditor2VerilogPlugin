using System.Collections.Generic;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public interface IModuleOrInterfaceOrProgram : IBuildingBlock
    {

        // Port
        Dictionary<string, DataObjects.Port> Ports { get; }
        List<DataObjects.Port> PortsList { get; }
        List<string> PortParameterNameList { get; }

        // Generate


    }
}
