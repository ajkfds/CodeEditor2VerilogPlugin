using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Nets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface INameSpace
    {
        public IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; }
        public Dictionary<string, DataObjects.DataObject> DataObjects { get; }

        public NameSpace? Parent { get; init; }

        public Dictionary<string, DataObjects.Constants.Constants> Constants { get; }
        public Dictionary<string, DataObjects.Typedef> Typedefs { get; }
        public Dictionary<string, BuildingBlocks.Class> Classes { get; }
        public BuildingBlocks.BuildingBlock BuildingBlock { get; protected set; }
        public Dictionary<string, INameSpace> NameSpaces { get; }

        public NameSpace GetHierarchyNameSpace(IndexReference iref);
        public void AppendAutoCompleteItem(List<AutocompleteItem> items);
        public DataObjects.DataObject? GetDataObject(string identifier);
        public DataObjects.Constants.Constants? GetConstants(string identifier);

    }
}
