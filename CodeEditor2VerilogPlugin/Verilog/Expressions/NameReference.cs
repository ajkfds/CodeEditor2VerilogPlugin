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

        public bool CanBeImplecitNet
        {
            get
            {
                if (Names.Count != 1) return false;
                if (Ranges[0] != null) return false;
                return true;
            }
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
                    // "this" resolved to the enclosing class; resolve the
                    // following member (this.srandom, this.x) against the class
                    // directly. Falling through to the general lookup below
                    // would overwrite baseNameSpace and miss class members.
                    name = Names[index];
                    return searchElement(baseNameSpace, index);
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
                BuildingBlock? buildingBlock = nameSpace.ProjectProperty.DefinitionNameSpace.Get(name);
                if(buildingBlock != null)
                {
                    baseNameSpace = buildingBlock;
                }
                else
                {
                    Class? @class = nameSpace.ProjectProperty.UnitNameSpace.Get(name) as Class;
                    if (@class != null) baseNameSpace = @class;
                }
                if (baseNameSpace == null)
                {
                    // same-file building block (module / interface / program / checker):
                    // a hierarchical reference such as "top.e" may name a module that is
                    // declared later in the same file. Those modules are registered in the
                    // current file's Root.BuildingBlocks and resolved here when the
                    // DefinitionNameSpace (cross-file) lookup misses (e.g. forward
                    // reference / single-file parse where the module is not yet registered).
                    var localRoot = nameSpace.BuildingBlock?.File?.VerilogParsedDocument?.Root;
                    if (localRoot != null && localRoot.BuildingBlocks.TryGetValue(name, out BuildingBlock? localBlock))
                    {
                        baseNameSpace = localBlock;
                    }
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

                // array objects (queue / dynamic array / associative array) expose
                // built-in methods (size, push_back, delete, ...) through NamedElements
                DataObjects.DataObject? dataObjectElement = namedElement as DataObjects.DataObject;
                if (dataObjectElement != null) return searchElement(dataObjectElement, index);

                DataObjects.Nets.Net? net = namedElement as DataObjects.Nets.Net;
                if(net != null)
                {
                    return searchElement(net, index);
                }

                return (null,null);
            }

            // Array element member access (e.g. "process job[]; job[1].await();").
            // A queue / dynamic array / associative array exposes its own
            // manipulation methods (size, push_back, delete, ...) but not the
            // members of its element data type. Fall back to the element
            // DataObject's NamedElements so that "job[1].await()" / "job[i].status"
            // resolve against the element type (IEEE 1800-2017 7.8).
            DataObjects.DataObject? arrayObject = nameSpace as DataObjects.DataObject;
            if (arrayObject != null)
            {
                DataObjects.DataObject? elementDataObject = GetArrayElementDataObject(arrayObject);
                if (elementDataObject != null && elementDataObject.NamedElements.ContainsKey(name))
                {
                    return searchElement(elementDataObject, index);
                }
            }

            WordReferences[index].AddError("unfound object");
            return (null, null);
        }

        /// <summary>
        /// Returns the element DataObject wrapped by a queue / dynamic array /
        /// associative array, or null when the DataObject is not such an array.
        /// </summary>
        private static DataObjects.DataObject? GetArrayElementDataObject(DataObjects.DataObject dataObject)
        {
            if (dataObject is DataObjects.Arrays.DynamicArray dynamicArray) return dynamicArray.DataObject;
            if (dataObject is DataObjects.Arrays.Queue queue) return queue.DataObject;
            if (dataObject is DataObjects.Arrays.AssociativeArray associativeArray) return associativeArray.DataObject;
            return null;
        }


        public static NameReference? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptRange)
        {
            if (word.Eof) return null;
            if (!General.IsSimpleIdentifier(word.Text)) return null;

            WordScanner wordClone = word.Clone(true);

            NameReference nameReference = new NameReference(wordClone.Project);
            while (!wordClone.Eof)
            {
                if (!General.IsSimpleIdentifier(wordClone.Text))
                {
                    // After a "." separator the member / method name may itself be a
                    // keyword (e.g. the unpacked array reduction methods "and"/"or"/
                    // "xor": b.or / b.and / b.xor). Accept such a keyword here so the
                    // built-in method resolves via NamedElements (IEEE 1800-2017 7.12.3).
                    if (!(nameReference.Separators.Count > 0 &&
                          nameReference.Separators[nameReference.Separators.Count - 1] == "." &&
                          General.IsIdentifier(wordClone.Text)))
                    {
                        break;
                    }
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

            // member name autocomplete: "obj." followed by EOF (e.g. "obj." at caret)
            // resolve the target element and append its members as autocomplete candidates
            if (word.CompletionContext != null && wordClone.Eof &&
                nameReference.Separators.Count > 0 &&
                nameReference.Separators[nameReference.Separators.Count - 1] == ".")
            {
                (INamedElement? memberElement, INamedElement? memberTarget) = nameReference.GetElement(nameSpace);
                // prefer the resolved element itself (module instance / object / etc.)
                // memberTarget is the name space containing the element; using it would
                // show sibling elements instead of the instance's own members
                INamedElement? memberRoot = memberElement ?? memberTarget;
                if (memberRoot != null) word.CompletionContext.AppendSubElements(memberRoot);
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
