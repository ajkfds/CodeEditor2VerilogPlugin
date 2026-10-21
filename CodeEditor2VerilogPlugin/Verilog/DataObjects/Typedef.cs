using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;

namespace pluginVerilog.Verilog.DataObjects
{
    public class Typedef : INamedElement
    {
        public required IDataType VariableType { get; init; }
        public required string Name { get; init; }
        public virtual CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Identifier; } }

        public NamedElements NamedElements { get; } = new NamedElements();
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        public void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("typedef ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            label.AppendText("\n");

            if (VariableType != null)
            {
                VariableType.AppendTypeLabel(label);
                label.AppendText("\n");
            }
        }
        public static bool ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {

            /* ## SystemVerilog2012
             type_declaration ::= 
                  "typedef" data_type type_identifier { variable_dimension } ;
                | "typedef" interface_instance_identifier constant_bit_select . type_identifier type_identifier ;
                | "typedef" [ "enum" | "struct" | "union" | "class" | "interface class" ] type_identifier ;
             */
            if (word.Text != "typedef") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // "typedef class A;" / "typedef interface class A;" forward declaration
            // register nothing (real Class is registered by its own declaration).
            // Registering a Typedef would conflict with the real Class name and
            // degrade member resolution via UserDefinedType.
            if (word.Text == "class" || (word.Text == "interface" && word.NextText == "class"))
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text == "class")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                if (General.IsIdentifier(word.Text))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                if (word.Text == ";")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("; expected");
                    word.SkipToKeyword(";");
                }
                return true;
            }

            // forward declarations
            //   typedef_identifier ;                    (forward typedef)
            //   typedef enum/struct/union identifier ;  (forward type declaration)
            if (word.Text == "enum" || word.Text == "struct" || word.Text == "union")
            {
                WordScanner probe = word.Clone(true);
                probe.MoveNext(); // enum / struct / union
                if (General.IsSimpleIdentifier(probe.Text) && probe.NextText == ";")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext(); // keyword
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext(); // identifier
                    word.MoveNext(); // ;
                    return true;
                }
            }
            else if (General.IsSimpleIdentifier(word.Text) && word.NextText == ";")
            {
                // forward typedef: "typedef identifier ;"
                // The actual data type definition shall be resolved within the
                // same local scope; it is an error when the type_identifier does
                // not resolve to a data type (IEEE 1800-2017 6.18). The check is
                // deferred until the whole file has been parsed, because a
                // forward typedef may precede its definition.
                word.Color(CodeDrawStyle.ColorType.Identifier);
                if (!word.Prototype && word.CompletionContext == null)
                {
                    word.RootParsedDocument.ForwardTypedefs.Add(
                        new ParsedDocument.ForwardTypedefDeclaration()
                        {
                            Name = word.Text,
                            Reference = word.GetReference(),
                            DeclaringNameSpace = nameSpace
                        });
                }
                word.MoveNext(); // identifier
                word.MoveNext(); // ;
                return true;
            }

            word.AddSystemVerilogError();

            IDataType? iDataType = DataTypeFactory.ParseCreate(word, nameSpace, null);
            if (iDataType == null)
            {
                word.AddError("data type expected");
                word.SkipToKeyword(";");
                return true;
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal type_identifier");
                word.SkipToKeyword(";");
                return true;
            }

            word.Color(CodeDrawStyle.ColorType.Identifier);
            Typedef typeDef = new Typedef() { Name = word.Text, VariableType = iDataType };
            word.MoveNext();

            // { variable_dimension } : unpacked / associative / queue / dynamic dimensions
            // (e.g. "typedef reg quartet[3:0];", "typedef data_t arr [k:0][j:0];")
            while (word.Text == "[" && !word.Eof)
            {
                int depth = 0;
                while (!word.Eof)
                {
                    if (word.Text == "[") depth++;
                    else if (word.Text == "]") depth--;
                    word.MoveNext();
                    if (depth == 0) break;
                }
            }


            if (word.Active)
            {
               if (word.Prototype)
                {
                    if (word.CompletionContext != null)
                    {
                        // do not update building block tree @ code completion partial parse
                    }
                    else if (nameSpace.NamedElements.ContainsKey(typeDef.Name))
                    {
                        //                            nameRef.AddError("duplicated name");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(typeDef.Name, typeDef);
                    }
                }
                else if (word.CompletionContext != null)
                {
                    // do not update building block tree @ code completion partial parse
                }
                else
                {
                    if (nameSpace.NamedElements.ContainsKey(typeDef.Name))
                    {
                        if (nameSpace.NamedElements[typeDef.Name] is Typedef)
                        {
                            typeDef = (Typedef)nameSpace.NamedElements[typeDef.Name];
                        }
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(typeDef.Name, typeDef);
                    }
                }
            }

            if (word.Text != ";")
            {
                word.AddError("; expected");
                return true;
            }
            word.MoveNext();    // ;

            return true;
        }



    }
}
