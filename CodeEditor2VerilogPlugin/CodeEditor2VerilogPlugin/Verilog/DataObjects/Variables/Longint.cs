using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Longint : IntegerAtomVariable, IPartSelectableDataObject
    {
        protected Longint() { }

//        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;
        public static new Longint Create(string name, IDataType dataType)
        {
            DataTypes.IntegerAtomType dType = (DataTypes.IntegerAtomType)dataType;

            Longint val = new Longint() { Name = name };
            val.DataType = dType;
            return val;
        }
        public override Variable Clone(string name)
        {
            Longint val = new Longint()
            {
                Name = name,
                DataType = DataType
            };
            return val;
        }

        public override Variable Clone()
        {
            Longint val = new Longint() { Name = Name, Defined = Defined };
            val.DataType = DataType;
            val.Signed = Signed;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
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
