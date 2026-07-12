using pluginVerilog.Verilog.BuildingBlocks;

namespace pluginVerilog.Verilog.Items
{
    public class ModuleOrGenerateItemDeclaration
    {
        /*
        ## SystemVerilog 2012

        module_or_generate_item_declaration ::= 
              package_or_generate_item_declaration 
            | genvar_declaration 
            | clocking_declaration 
            | default clocking clocking_identifier ;
        */

        public static async System.Threading.Tasks.Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            if(nameSpace is not BuildingBlock && nameSpace is not Generate.GenerateBlock)
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
            // package_or_generate_item_declaration
            if (await PackageOrGenerateItemDeclaration.Parse(word, nameSpace)) return true;

            switch (word.Text)
            {
                // genvar_declaration
                case "genvar":
                    DataObjects.Variables.Genvar.ParseCreateFromDeclaration(word, nameSpace);
                    return true;

                // clocking_declaration ::= [ default ] clocking [ clocking_identifier ] clocking_event ; { clocking_item } endclocking  [ : clocking_identifier]
                //                        | global clocking [ clocking_identifier ] clocking_event ; endclocking [ : clocking_identifier]
                case "clocking":
                case "global":
                    Clocking? clocking = Clocking.ParseCreate(word, nameSpace, null);
                    if (clocking != null)
                    {
                        // Register the clocking block in the namespace if it has a name
                        if (!string.IsNullOrEmpty(clocking.Name) && !word.Prototype)
                        {
                            if (!nameSpace.NamedElements.ContainsKey(clocking.Name))
                            {
                                nameSpace.NamedElements.Add(clocking.Name, clocking);
                            }
                        }
                        return true;
                    }
                    return false;

                // "default clocking" clocking_identifier;
                case "default":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if (word.Text == "clocking")
                    {
                        // Parse default clocking statement
                        Clocking.ParseDefaultClocking(word, nameSpace);
                    }
                    else
                    {
                        word.AddError("clocking expected");
                        word.SkipToKeyword(";");
                        if (word.Text == ";") word.MoveNext();
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
