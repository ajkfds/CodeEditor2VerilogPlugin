using pluginVerilog.Verilog.Items.Generate;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ProgramGenerateItem
    {
        /*
            program_generate_item ::= 
                  loop_generate_construct 
                | conditional_generate_construct 
                | generate_region
         */
        public static async System.Threading.Tasks.Task<bool> ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region
                case "generate":
                    return await GenerateRegion.ParseAsync(word, nameSpace);
                case "for":
                    // loop_generate_construct
                    return await Generate.LoopGenerateConstruct.ParseAsync(word, nameSpace);
                case "if":
                    // conditional_generate_construct
                    return await Generate.IfGenerateConstruct.ParseAsync(word, nameSpace);
                default:
                    break;

            }
            return false;
        }
    }
}
