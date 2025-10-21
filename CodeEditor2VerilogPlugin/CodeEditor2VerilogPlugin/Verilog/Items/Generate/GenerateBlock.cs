using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items.Generate
{
    public class GenerateBlock : NameSpace
    {
        protected GenerateBlock(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Identifier; } }

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "begin") return false;

            string name = "";

            // generate_block ::= begin[ : generate_block_identifier]  { generate_item } end
            word.Color(CodeDrawStyle.ColorType.Keyword);
            WordReference beginRef = word.GetReference();
            IndexReference beginIdexRef = word.CreateIndexReference();
            word.MoveNext();

            GenerateBlock? generateBlock = null;
            if (word.Text != ":")
            {
                beginRef.AddError(": required");
            }
            else
            {
                word.MoveNext();

                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("identifier required");
                    return true;
                }
                name = word.Text;

                if (word.Prototype)
                {
                    if (nameSpace.NamedElements.ContainsKey(name))
                    {
                        word.AddPrototypeError("duplicated block name");
                    }
                    else
                    {
                        generateBlock = new GenerateBlock(nameSpace.BuildingBlock, nameSpace)
                        {
                            BeginIndexReference = beginIdexRef,
                            DefinitionReference = beginRef,
                            Name = name,
                            Parent = nameSpace,
                            Project = word.Project
                        };
                        nameSpace.NamedElements.Add(name, generateBlock);
                    }
                }
                else
                {
                    if (nameSpace.NamedElements.ContainsKey(name))
                    {
                        INamedElement namedElement = nameSpace.NamedElements[name];
                        if(namedElement is GenerateBlock)
                        {
                            generateBlock = (GenerateBlock)namedElement;
                        }
                        else
                        {
                            word.AddError("duplicated block name");
                        }
                    }
                    else
                    {
                        generateBlock = new GenerateBlock(nameSpace.BuildingBlock, nameSpace)
                        {
                            BeginIndexReference = beginIdexRef,
                            DefinitionReference = beginRef,
                            Name = name,
                            Parent = nameSpace,
                            Project = word.Project
                        };
                        nameSpace.NamedElements.Add(name, generateBlock);
                    }
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            IndexReference startBlock = word.CreateIndexReference();

            if (word.Active)
            {
                while (!word.Eof)
                {
                    if(generateBlock != null)
                    {
                        if (!GenerateItem.Parse(word, generateBlock)) break;
                    }
                    else
                    {
                        if (!GenerateItem.Parse(word, nameSpace)) break;
                    }
                }
            }
            else
            {
                int beginCount = 0;
                while (!word.Eof && word.Text != "endgenerate")
                {
                    if (word.Text == "begin")
                    {
                        beginCount++;
                    }
                    else if (word.Text == "end")
                    {
                        if (beginCount == 0)
                        {
                            break;
                        }
                        beginCount--;
                    }
                    else
                    {
                        word.Color(CodeDrawStyle.ColorType.Inactivated);
                    }
                    word.MoveNext();
                }
            }

            word.AppendBlock(startBlock, word.CreateIndexReference());

            if (word.Text == "end")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if(word.Text == ":")
                {
                    word.MoveNext();
                    if (name == word.Text)
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("block identifier mismatch");
                    }
                }
                return true;
            }
            else
            {
                word.AddError("end required");
            }
            return true;
        }

    }
}
