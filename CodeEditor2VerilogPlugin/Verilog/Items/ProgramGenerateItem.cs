using pluginVerilog.Verilog.Items.Generate;

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
                    await GenerateRegion.ParseAsync(word, nameSpace);
                    return true;
                case "for":
                    // loop_generate_construct
                    await Generate.LoopGenerateConstruct.ParseAsync(word, nameSpace);
                    return true;
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
