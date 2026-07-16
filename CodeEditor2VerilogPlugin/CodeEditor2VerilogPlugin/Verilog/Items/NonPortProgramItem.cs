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




        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            // TODO
            //{ attribute_instance } final_construct 
            //{ attribute_instance } concurrent_assertion_item 
            //timeunits_declaration 

            //program_generate_item
            if (await ProgramGenerateItem.ParseAsync(word, nameSpace))
            {
                return;
            }
            //{ attribute_instance } module_or_generate_item_declaration 
            if (ModuleOrGenerateItemDeclaration.Parse(word, nameSpace))
            {
                return;
            }


            switch (word.Text)
            {
                //{ attribute_instance } continuous_assign 
                case "assign":
                    Items.ContinuousAssign.Parse(word, nameSpace);
                    return;
                //{ attribute_instance } initial_construct 
                case "initial":
                    Items.InitialConstruct.Parse(word, nameSpace);
                    return;
                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return;
                default:
                    break;

            }
            await ModuleOrGenerateItem.ParseAsync(word, nameSpace);
            return;
        }
    }
}
