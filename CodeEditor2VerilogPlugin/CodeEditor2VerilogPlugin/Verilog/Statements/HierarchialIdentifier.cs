using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.ModuleItems;
using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog.Statements
{
    public class HierarchialIdentifier
    {
        /*
                    hierarchical_identifier ::= [ $root . ] { identifier constant_bit_select . } identifier

         */
        protected HierarchialIdentifier() { }

        List<string> Identifiers = new List<string>();
        private NameSpace BaseNameSpace { init; get; } 

        public NameSpace? GetNameSpace(NameSpace baseNameSpace)
        {
            return null;
        }

        public HierarchialIdentifier? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            string name = word.Text;
            if (word.Text == "$root")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text != ".")
                {
                    word.AddError("'.' required after $root");
                    return null;
                }
                else
                {
                    word.MoveNext(); // .
                    HierarchialIdentifier identifier = new HierarchialIdentifier() { BaseNameSpace = nameSpace };
                    identifier.Identifiers.Add(name);
                    parseHierarchy(word, nameSpace, identifier);
                }
            }

            if (word.NextText != ".")
            {
                return null;
            }

            {
                HierarchialIdentifier identifier = new HierarchialIdentifier() { BaseNameSpace = nameSpace };
                return parseHierarchy(word, nameSpace, identifier);
            }
        }

        private HierarchialIdentifier? parseHierarchy(WordScanner word, NameSpace nameSpace,HierarchialIdentifier hierarchialIdentifier)
        {
            if (word.Eof) return hierarchialIdentifier;
            if (word.NextText != ".") return hierarchialIdentifier;

            INamedElement element = nameSpace.NamedElements[word.Text];
            if (element is　Verilog.DataObjects.DataObject)
            {
                return hierarchialIdentifier;
            }

            // Since Task and Function are also namespaces, they need to be processed before namespaces.
            // task reference : for left side only
            if (element is Task)
            {
                return hierarchialIdentifier;
            }
            // function call : for right side only
            if (element is Function)
            {
                return hierarchialIdentifier;
            }

            if (element is NameSpace)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                hierarchialIdentifier.Identifiers.Add(word.Text);
                word.MoveNext();
                word.MoveNext();// .
                parseHierarchy(word, (NameSpace)element, hierarchialIdentifier);
            }

            if (element is DataObjects.Constants.Constants)
            {
                return hierarchialIdentifier;
            }

            if (element is IBuildingBlockInstantiation)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                hierarchialIdentifier.Identifiers.Add(word.Text);
                word.MoveNext();
                IBuildingBlockInstantiation buildingBlockInstantiation = (IBuildingBlockInstantiation)element;
                BuildingBlock? buildingBlock = buildingBlockInstantiation.GetInstancedBuildingBlock();
                if (buildingBlock == null) return null;

                word.MoveNext();
                parseHierarchy(word, buildingBlock, hierarchialIdentifier);
            }
            return parseUndefinedHierarchy(word, nameSpace, hierarchialIdentifier);
        }

        private HierarchialIdentifier? parseUndefinedHierarchy(WordScanner word, NameSpace nameSpace, HierarchialIdentifier hierarchialIdentifier)
        {
            if (word.Eof) return hierarchialIdentifier;
            if (word.NextText != ".") return hierarchialIdentifier;

            string identifier = word.Text;
            hierarchialIdentifier.Identifiers.Add(identifier);
            word.MoveNext();

            return parseUndefinedHierarchy(word, nameSpace, hierarchialIdentifier);
        }

    }

}

