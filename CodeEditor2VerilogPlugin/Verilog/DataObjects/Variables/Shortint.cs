using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Shortint : IntegerAtomVariable, IPartSelectableDataObject
    {
        protected Shortint() { }

        public static new Shortint Create(string name, IDataType dataType)
        {
            DataTypes.IntegerAtomType dType = (DataTypes.IntegerAtomType)dataType;

            Shortint val = new Shortint() { Name = name };
            val.DataType = dType;
            return val;
        }

        public override Variable Clone(string name)
        {
            Shortint val = new Shortint() { Name = name, Defined = Defined };
            val.DataType = DataType;
            val.Signed = Signed;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
        public override Variable Clone()
        {
            return Clone(Name);
        }

        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = DataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }

    }
}
