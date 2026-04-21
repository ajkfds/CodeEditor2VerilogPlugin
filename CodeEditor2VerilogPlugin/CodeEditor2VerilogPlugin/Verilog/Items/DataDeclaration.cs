using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class DataDeclaration
    {
        /*
        data_declaration ::=      [ const ] [ var ] [ lifetime ] data_type_or_implicit list_of_variable_decl_assignments ;
                                | type_declaration
                                | package_import_declaration
                                | net_type_declaration

        package_import_declaration ::=
            "import" package_import_item { , package_import_item } ;
         */
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            // [ const ] [ var ] [ lifetime ] data_type_or_implicit list_of_variable_decl_assignments ;
            if (DataObjects.Variables.Variable.ParseDeclaration(word, nameSpace)) return true;

            // type_declaration
            if (word.Text == "typedef")
            {
                return DataObjects.Typedef.ParseDeclaration(word, nameSpace);
            }

            // package_import_declaration
            if (word.Text == "import")
            {
                PackageImportDeclaration.Parse(word, nameSpace);
                return true;
            }

            // net_type_declaration
            return false;
        }

    }
}
