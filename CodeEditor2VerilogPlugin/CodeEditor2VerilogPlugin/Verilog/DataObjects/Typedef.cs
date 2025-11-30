using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


            if (word.Active)
            {
                if (word.Prototype)
                {
                    if (nameSpace.NamedElements.ContainsKey(typeDef.Name))
                    {
                        //                            nameRef.AddError("duplicated name");
                    }
                    else
                    {
                        nameSpace.NamedElements.Add(typeDef.Name, typeDef);
                    }
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

            if(word.Text != ";")
            {
                word.AddError("; expected");
                return true;
            }
            word.MoveNext();    // ;

            return true;
        }



    }
}
