using pluginVerilog.Verilog.Items.Generate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class NonPortInterfaceItem
    {
        /*
        non_port_interface_item ::=  generate_region 
                                    | interface_or_generate_item 
                                    | program_declaration 
                                    | modport_declaration
                                    | interface_declaration 
                                    | timeunits_declaration3
       */
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region 
                case "generate":
                    return await GenerateRegion.Parse(word, nameSpace);
                case "specify":
                    return SpecifyBlock.Parse(word, nameSpace);
                // modport_declaration
                case "modport":
                    return ModPort.Parse(word, nameSpace);
                // timeunits_declaration3
                // interface_declaration 
                case "interface":
                    BuildingBlocks.Interface.Create(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;
                // program_declaration 
                default:
                    break;

            }
            // interface_or_generate_item 
            if (await InterfaceOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
