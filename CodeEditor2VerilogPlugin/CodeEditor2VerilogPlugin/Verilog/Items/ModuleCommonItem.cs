using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ModuleCommonItem
    {
        /*
        ## SystemVerilog 2012

        module_common_item ::= 
              module_or_generate_item_declaration v
            | interface_instantiation v
            | program_instantiation v
            | assertion_item v
            | bind_directive 
            | continuous_assign v
            | net_alias 
            | initial_construct 
            | final_construct 
            | always_construct v
            | loop_generate_construct v
            | conditional_generate_constructv
            | elaboration_system_task
        */

        public static async System.Threading.Tasks.Task<bool> ParseAsync(WordScanner word, NameSpace nameSpace)
        {



            // module_or_generate_item_declaration
            if (ModuleOrGenerateItemDeclaration.Parse(word, nameSpace)) return true;
            //assertion_item::= concurrent_assertion_item | deferred_immediate_assertion_item

            switch (word.Text)
            {
                // assertion_item
                case "assert":
                    //assertion_item::=
                    //        [block_identifier: ] concurrent_assertion_statement
                    //      | checker_instantiation
                    //      | deferred_immediate_assertion_item
                    return ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
                // bind_directive
                case "bind":
                    Items.BindDirective? bindDirective;
                    return Items.BindDirective.Parse(word, nameSpace, out bindDirective);
                // net_alias
                case "alias":
                    return Items.NetAlias.Parse(word, nameSpace);
                // final_construct
                case "final":
                    return Items.FinalConstruct.Parse(word, nameSpace);
                // elaboration_system_task

                // continuous_assign
                case "assign":
                    return Items.ContinuousAssign.Parse(word, nameSpace);
                // initial_construct
                case "initial":
                    return Items.InitialConstruct.Parse(word, nameSpace);
                // always_construct
                case "always":
                case "always_comb":
                case "always_latch":
                case "always_ff":
                    return Items.AlwaysConstruct.Parse(word, nameSpace);
                // loop_generate_construct
                case "for":
                    //                    word.AddSystemVerilogError();
                    await Generate.LoopGenerateConstruct.ParseAsync(word, nameSpace);
                    return true;
                // conditional_generate_construct
                case "if":
                    return await Generate.IfGenerateConstruct.ParseAsync(word, nameSpace);
                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    var timeunits = DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return timeunits != null;
            }


            // interface_instantiation
            if (InterfaceInstance.Parse(word, nameSpace)) return true;

            // program_instantiation
            if (await Items.ProgramInstantiation.ParseAsync(word, nameSpace)) return true;

            //assertion_item ::=
            //        [block_identifier: ] concurrent_assertion_statement
            //      | checker_instantiation
            //      | deferred_immediate_assertion_item
            if (General.IsSimpleIdentifier(word.Text) && word.NextText == ":")
            {
                return ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
            }

            return false;
        }

    }
}
