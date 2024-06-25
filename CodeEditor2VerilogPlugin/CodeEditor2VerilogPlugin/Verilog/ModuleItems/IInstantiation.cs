using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public interface IInstantiation
    {
        string SourceName { get; }

        Dictionary<string, Expressions.Expression> ParameterOverrides { get; set; }
        Dictionary<string, Expressions.Expression> PortConnection { get; set; }
        string OverrideParameterID { get; }
        bool Prototype { get; set; }
        BuildingBlock GetInstancedBuildingBlock();
        void AppendLabel(IndexReference iref, AjkAvaloniaLibs.Contorls.ColorLabel label);

        IndexReference BeginIndexReference { get; }
        IndexReference LastIndexReference { get; }
        // Item
        string Name { get; set; }
        Attribute Attribute { get; set; }
        WordReference DefinitionReference { get; set; }

        CodeEditor2.Data.Project Project { get; }


    }
}
