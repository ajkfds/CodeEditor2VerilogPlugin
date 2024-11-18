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
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region 
                case "generate":
                    return GenerateRegion.Parse(word, nameSpace);
                case "specify":
                    // TODO
                    word.MoveNext();
                    return true;
                // modport_declaration
                case "modport":
                    return ModPort.Parse(word, nameSpace);
                // timeunits_declaration3
                // interface_declaration 
                case "interface":
                    BuildingBlocks.Interface.Create(word, nameSpace, null, word.RootParsedDocument.File, word.Prototype);
                    return true;
                // program_declaration 
                default:
                    break;

            }
            // interface_or_generate_item 
            if (InterfaceOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
