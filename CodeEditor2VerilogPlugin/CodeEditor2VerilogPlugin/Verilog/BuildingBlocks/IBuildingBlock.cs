using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public interface IBuildingBlock
    {
        // NameSpace

        NamedElements NamedElements { get; }
        NameSpace Parent { get; }
        NameSpace? GetHierarchyNameSpace(IndexReference index);
        DataObjects.Constants.Constants? GetConstants(string identifier);

        // Bulding Block
//        Dictionary<string, Function> Functions { get; }
//        Dictionary<string, Task> Tasks { get; }
    }
}
