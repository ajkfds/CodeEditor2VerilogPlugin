using pluginVerilog.Verilog.Items.Generate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class NonPortModuleItem
    {
        /*
        non_port_module_item ::=
              generate_region 
            | module_or_generate_item 
            | specify_block 
            | { attribute_instance } specparam_declaration 
            | program_declaration 
            | module_declaration 
            | interface_declaration 
            | timeunits_declaration
       */
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region
                case "generate":
                    return GenerateRegion.Parse(word, nameSpace);
                // specify_block
                case "specify":
                    return SpecifyBlock.Parse(word, nameSpace);
                // { attribute_instance }specparam_declaration
                // program_declaration
                case "program":
                    BuildingBlocks.Program.Parse(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;
                // module_declaration
                case "module":
                case "macromodule":
                    BuildingBlocks.Module.ParseCreate(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File,word.Prototype);
                    return true;
                // interface_declaration
                case "interface":
                    BuildingBlocks.Interface.Create(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;


                // timeunits_declaration
                // module_or_generate_item
                default:
                    break;

            }
            if (ModuleOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
