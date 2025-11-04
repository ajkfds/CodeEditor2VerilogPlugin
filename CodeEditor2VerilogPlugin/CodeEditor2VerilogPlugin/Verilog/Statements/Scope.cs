using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    /* SystemVerilog IEEE1800-2017
        package_scope ::=     package_identifier "::"
                            | "$unit" "::"
        class_scope ::= class_type "::"
     */
    public class Scope
    {
        public string Name { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonIgnore]
        private ProjectProperty projectProperty { init; get; }
        public Scope? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.NextText != "::") return null;
            if (General.ListOfKeywords.Contains(word.Text)) return null;

            string name = word.Text;
            if (word.Text == "$unit")
            {
                name = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext(); // skip "::"
                return new PackageScope() { Name = name,projectProperty = word.ProjectProperty };
            }

            BuildingBlocks.BuildingBlock? buildingBlock = word.ProjectProperty.GetBuildingBlock(name);
            
            
            if (buildingBlock == null)
            {
                word.AddError($"undefined scope {name}");
                return new PackageScope() { Name = name, projectProperty = word.ProjectProperty };
            }
            return null;
        }
        public BuildingBlocks.BuildingBlock? BuildingBlock
        {
            get { 
                return projectProperty.GetBuildingBlock(Name); 
            }
        }
    }

    public class  PackageScope : Scope
    {
        
    }

    public class  ClassScope : Scope
    {
        
    }
}
