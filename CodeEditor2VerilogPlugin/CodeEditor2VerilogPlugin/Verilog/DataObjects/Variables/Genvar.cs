using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Genvar : Variable
    {
        protected Genvar() { }

        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        [SetsRequiredMembers]
        public Genvar(string Name)
        {
            this.Name = Name;
        }

        public override Genvar Clone()
        {
            return Clone(Name);
        }

        public override Genvar Clone(string name)
        {
            Genvar val = new Genvar() { Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
        public static void ParseCreateFromDeclaration(WordScanner word, NameSpace nameSpace)
        {
            //            genvar_declaration::= genvar list_of_genvar_identifiers;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (!General.IsSimpleIdentifier(word.Text))
                {
                    word.AddError("illegal real identifier");
                    return;
                }
                Genvar val = new Genvar() { Name = word.Text };
                val.DefinedReference = word.GetReference();

                if (word.Active)
                {
                    if (word.Prototype)
                    {
                        if (nameSpace.NamedElements.ContainsKey(val.Name))
                        {
//                            nameRef.AddError("duplicated net name");
                        }
                        else
                        {
                            nameSpace.NamedElements.Add(val.Name, val);
                        }
                    }
                    else
                    {
                        if (nameSpace.NamedElements.ContainsKey(val.Name) && nameSpace.NamedElements[val.Name] is Genvar)
                        {
                            val = (Genvar)nameSpace.NamedElements[val.Name];
                            val.Defined = true;
                        }
                    }
                }

                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();

                if (word.GetCharAt(0) != ',') break;
                word.MoveNext();
            }

            if (word.Eof || word.GetCharAt(0) != ';')
            {
                word.AddError("; expected");
            }
            else
            {
                word.MoveNext();
            }

            return;
        }
    }
}
