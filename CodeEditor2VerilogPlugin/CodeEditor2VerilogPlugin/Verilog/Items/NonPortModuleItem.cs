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
        public static async System.Threading.Tasks.Task ParseAsync(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // generate_region
                case "generate":
                    await GenerateRegion.ParseAsync(word, nameSpace);
                    return;
                // specify_block
                case "specify":
                    SpecifyBlock.Parse(word, nameSpace);
                    return;
                // { attribute_instance }specparam_declaration
                case "specparam":
                    DataObjects.Constants.Constants.ParseCreateDeclaration(word, nameSpace,null);
                    return;
                // program_declaration
                case "program":
                    await BuildingBlocks.Program.ParseAsync(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return;
                // module_declaration
                case "module":
                case "macromodule":
                    await BuildingBlocks.Module.ParseCreateAsync(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return;
                // interface_declaration
                case "interface":
                    if (word.NextText == "class") break;
                    await BuildingBlocks.Interface.Create(word, nameSpace, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype);
                    return;

                // clocking_declaration
                case "clocking":
                    BuildingBlocks.Clocking.ParseCreate(word, nameSpace, null);
                    return;

                // default clocking clocking_identifier ;
                // or default disable iff expression_or_dist ;
                case "default":
                    if (word.NextText == "clocking")
                    {
                        BuildingBlocks.Clocking.ParseDefaultClocking(word, nameSpace);
                        return;
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
                        return;
                    }
                    break;

                // timeunits_declaration
                case "timeunit":
                case "timeprecision":
                    DataObjects.TimeunitsDeclaration.ParseCreate(word, nameSpace);
                    return;
                // let_declaration
                case "let":
                    var letDecl = DataObjects.LetDeclaration.ParseCreate(word, nameSpace);
                    if (letDecl != null && !string.IsNullOrEmpty(letDecl.Name))
                    {
                        nameSpace.NamedElements.Add(letDecl.Name, letDecl);
                    }
                    return;
                // module_or_generate_item
                default:
                    break;

            }
            await ModuleOrGenerateItem.ParseAsync(word, nameSpace);
            return;
        }
    }
}
