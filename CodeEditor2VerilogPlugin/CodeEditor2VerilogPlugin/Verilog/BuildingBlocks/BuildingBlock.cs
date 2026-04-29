using pluginVerilog.Verilog.DataObjects.Nets;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class BuildingBlock : NameSpace, INamedElement
    {
        protected BuildingBlock(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }

        /// <summary>
        /// Lock for thread-safe access to BuildingBlocks dictionary
        /// </summary>
        protected readonly object buildingBlocksLock = new object();

        #region IDesignElementContainer

        /// <summary>
        /// Sub building block container
        /// </summary>
        public Dictionary<string, BuildingBlock> BuildingBlocks { get; set; } = new Dictionary<string, BuildingBlock>();

        /// <summary>
        /// Atomically adds or updates a building block in the dictionary.
        /// Thread-safe operation using lock.
        /// </summary>
        /// <param name="name">The name/key of the building block</param>
        /// <param name="buildingBlock">The building block to add or update</param>
        /// <returns>True if added, false if updated</returns>
        public bool AddOrUpdateBuildingBlock(string name, BuildingBlock buildingBlock)
        {
            lock (buildingBlocksLock)
            {
                if (BuildingBlocks.ContainsKey(name))
                {
                    BuildingBlocks[name] = buildingBlock;
                    return false;
                }
                else
                {
                    BuildingBlocks.Add(name, buildingBlock);
                    return true;
                }
            }
        }

        /// <summary>
        /// Atomically gets a building block by name.
        /// Thread-safe operation using lock.
        /// </summary>
        /// <param name="name">The name/key of the building block</param>
        /// <returns>The building block if found, null otherwise</returns>
        public BuildingBlock? GetBuildingBlock(string name)
        {
            lock (buildingBlocksLock)
            {
                if (BuildingBlocks.TryGetValue(name, out var block))
                {
                    return block;
                }
                return null;
            }
        }

        /// <summary>
        /// Atomically checks if a building block exists.
        /// Thread-safe operation using lock.
        /// </summary>
        /// <param name="name">The name/key to check</param>
        /// <returns>True if exists, false otherwise</returns>
        public bool ContainsBuildingBlock(string name)
        {
            lock (buildingBlocksLock)
            {
                return BuildingBlocks.ContainsKey(name);
            }
        }

        public virtual string FileId { get; protected set; }

        [JsonIgnore]
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

        [JsonIgnore]
        public virtual Data.IVerilogRelatedFile File { get; init; }



        #endregion

        public static void CheckVariablesUseAndDriven(WordScanner word, NameSpace nameSpace)
        {
            foreach (var element in nameSpace.NamedElements.Values)
            {
                if (element is Net)
                {
                    Net net = (Net)element;
                    if (net.DefinedReference == null) continue;
                    if (net.AssignedMap?.IsFullMapped() != true)
                    {
                        net.DefinedReference.AddNotice("undriven");
                    }

                    if (net.UsedReferences.Count == 0)
                    {
                        net.DefinedReference.AddNotice("unused");
                    }
                    //if (net.AssignedReferences.Count == 0)
                    //{
                    //    if (net.UsedReferences.Count == 0)
                    //    {
                    //        net.DefinedReference.AddNotice("undriven & unused");
                    //    }
                    //    else
                    //    {
                    //        net.DefinedReference.AddNotice("undriven");
                    //    }
                    //}
                    //else
                    //{
                    //    if (net.UsedReferences.Count == 0)
                    //    {
                    //        net.DefinedReference.AddNotice("unused");
                    //    }
                    //}
                    continue;
                }

                DataObjects.Variables.ValueVariable? valueVar = element as DataObjects.Variables.ValueVariable;
                if (valueVar == null) continue;
                if (valueVar.DefinedReference == null) continue;

                if (valueVar.AssignedMap?.IsFullMapped() != true)
                {
                    valueVar.DefinedReference.AddNotice("undriven");
                }

                if (valueVar.UsedReferences.Count == 0)
                {
                    valueVar.DefinedReference.AddNotice("unused");
                }

                //if (valueVar.AssignedReferences.Count == 0)
                //{
                //    if (valueVar.UsedReferences.Count == 0)
                //    {
                //        valueVar.DefinedReference.AddNotice("undriven & unused");
                //    }
                //    else
                //    {
                //        valueVar.DefinedReference.AddNotice("undriven");
                //    }
                //}
                //else
                //{
                //    if (valueVar.UsedReferences.Count == 0)
                //    {
                //        valueVar.DefinedReference.AddNotice("unused");
                //    }
                //}
            }
        }

    }
}
