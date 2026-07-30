using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    public interface IPortNameSpace
    {

        NamedElements NamedElements { get; }
        //        Dictionary<string, DataObjects.DataObject> DataObjects { get; }
        NameSpace Parent { get; }
        //        Dictionary<string, DataObjects.Constants.Constants> Constants { get; }

        BuildingBlocks.BuildingBlock BuildingBlock { get; }

        //        Dictionary<string, NameSpace> NameSpaces { get; }

        Dictionary<string, DataObjects.Port> Ports { get; }
        List<DataObjects.Port> PortsList { get; }
    }
}
