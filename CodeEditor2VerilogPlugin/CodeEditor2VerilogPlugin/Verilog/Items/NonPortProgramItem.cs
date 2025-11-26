using pluginVerilog.Verilog.Items.Generate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class NonPortProgramItem
    {
        /*
            non_port_program_item ::= 
                  { attribute_instance } continuous_assign 
                | { attribute_instance } module_or_generate_item_declaration 
                | { attribute_instance } initial_construct 
                | { attribute_instance } final_construct 
                | { attribute_instance } concurrent_assertion_item 
                | timeunits_declaration 
                | program_generate_item
            
         */
        
         


        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            // TODO
            //{ attribute_instance } final_construct 
            //{ attribute_instance } concurrent_assertion_item 
            //timeunits_declaration 

            //program_generate_item
            if (await ProgramGenerateItem.Parse(word, nameSpace))
            {
                return true;
            }
            //{ attribute_instance } module_or_generate_item_declaration 
            if (ModuleOrGenerateItemDeclaration.Parse(word, nameSpace.BuildingBlock))
            {
                return true;
            }


            switch (word.Text)
            {
                //{ attribute_instance } continuous_assign 
                case "assign":
                    return await ModuleItems.ContinuousAssign.Parse(word, nameSpace);
                //{ attribute_instance } initial_construct 
                case "initial":
                    return await ModuleItems.InitialConstruct.Parse(word, nameSpace);
                default:
                    break;

            }
            if (await ModuleOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
