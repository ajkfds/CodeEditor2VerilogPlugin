﻿using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class BuildingBlock : NameSpace, IBuildingBlock
    {
        protected BuildingBlock(BuildingBlock buildingBlock, NameSpace parent) :base(buildingBlock, parent)
        {
        }


        #region IDesignElementContainer

        public Dictionary<string, Function> Functions { get; set; } = new Dictionary<string, Function>();

        public Dictionary<string, Task> Tasks { get; set; } = new Dictionary<string, Task>();

//        public Dictionary<string, Class> Classes { get; set; } = new Dictionary<string, Class>();

        public Dictionary<string, IDataType> Datatypes { get; set; } = new Dictionary<string, IDataType>();

        public Dictionary<string, BuildingBlock> Elements { get; set; } = new Dictionary<string, BuildingBlock>();

        public Dictionary<string, IBuildingBlockInstantiation> Instantiations { get; } = new Dictionary<string, IBuildingBlockInstantiation>();


        public virtual string FileId { get; protected set; }

        public WordReference NameReference;
        public List<string> PortParameterNameList { get; } = new List<string>();

        public virtual List<string> GetExitKeywords()
        {
            return new List<string> { };
        }

        public bool AnsiStylePortDefinition { get; set; } = false;
        public Net.NetTypeEnum DefaultNetType = Net.NetTypeEnum.Wire;

        public required virtual Data.IVerilogRelatedFile File { get; init; }


        private bool reparseRequested = false;
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
