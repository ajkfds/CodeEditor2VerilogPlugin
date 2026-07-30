namespace pluginVerilog.Verilog.Items
{
    public class PackageExportDeclaration
    {
        /*
        package_export_declaration ::=
              export *::* ;
            | export package_import_item { , package_import_item } ;
        package_import_item ::=
              package_identifier :: identifier
            | package_identifier :: *

        package_import_declaration ::=
              import package_identifier :: item_identifier { , item_identifier }
            | import package_identifier :: *
         */

        public static void ParseExportDeclaration(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "export") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == "*")
            {
                // export *::* ;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text != "::") System.Diagnostics.Debugger.Break();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text != "*") System.Diagnostics.Debugger.Break();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            else
            {
                // export package_import_item { , package_import_item } ;
                ParsePackageImportItems(word, nameSpace);
            }
        }

        public static void ParseImportDeclaration(WordScanner word, NameSpace nameSpace)
        {
            /*
            package_import_declaration ::=
                  import package_identifier :: item_identifier { , item_identifier }
                | import package_identifier :: *
             */
            if (word.Text != "import") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse single item or wildcard import
            while (true)
            {
                // Parse package_identifier
                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("package identifier expected");
                    word.SkipToKeyword(";");
                    break;
                }
                string packageName = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text != "::")
                {
                    word.AddError(":: expected");
                    word.SkipToKeyword(";");
                    break;
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text == "*")
                {
                    // import package_identifier :: * ;
                    // Wildcard import - import all items from the package
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    // Add to imported packages list
                    word.RootParsedDocument.ImportedPackages.Add(packageName);
                }
                else
                {
                    // import package_identifier :: item_identifier
                    if (!General.IsSimpleIdentifier(word.Text))
                    {
                        word.AddError("identifier expected");
                        word.SkipToKeyword(";");
                        break;
                    }
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    // Add to imported packages list (for specific item imports)
                    word.RootParsedDocument.ImportedPackages.Add(packageName);
                }

                if (word.Text != ",")
                {
                    break;
                }
                word.Color(CodeDrawStyle.ColorType.Normal);
                word.MoveNext();
            }
        }

        private static void ParsePackageImportItems(WordScanner word, NameSpace nameSpace)
        {
            /*
            package_import_item ::=
                  package_identifier :: identifier
                | package_identifier :: *
             */
            while (true)
            {
                // Parse package_identifier
                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("package identifier expected");
                    word.SkipToKeyword(";");
                    break;
                }
                string packageName = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text != "::")
                {
                    word.AddError(":: expected");
                    word.SkipToKeyword(";");
                    break;
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text == "*")
                {
                    // package_identifier :: *
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    // package_identifier :: identifier
                    if (!General.IsSimpleIdentifier(word.Text))
                    {
                        word.AddError("identifier expected");
                        word.SkipToKeyword(";");
                        break;
                    }
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                if (word.Text != ",")
                {
                    break;
                }
                word.Color(CodeDrawStyle.ColorType.Normal);
                word.MoveNext();
            }
        }

        // Keep old method for backward compatibility
        [System.Obsolete("Use ParseExportDeclaration instead")]
        public void Parse(WordScanner word, NameSpace nameSpace)
        {
            ParseExportDeclaration(word, nameSpace);
        }
    }
}
