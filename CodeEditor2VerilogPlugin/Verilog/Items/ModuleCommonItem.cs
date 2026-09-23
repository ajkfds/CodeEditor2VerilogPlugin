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

        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {


            IndexReference iref = word.CreateIndexReference();
            // module_common_item
            ModuleOrGenerateItemDeclaration.Parse(word, nameSpace);
            if (!word.CreateIndexReference().IsSameAs(iref)) return;

            // module_or_generate_item_declaration
            //assertion_item::= concurrent_assertion_item | deferred_immediate_assertion_item

            switch (word.Text)
            {
                // assertion_item
                case "assert":
                    //assertion_item::=
                    //        [block_identifier: ] concurrent_assertion_statement
                    //      | checker_instantiation
                    //      | deferred_immediate_assertion_item
                    ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
                    return;
                // bind_directive
                case "bind":
                    Items.BindDirective? bindDirective;
                    Items.BindDirective.Parse(word, nameSpace, out bindDirective);
                    return;
                // net_alias
                case "alias":
                    Items.NetAlias.Parse(word, nameSpace);
                    return;
                // final_construct
                case "final":
                    Items.FinalConstruct.Parse(word, nameSpace);
                    return;
                // elaboration_system_task

                // continuous_assign
                case "assign":
                    Items.ContinuousAssign.Parse(word, nameSpace);
                    return;
                // initial_construct
                case "initial":
                    Items.InitialConstruct.Parse(word, nameSpace);
                    return;
                // always_construct
                case "always":
                case "always_comb":
                case "always_latch":
                case "always_ff":
                    Items.AlwaysConstruct.ParseCreate(word, nameSpace);
                    return;
                // loop_generate_construct
                case "for":
                    //                    word.AddSystemVerilogError();
                    await Generate.LoopGenerateConstruct.ParseAsync(word, nameSpace);
                    return;
                // conditional_generate_construct
                case "if":
                    await Generate.IfGenerateConstruct.ParseAsync(word, nameSpace);
                    return;
                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    var timeunits = DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return;
            }


            // interface_instantiation
            InterfaceInstance.Parse(word, nameSpace);
            if (!word.CreateIndexReference().IsSameAs(iref)) return;

            // program_instantiation
            await Items.ProgramInstantiation.ParseAsync(word, nameSpace);
            if (!word.CreateIndexReference().IsSameAs(iref)) return;

            //assertion_item ::=
            //        [block_identifier: ] concurrent_assertion_statement
            //      | checker_instantiation
            //      | deferred_immediate_assertion_item
            if (General.IsSimpleIdentifier(word.Text) && word.NextText == ":")
            {
                ConcurrentAssertionItemExceptCheckerInstantiation.Parse(word, nameSpace);
            }
        }

    }
}
