using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.ModuleItems;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public interface IModuleOrInterface : IModuleOrInterfaceOrProgram
    {
//        Dictionary<string, IBuildingBlockInstantiation> Instantiations { get; }
    }
}
