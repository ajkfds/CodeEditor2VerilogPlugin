using pluginVerilog.Verilog.Items.Generate;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class NonPortModuleItem
    {
        /*
        non_port_module_item ::=
              generate_region 
            | module_or_generate_item 
            | specify_block 
            | { attribute_instance } specparam_declaration 
            | program_declaration 
            | module_declaration 
            | interface_declaration 
            | timeunits_declaration
            | clocking_declaration
       */
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region
                case "generate":
                    return await GenerateRegion.Parse(word, nameSpace);
                // specify_block
                case "specify":
                    return SpecifyBlock.Parse(word, nameSpace);
                // { attribute_instance }specparam_declaration
                case "specparam":
                    DataObjects.Constants.Constants.ParseCreateDeclaration(word, nameSpace,null);
                    return true;
                // program_declaration
                case "program":
                    await BuildingBlocks.Program.Parse(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;
                // module_declaration
                case "module":
                case "macromodule":
                    await BuildingBlocks.Module.ParseCreate(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;
                // interface_declaration
                case "interface":
                    if (word.NextText == "class") break;
                    await BuildingBlocks.Interface.Create(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return true;

                // clocking_declaration
                case "clocking":
                    BuildingBlocks.Clocking.ParseCreate(word, nameSpace, null);
                    return true;

                // default clocking clocking_identifier ;
                // or default disable iff expression_or_dist ;
                case "default":
                    if (word.NextText == "clocking")
                    {
                        BuildingBlocks.Clocking.ParseDefaultClocking(word, nameSpace);
                        return true;
                    }
                    if (word.NextText == "disable")
                    {
                        // default disable iff expression_or_dist ;
                        // For now, just skip past it
                        word.MoveNext(); // default
                        word.MoveNext(); // disable
                        if (word.Text == "iff")
                        {
                            word.MoveNext(); // iff
                            // Skip the expression
                            Expressions.Expression.ParseCreate(word, nameSpace);
                        }
                        // Skip to semicolon
                        word.SkipToKeyword(";");
                        if (word.Text == ";") word.MoveNext();
                        return true;
                    }
                    break;

                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    var timeunits = DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return timeunits != null;
                // let_declaration
                case "let":
                    var letDecl = DataObjects.LetDeclaration.ParseCreate(word, nameSpace);
                    if (letDecl != null)
                    {
                        nameSpace.NamedElements.Add(letDecl.Name, letDecl);
                    }
                    return true;
                // module_or_generate_item
                default:
                    break;

            }
            if (await ModuleOrGenerateItem.Parse(word, nameSpace)) return true;
            return false;
        }
    }
}
