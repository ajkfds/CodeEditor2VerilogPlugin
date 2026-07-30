using pluginVerilog.Verilog.Items.Generate;
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
        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region 
                case "generate":
                    await GenerateRegion.ParseAsync(word, nameSpace);
                    return;
                case "specify":
                    SpecifyBlock.Parse(word, nameSpace);
                    return;
                // modport_declaration
                case "modport":
                    ModPort.Parse(word, nameSpace);
                    return;
                // timeunits_declaration3
                case "timeunit":
                case "timeprecision":
                    DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return;
                // interface_declaration 
                case "interface":
                    await BuildingBlocks.Interface.Create(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return;
                // program_declaration 
                default:
                    break;

            }
            // interface_or_generate_item 
            await InterfaceOrGenerateItem.ParseAsync(word, nameSpace);
            return;
        }
    }
}
