using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ProgramItem
    {
        /*
        program_item ::= 
                port_declaration ; 
            | non_port_program_item

       */
        public static async System.Threading.Tasks.Task<bool> ParseAsync(WordScanner word, NameSpace nameSpace)
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
                    return await NonPortProgramItem.ParseAsync(word, nameSpace);
            }
            return true;
        }

    }
}
