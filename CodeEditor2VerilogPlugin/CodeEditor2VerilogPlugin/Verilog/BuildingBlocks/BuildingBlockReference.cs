using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.ModuleItems;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    public class BuildingBlockReference
    {
        public BuildingBlockReference() { }



        public BuildingBlock? ParseCrate(WordScanner word, NameSpace nameSpace)
        {
            if (word.NextText != "::" && word.NextText != "." && word.NextText != "#") return null;

            BuildingBlock buildingBlock = nameSpace.BuildingBlock;

            // get target building block
            BuildingBlock? targetBuildingBlock;
            INamedElement? namedElement = buildingBlock.GetNamedElementUpward(word.Text); // search upward
            if (namedElement == null || namedElement is not BuildingBlock)
            {
                targetBuildingBlock = word.ProjectProperty.GetBuildingBlock(word.Text);
            }
            else
            {
                targetBuildingBlock = buildingBlock as BuildingBlock;
            }

            if (word.NextText == "#") // parameter value assignment
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext(); // #

                Dictionary<string, Expressions.Expression> parameterOverrides = new Dictionary<string, Expressions.Expression>();
                ParameterValueAssignment.ParseCreate(word, nameSpace, parameterOverrides, targetBuildingBlock);
            }

            if (word.NextText == "::") // scope identifier
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext(); // ::

                if (targetBuildingBlock != null && targetBuildingBlock.BuildingBlocks.ContainsKey(word.Text))
                {
                    targetBuildingBlock = targetBuildingBlock.BuildingBlocks[word.Text];
                }
                return parse(word, nameSpace, targetBuildingBlock);
            }
            if (word.NextText == ".") // hierarchy identifier
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext(); // .
                if (targetBuildingBlock != null && targetBuildingBlock.NamedElements.ContainsKey(word.Text))
                {
                    INamedElement subNamedElement = targetBuildingBlock.NamedElements[word.Text];
                    if(subNamedElement is ModuleInstantiation)
                    {
                        ModuleInstantiation moduleInstantiation = (ModuleInstantiation)subNamedElement;
                        targetBuildingBlock = moduleInstantiation.GetInstancedBuildingBlock();
                    }
                }
                return parse(word, nameSpace, targetBuildingBlock);
            }
            return null;
        }
        private BuildingBlock? parse(WordScanner word, NameSpace nameSpace,BuildingBlock? targetBuildingBlock)
        {
            if (word.NextText != "::" && word.NextText != "." && word.NextText != "#") return targetBuildingBlock;




            /*
            var identifierReference = word.CrateWordReference();
            string identifier = word.Text;

            IndexReference beginIndexReference = word.CreateIndexReference();
            */

            return null;
        }



        private BuildingBlock? SearchBuildingBlockUpward(string name,BuildingBlock baseBuildingBlock)
        {
            if(baseBuildingBlock.BuildingBlocks.ContainsKey(name)) return baseBuildingBlock.BuildingBlocks[name];
            if (baseBuildingBlock.Parent == null || baseBuildingBlock.Parent.BuildingBlock == null) return null;
            return SearchBuildingBlockUpward(name, baseBuildingBlock.Parent.BuildingBlock);
        }
    }
}
