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
         
         */
        public void Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "export" && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text != "::" && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text != "*" && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            else
            {
                //while (true)
                //{
                //    if (!PackageImportItem.Parse(word, nameSpace))
                //    {
                //        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                //        return false;
                //    }
                //    if (word.Text != ",")
                //    {
                //        break;
                //    }
                //    word.Color(CodeDrawStyle.ColorType.Normal);
                //    word.MoveNext();
                //}
            }
        }
    }
}
