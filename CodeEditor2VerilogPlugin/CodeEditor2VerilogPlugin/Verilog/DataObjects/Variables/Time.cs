using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Time : IntegerAtomVariable, IPartSelectableDataObject
    {
        protected Time() { }

        public static new Time Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Time);
            DataTypes.IntegerAtomType dType = dataType as DataTypes.IntegerAtomType;

            Time val = new Time() { Name = name };
            val.DataType = dType;
            return val;
        }

        public override Variable Clone(string name)
        {
            Time val = new Time() { Name = name, Defined = Defined };
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
            TimeType? type = DataType as TimeType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }
    }
}
