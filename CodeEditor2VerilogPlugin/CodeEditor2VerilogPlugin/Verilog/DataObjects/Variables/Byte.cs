using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Byte : IntegerAtomVariable, IPartSelectableDataObject
    {
        protected Byte() { }

        public override Byte Clone()
        {
            return Clone(Name);
        }

        public override Byte Clone(string name)
        {
            Byte val = new Byte() { Name = name, Defined = Defined, Signed = Signed };
            if (DataType != null) val.DataType = DataType.Clone();
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
        public static new Byte Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Byte);
            DataTypes.IntegerAtomType? dType = dataType as DataTypes.IntegerAtomType;
            if (dType == null) throw new Exception();

            Byte val = new Byte() { Name = name };
            val.DataType = dType;
            return val;
        }

        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = DataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }

    }
}
