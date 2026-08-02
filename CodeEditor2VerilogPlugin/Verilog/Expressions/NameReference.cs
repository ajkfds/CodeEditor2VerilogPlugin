using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class NameReference
    {
        public NameReference(CodeEditor2.Data.Project project)
        {
            this.project = project;
        }
        CodeEditor2.Data.Project project { get; init; }


        private List<string> Names = new List<string>();
        private List<string> Separators = new List<string>();
        private List<List<Verilog.Expressions.RangeExpression>?> Ranges = new List<List<Expressions.RangeExpression>?>();
        private List<WordReference> WordReferences = new List<WordReference>();

        private NameSpace? searchNameSpaceUpward(string name,NameSpace nameSpace)
        {
            if (nameSpace.NamedElements.ContainsKey(name))
            {
                return nameSpace;
            }
            else
            {
                NameSpace? parentNameSpace = nameSpace.Parent;
                if (parentNameSpace == null) return null;
                return searchNameSpaceUpward(name, parentNameSpace);
            }
        }

        private NameSpace? searchNameSpaceDownward(string name, NameSpace nameSpace)
        {
            if (nameSpace.NamedElements.ContainsKey(name))
            {
                return nameSpace;
            }
            else
            {
                foreach (var element in nameSpace.NamedElements) 
                {
                    NameSpace? subNameSpace = element as NameSpace;
                    if (subNameSpace == null) continue;

                    NameSpace? foundNameSpace = searchNameSpaceDownward(name, subNameSpace);
                    if (foundNameSpace != null) return foundNameSpace;
                }
            }
            return null;
        }

        public string GetNameSpaceText()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < Names.Count - 1; i++)
            {
                sb.Append(Names[i]);
                List<Expressions.RangeExpression>? ranges = Ranges[i];
                if (ranges != null)
                {
                    foreach(var range in ranges)
                    {
                        sb.Append(range.CreateString());
                    }
                }
                sb.Append(Separators[i]);
            }
            return sb.ToString();
        }
        public (INamedElement?, INamedElement?) GetElement(NameSpace nameSpace)
        {
            if (Names.Count <= 0) return (null, null);

            int index = 0;
            string name = Names[index];

            NameSpace? baseNameSpace = null;
            if (name == "this")
            {
                if(nameSpace.BuildingBlock is Class && Separators[0] == ".")
                {
                    baseNameSpace = nameSpace.BuildingBlock;
                    index++;
                    if (index >= Names.Count) return (baseNameSpace,baseNameSpace);
                }
                else
                {
                    WordReferences[0].AddError("Invalid use of 'this' keyword");
                    return (null, null);
                }
            }

            name = Names[index];
            {
                baseNameSpace = searchNameSpaceUpward(name, nameSpace);

                if (baseNameSpace != null)
                { // search downward
                    baseNameSpace = searchNameSpaceDownward(name, baseNameSpace);
                }
            }

            if (baseNameSpace == null)
            { // unfound element
                BuildingBlock? buildingBlock = nameSpace.ProjectProperty.GetBuildingBlock(name);
                if(buildingBlock != null)
                {
                    baseNameSpace = buildingBlock;
                }
                index++;
                if (index >= Names.Count) return (baseNameSpace,baseNameSpace);
                if(baseNameSpace == null) return (null, null);
            }
            name = Names[index];
            return searchElement(baseNameSpace, index);
        }

        private (INamedElement?, INamedElement?) searchElement(INamedElement nameSpace,int index)
        {
            string name = Names[index];

            if (nameSpace.NamedElements.ContainsKey(name))
            {
                INamedElement namedElement = nameSpace.NamedElements[name];
                index++;
                if (index >= Names.Count) return (namedElement, nameSpace);
                NameSpace? subNameSpace = namedElement as NameSpace;
                if (subNameSpace != null) return searchElement(subNameSpace, index);

                Items.IBuildingBlockInstantiation? instantiation = namedElement as Items.IBuildingBlockInstantiation;
                BuildingBlock? instancedBuildingBlock = instantiation?.GetInstancedBuildingBlock();
                if (instancedBuildingBlock != null) return searchElement(instancedBuildingBlock, index);

                DataObjects.Variables.VirtualInterface? virtualInterface = namedElement as DataObjects.Variables.VirtualInterface;
                if(virtualInterface != null)
                {
                    Interface? @interface = virtualInterface.GetSourceInterface();
                    if (@interface == null) return (null, null);
                    return searchElement(@interface, index);
                }

                DataObjects.Variables.Variable? variable = namedElement as DataObjects.Variables.Variable;
                if(variable != null) return searchElement(variable, index);

                return (null,null);
            }
            WordReferences[index].AddError("unfound object");
            return (null, null);
        }


        public static NameReference? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptRange)
        {
            if (word.Eof) return null;
            if (!General.IsSimpleIdentifier(word.Text)) return null;

            WordScanner wordClone = word.Clone();

            NameReference nameReference = new NameReference(wordClone.Project);
            while (!wordClone.Eof)
            {
                if (!General.IsSimpleIdentifier(wordClone.Text))
                {
                    break;
                }

                nameReference.Names.Add(wordClone.Text);
                wordClone.Color(CodeDrawStyle.ColorType.Identifier);
                nameReference.WordReferences.Add(wordClone.GetReference());
                wordClone.MoveNext();

                if (wordClone.Text == "[")
                {
                    if (!acceptRange) break;
                    List<Expressions.RangeExpression>? ranges = new List<Expressions.RangeExpression>();
                    while (!wordClone.Eof && wordClone.Text == "[")
                    {
                        Verilog.Expressions.RangeExpression? range;
                        range = Verilog.Expressions.RangeExpression.ParseCreate(wordClone, nameSpace);
                        if (range == null) break;
                        ranges.Add(range);
                    }
                    nameReference.Ranges.Add(ranges);
                }
                else
                {
                    nameReference.Ranges.Add(null);
                }

                if (wordClone.Eof) break;
                if (wordClone.Text == "." || wordClone.Text == "::")
                {
                    nameReference.Separators.Add(wordClone.Text);
                    wordClone.MoveNext();
                    continue;
                }
                nameReference.Separators.Add("");
                break;
            }

            while (!word.Eof && !nameReference.WordReferences.Last().IndexReference.IsSameAs(word.CreateIndexReference()) )
            {
                word.MoveNext();
            }

            return nameReference;
        }


        public void AppendString(StringBuilder sb)
        {
            for (int i = 0; i < Names.Count; i++)
            {
                sb.Append(Names[i]);

                List<Expressions.RangeExpression>? ranges = Ranges[i];

                if (ranges != null)
                {
                    foreach (var range in ranges)
                    {
                        sb.Append(range.CreateString());
                    }
                }

                sb.Append(Separators[i]);
            }
        }





    }

}
