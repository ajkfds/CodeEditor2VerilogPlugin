using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Int : IntegerAtomVariable
    {
        protected Int() { }

        public static new Int Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Int);
            DataTypes.IntegerAtomType? dType = dataType as DataTypes.IntegerAtomType;
            if (dType == null) throw new Exception();

            Int val = new Int() { Name = name };
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Int val = new Int() { Name = Name };
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }


    }
}
