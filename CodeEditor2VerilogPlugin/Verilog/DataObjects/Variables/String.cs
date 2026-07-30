using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class String : ValueVariable, IPartSelectableDataObject
    {
        protected String()
        {
        }

        public override int? BitWidth
        {
            get
            {
                if (Length == null) return null;
                return Length * 8;
            }
        }

        public int? Length = null;
        public static new String Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.String);

            String val = new String() { Name = name };
            val.DataType = dataType;

            return val;
        }

        // substrの戻り値がStringを持つため、遅延評価される必要がある。
        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;
                namedElements = new NamedElements();

                if (DataType == null) return namedElements;
                DataType.AppendChiledNamedElements(namedElements);
                return namedElements;
            }
        }

        public override Variable Clone(string name)
        {
            String val = new String() { Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataType = DataType;
            return val;
        }
        public override Variable Clone()
        {
            return Clone(Name);
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("string ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = DataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }

    }
}
