using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Longint : IntegerAtomVariable
    {
        protected Longint() { }

        public static new Longint Create(IDataType dataType)
        {
            if (dataType.Type == DataTypeEnum.Int) System.Diagnostics.Debugger.Break();
            DataTypes.IntegerAtomType? dType = dataType as DataTypes.IntegerAtomType;
            if (dType == null) throw new Exception();

            Longint val = new Longint();
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Longint val = new Longint();
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }


    }
}
