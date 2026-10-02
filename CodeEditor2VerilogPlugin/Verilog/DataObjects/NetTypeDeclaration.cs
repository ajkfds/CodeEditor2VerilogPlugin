using System.Collections.Generic;

using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects
{
    /// <summary>
    /// net_type_declaration (IEEE 1800-2017 A.2.1.4)
    ///
    /// net_type_declaration ::=
    ///       "nettype" data_type net_type_identifier [ "with" [ package_scope ] tf_identifier ] ";"
    ///     | "nettype" [ package_scope ] net_type_identifier net_type_identifier ";"
    ///
    /// Declares a user-defined net type (used with user-defined nettypes / resolution functions).
    /// The parser registers the net type name and consumes the declaration.
    /// </summary>
    public class NetTypeDeclaration : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            // "typedef" "nettype" is handled here (typedef keyword consumed by caller)
            if (word.Text == "typedef")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (word.Text != "nettype") return false;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            NetTypeDeclaration netTypeDecl = new NetTypeDeclaration();

            // data_type or [ package_scope ] net_type_identifier
            DataObjects.DataTypes.IDataType? dataType = null;
            if (word.Text == "logic" || word.Text == "bit" || word.Text == "reg" ||
                word.Text == "wire" || word.Text == "tri" || word.Text == "uwire" ||
                word.Text == "wand" || word.Text == "wor" || word.Text == "tri0" ||
                word.Text == "tri1" || word.Text == "triand" || word.Text == "trior" ||
                word.Text == "trireg" || word.Text == "supply0" || word.Text == "supply1" ||
                word.Text == "interconnect" ||
                word.Text == "byte" || word.Text == "shortint" || word.Text == "int" ||
                word.Text == "longint" || word.Text == "integer" || word.Text == "time" ||
                word.Text == "shortreal" || word.Text == "real" || word.Text == "realtime" ||
                word.Text == "struct" || word.Text == "union" || word.Text == "enum" ||
                word.Text == "signed" || word.Text == "unsigned" || word.Text == "[" )
            {
                dataType = DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
                netTypeDecl.Name = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            else
            {
                // [ package_scope ] net_type_identifier net_type_identifier
                if (word.NextText == "::")
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    word.MoveNext(); // ::
                }
                // source net type identifier
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                // target net type identifier
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("net type identifier expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }
                netTypeDecl.Name = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            // [ "with" [ package_scope ] tf_identifier ]
            if (word.Text == "with")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.NextText == "::")
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    word.MoveNext(); // ::
                }
                if (General.IsIdentifier(word.Text))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }

            // register the net type name
            if (word.CompletionContext == null && !string.IsNullOrEmpty(netTypeDecl.Name))
            {
                if (!nameSpace.NamedElements.ContainsKey(netTypeDecl.Name))
                {
                    nameSpace.NamedElements.Add(netTypeDecl.Name, netTypeDecl);
                }
            }

            return true;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType)
            );
        }

        public void DisposeSubReference()
        {
        }
    }
}
