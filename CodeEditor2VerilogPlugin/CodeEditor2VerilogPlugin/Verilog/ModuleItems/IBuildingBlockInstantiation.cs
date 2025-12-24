using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.ModuleItems
{
    public interface IBuildingBlockInstantiation
    {
        string SourceName { get; }

        Dictionary<string, Expressions.Expression> ParameterOverrides { get; init; }
        Dictionary<string, Expressions.Expression> PortConnection { get; set; }
        //string OverrideParameterID { get; }
        bool Prototype { get; set; }
        BuildingBlock? GetInstancedBuildingBlock();
        void AppendLabel(IndexReference iref, AjkAvaloniaLibs.Controls.ColorLabel label);

        IndexReference BeginIndexReference { get; }
        IndexReference? BlockBeginIndexReference { get; }
        IndexReference? LastIndexReference { get; }
        // Item
        string Name { get; init; }
        Attribute? Attribute { get; set; }
        WordReference? DefinitionReference { get; init; }

        CodeEditor2.Data.Project Project { get; }


    }
}
