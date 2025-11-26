using pluginVerilog.Verilog.Items.Generate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region
                case "generate":
                    return await GenerateRegion.Parse(word, nameSpace);
                case "for":
                    // loop_generate_construct
                    return Generate.LoopGenerateConstruct.Parse(word, nameSpace);
                case "if":
                    // conditional_generate_construct
                    return await Generate.IfGenerateConstruct.Parse(word, nameSpace);
                default:
                    break;

            }
            return false;
        }
    }
}
