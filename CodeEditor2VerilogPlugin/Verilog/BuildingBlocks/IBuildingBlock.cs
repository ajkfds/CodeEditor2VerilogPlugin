using System.Collections.Generic;
using System.Collections.Concurrent;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public interface IBuildingBlock
    {
        // IModuleOrInterfaceOrProgram にbuilding blockのinterfaceをもたせるために使用

        NamedElements NamedElements { get; }

        // definition tree parents
        NameSpace Parent { get; }
        // definition tree chiledren
        ConcurrentDictionary<string, BuildingBlock> BuildingBlocks { get; }

        NameSpace? GetHierarchyNameSpace(IndexReference index);
        DataObjects.Constants.Constants? GetConstants(string identifier);

        // Bulding Block
        //        Dictionary<string, Function> Functions { get; }
        //        Dictionary<string, Task> Tasks { get; }
    }
}
