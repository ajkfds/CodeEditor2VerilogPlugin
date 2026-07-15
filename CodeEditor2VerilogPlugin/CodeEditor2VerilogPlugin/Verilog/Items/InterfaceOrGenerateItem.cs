using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InterfaceOrGenerateItem
    {
        /*
        interface_or_generate_item  ::=   { attribute_instance } module_common_item 
                                        | { attribute_instance } extern_tf_declaration      
         */
        public static async Task<bool> ParseAsync(WordScanner word, NameSpace nameSpace)
        {

            // module_common_item
            if (await ModuleCommonItem.ParseAsync(word, nameSpace)) return true;


            return false;
        }
    }
}
