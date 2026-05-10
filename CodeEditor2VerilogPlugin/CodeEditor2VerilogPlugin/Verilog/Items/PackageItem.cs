namespace pluginVerilog.Verilog.Items
{
    /*
    package_item ::=
          package_or_generate_item_declaration 
        | anonymous_program 
        | package_export_declaration 
        | timeunits_declaration
    
    A timeunits_declaration shall be legal as a non_port_module_item, non_port_interface_item,
    non_port_program_item, or package_item only if it repeats and matches a previous timeunits_declaration within
    the same time scope.
     */


    public class PackageItem
    {
        /*
        package_item ::=
              package_or_generate_item_declaration 
            | anonymous_program 
            | package_export_declaration 
            | timeunits_declaration
        */
        public static async System.Threading.Tasks.Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                // anonymous_program
                case "program":
                    return await BuildingBlocks.Program.Parse(word, null, nameSpace.BuildingBlock, word.RootParsedDocument.File, word.Prototype) != null;
                // package_export_declaration
                case "export":
                    PackageExportDeclaration.ParseExportDeclaration(word, nameSpace);
                    return true;
                case "import":
                    PackageExportDeclaration.ParseImportDeclaration(word, nameSpace);
                    return true;
                // timeunits_declaration
                default:
                    return await PackageOrGenerateItemDeclaration.Parse(word, nameSpace);
            }
        }
    }
}
