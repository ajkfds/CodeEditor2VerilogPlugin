using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class InterfaceOrGenerateItem
    {
        /*
        interface_or_generate_item  ::=   { attribute_instance } module_common_item 
                                        | { attribute_instance } extern_tf_declaration      
         */
        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            IndexReference iref = word.CreateIndexReference();

            // module_common_item
            await ModuleCommonItem.ParseAsync(word, nameSpace);
            if (!word.CreateIndexReference().IsSameAs(iref)) return;

            // interface_or_generate_item may also contain a module / program /
            // udp / gate instantiation (an interface can instantiate modules,
            // e.g. "interface bus_if; leaf u_leaf(); endinterface"). Without
            // these fallbacks the identifier is left unconsumed and the item
            // loop reports "illegal interface item", so the interface body's
            // instances never reach the hierarchy.
            switch (word.Text)
            {
                // gate_instantiation
                case "cmos": case "rcmos": case "bufif0": case "bufif1":
                case "notif0": case "notif1": case "nmos": case "pmos":
                case "rnmos": case "rpmos": case "and": case "nand":
                case "or": case "nor": case "xor": case "xnor":
                case "buf": case "not": case "tranif0": case "tranif1":
                case "rtranif0": case "rtranif1": case "tran": case "rtran":
                case "pullup": case "pulldown":
                    Items.GateInstantiation.Parse(word, nameSpace);
                    return;
            }

            if (!General.IsSimpleIdentifier(word.Text)) return;
            if (General.ListOfKeywords.Contains(word.Text)) return;

            // udp_instantiation
            if (
                word.ProjectProperty.DefinitionNameSpace.Get(word.Text) is BuildingBlocks.Primitive ||
                word.ProjectProperty.ExtenralPrimitiveLibraryPath.ContainsKey(word.Text)
                )
            {
                await Items.UdpInstantiation.ParseAsync(word, nameSpace);
                if (!word.CreateIndexReference().IsSameAs(iref)) return;
                return;
            }

            // module_instantiation
            await Items.ModuleInstantiation.ParseAsync(word, nameSpace);
            if (!word.CreateIndexReference().IsSameAs(iref)) return;
        }
    }
}
