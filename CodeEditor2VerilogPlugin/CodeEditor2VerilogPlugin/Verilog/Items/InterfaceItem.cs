using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InterfaceItem
    {
        /*
        interface_item  ::=   port_declaration ;
                            | non_port_interface_item 

       */
        public static async System.Threading.Tasks.Task Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "input":
                case "output":
                case "inout":
                    Verilog.DataObjects.Port.ParsePortDeclarations(word, nameSpace);
                    if (word.GetCharAt(0) != ';')
                    {
                        word.AddError("; expected");
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    break;
                default:
                    await NonPortInterfaceItem.ParseAsync(word, nameSpace);
                    return;
            }
            return;
        }

    }
}
