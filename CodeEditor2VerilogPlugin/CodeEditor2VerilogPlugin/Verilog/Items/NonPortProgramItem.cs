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
            if (await ModuleOrGenerateItemDeclaration.Parse(word, nameSpace))
            {
                return true;
            }


            switch (word.Text)
            {
                //{ attribute_instance } continuous_assign 
                case "assign":
                    return await Items.ContinuousAssign.Parse(word, nameSpace);
                //{ attribute_instance } initial_construct 
                case "initial":
                    return await Items.InitialConstruct.Parse(word, nameSpace);
                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    var timeunits = DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return timeunits != null;
                default:
                    break;

            }
            if (await ModuleOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
