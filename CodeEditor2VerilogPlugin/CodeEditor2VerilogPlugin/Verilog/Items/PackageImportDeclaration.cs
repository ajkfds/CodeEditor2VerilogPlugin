using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class PackageImportDeclaration
    {
        /*
        package_import_declaration ::=
            import package_import_item { , package_import_item } ;
        package_import_item ::=
              package_identifier :: identifier
            | package_identifier :: *         
         
         */
        public static void Parse(WordScanner word, NameSpace nameSpace)
        {
            if(word.Text != "import" && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            string packageIdentifier = word.Text;
            Package? package = word.ProjectProperty.GetBuildingBlock(packageIdentifier) as Package;


            while (!word.Eof)
            {
                if (package == null)
                {
                    word.AddError("Package not found: " + packageIdentifier);
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text != "::")
                    {
                        word.AddError("Expected ::");
                        return;
                    }
                    word.MoveNext();
                    if (word.Text == "*")
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                        return;
                    }
                    if (!General.IsIdentifier(word.Text))
                    {
                        word.AddError("illegal identifier");
                        return;
                    }
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    if (word.Text != ",") break;
                    continue;
                }

                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if (word.Text != "::")
                {
                    word.AddError("Expected ::");
                    return;
                }
                word.MoveNext();
                if (word.Text == "*")
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    foreach (var namedElement in package.NamedElements)
                    {
                        if (nameSpace.BuildingBlock.NamedElements.ContainsKey(namedElement.Name))
                        {
                            if (!word.Prototype) word.AddError("Name conflict: " + namedElement.Name);
                            nameSpace.BuildingBlock.NamedElements.Replace(namedElement.Name, namedElement);
                        }
                        else
                        {
                            nameSpace.BuildingBlock.NamedElements.Add(namedElement.Name, namedElement);
                        }
                    }
                    if (word.Text != ",") break;
                    continue;
                }

                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal identifier");
                    return;
                }
                word.Color(CodeDrawStyle.ColorType.Identifier);
                string identifier = word.Text;
                INamedElement? targetElement = null;
                if (package.NamedElements.ContainsKey(identifier)) targetElement = package.NamedElements[identifier];
                if (targetElement != null)
                {
                    if (nameSpace.BuildingBlock.NamedElements.ContainsKey(identifier))
                    {
                        if (!word.Prototype) word.AddError("Name conflict: " + identifier);
                        nameSpace.BuildingBlock.NamedElements.Replace(targetElement.Name, targetElement);
                    }
                    else
                    {
                        nameSpace.BuildingBlock.NamedElements.Add(targetElement.Name, targetElement);
                    }
                }
                word.MoveNext();

                if (word.Text != ",") break;
                continue;
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
            }
        }
    }
}
