using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects;

namespace pluginVerilog.Verilog.Items
{
    public class ModuleCommonItem
    {
        /*
        ## SystemVerilog 2012

        module_common_item ::= 
              module_or_generate_item_declaration 
            | interface_instantiation 
            | program_instantiation 
            | assertion_item 
            | bind_directive 
            | continuous_assign 
            | net_alias 
            | initial_construct 
            | final_construct 
            | always_construct 
            | loop_generate_construct 
            | conditional_generate_construct
            | elaboration_system_task
        */

        public static async Task<bool> Parse(WordScanner word,NameSpace nameSpace)
        {



            // module_or_generate_item_declaration
            if (await ModuleOrGenerateItemDeclaration.Parse(word, nameSpace.BuildingBlock )) return true;
            //assertion_item::= concurrent_assertion_item | deferred_immediate_assertion_item

            switch (word.Text)
            {
                // assertion_item
//                case "assert":
                    //assertion_item ::=
                    //        [block_identifier: ] concurrent_assertion_statement
                    //      | checker_instantiation
                    //      | deferred_immediate_assertion_item
//                    return await ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
                // bind_directive
                // net_alias
                // final_construct
                // elaboration_system_task

                        // continuous_assign
                case "assign":
                    return await ModuleItems.ContinuousAssign.Parse(word, nameSpace);
                // initial_construct
                case "initial":
                    return await ModuleItems.InitialConstruct.Parse(word, nameSpace);
                // always_construct
                case "always":
                case "always_comb":
                case "always_latch":
                case "always_ff":
                    return await ModuleItems.AlwaysConstruct.Parse(word, nameSpace);
                // loop_generate_construct
                case "for":
                    //                    word.AddSystemVerilogError();
                    return await Generate.LoopGenerateConstruct.Parse(word, nameSpace);
                // conditional_generate_construct
                case "if":
                    return await Generate.IfGenerateConstruct.Parse(word, nameSpace);
            }


            // interface_instantiation
            if (InterfaceInstance.Parse(word, nameSpace)) return true;

            // program_instantiation

            //assertion_item ::=
            //        [block_identifier: ] concurrent_assertion_statement
            //      | checker_instantiation
            //      | deferred_immediate_assertion_item
            if (General.IsSimpleIdentifier(word.Text) && word.NextText == ":")
            {
                return await ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
            }

            return false;
        }

    }
}
