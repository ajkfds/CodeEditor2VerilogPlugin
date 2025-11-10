using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class BuildingBlock : NameSpace, INamedElement
    {
        protected BuildingBlock(BuildingBlock buildingBlock, NameSpace parent) :base(buildingBlock, parent)
        {
        }


        #region IDesignElementContainer

        /// <summary>
        /// Sub building block container
        /// </summary>
        public Dictionary<string, BuildingBlock> BuildingBlocks { get; set; } = new Dictionary<string, BuildingBlock>();


        public virtual string FileId { get; protected set; }

        [Newtonsoft.Json.JsonIgnore]
        public WordReference NameReference;
        public List<string> PortParameterNameList { get; } = new List<string>();

        public virtual List<string> GetExitKeywords()
        {
            return new List<string> { };
        }

        public BuildingBlock? SearchBuildingBlockUpward(string name)
        {
            if (Parent == null || Parent.BuildingBlock == null) return null;
            if (Name == name) return this;
            if (this.BuildingBlocks.ContainsKey(name)) return this.BuildingBlocks[name];
            return Parent.BuildingBlock.SearchBuildingBlockUpward(name);
        }

        public bool AnsiStylePortDefinition { get; set; } = false;
        public Net.NetTypeEnum DefaultNetType = Net.NetTypeEnum.Wire;

        [Newtonsoft.Json.JsonIgnore]
        public virtual Data.IVerilogRelatedFile File { get; init; }


        private bool reparseRequested = false;
        [Newtonsoft.Json.JsonIgnore]
        public bool ReparseRequested
        {
            get
            {
                return reparseRequested;
            }
            set
            {
                reparseRequested = value;
            }
        }

        #endregion

        public static void CheckVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        {
            foreach (var element in nameSpace.NamedElements.Values)
            {
                if(element is Net)
                {
                    Net net = (Net)element;
                    if (net.DefinedReference == null) continue;
                    if (net.AssignedReferences.Count == 0)
                    {
                        if (net.UsedReferences.Count == 0)
                        {
                            net.DefinedReference.AddNotice("undriven & unused");
                        }
                        else
                        {
                            net.DefinedReference.AddNotice("undriven");
                        }
                    }
                    else
                    {
                        if (net.UsedReferences.Count == 0)
                        {
                            net.DefinedReference.AddNotice("unused");
                        }
                    }
                    continue;
                }

                DataObjects.Variables.ValueVariable? valueVar = element as DataObjects.Variables.ValueVariable;
                if (valueVar == null) continue;
                if (valueVar.DefinedReference == null) continue;

                if (valueVar.AssignedReferences.Count == 0)
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        valueVar.DefinedReference.AddNotice("undriven & unused");
                    }
                    else
                    {
                        valueVar.DefinedReference.AddNotice("undriven");
                    }
                }
                else
                {
                    if (valueVar.UsedReferences.Count == 0)
                    {
                        valueVar.DefinedReference.AddNotice("unused");
                    }
                }
            }
        }

    }
}
