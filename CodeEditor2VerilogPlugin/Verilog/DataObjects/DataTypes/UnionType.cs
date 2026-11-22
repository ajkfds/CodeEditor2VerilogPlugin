
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    /// <summary>
    /// SystemVerilog union (packed/unpacked).
    /// struct_union ::= "struct" | "union" [ "tagged" ]
    /// BitWidth is the maximum width of its members.
    /// </summary>
    public class UnionType : StructType
    {
        public override DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Union;
            }
        }

        public override int? BitWidth
        {
            get
            {
                int size = 0;
                foreach (Member member in Members.Values)
                {
                    if (member.DatType.BitWidth == null)
                    {
                        return null;
                    }
                    if ((int)member.DatType.BitWidth > size) size = (int)member.DatType.BitWidth;
                }
                return size;
            }
        }

        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("union", CodeDrawStyle.ColorType.Keyword);
            if (Tagged) label.AppendText(" tagged", CodeDrawStyle.ColorType.Keyword);
            if (Packed) label.AppendText(" packed", CodeDrawStyle.ColorType.Keyword);
            if (Signed) label.AppendText(" signed", CodeDrawStyle.ColorType.Keyword);
            if (Members.Count != 0)
            {
                label.AppendText("{\n");
                foreach (Member member in Members.Values)
                {
                    label.AppendText("\t");
                    member.AppendTypeLabel(label);
                    label.AppendText("\n");
                }
                label.AppendText("}");
            }
        }

        public new IDataType Clone()
        {
            UnionType unionType = new UnionType() { Signed = Signed, Tagged = Tagged, IsUnion = true };
            foreach (Member member in Members.Values)
            {
                unionType.Members.Add(member.Identifier, member.Clone());
            }
            foreach (var packedDimention in PackedDimensions)
            {
                unionType.PackedDimensions.Add(packedDimention.Clone());
            }
            return unionType;
        }

        /// <summary>
        /// Parse "union [ tagged ] [ packed [ signing ] ] { members } { packed_dimension }"
        /// </summary>
        public static UnionType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "union") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.AddSystemVerilogError();
            word.MoveNext();

            UnionType type = new UnionType();
            type.IsUnion = true;
            return parseCommon(type, word, nameSpace) as UnionType;
        }
    }
}
