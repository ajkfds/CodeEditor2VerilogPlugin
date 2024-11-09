using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Integer : IntegerAtomVariable
    {
        protected Integer() { }

        public static new Integer Create(IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Int);
            DataTypes.IntegerAtomType? dType = dataType as DataTypes.IntegerAtomType;
            if (dType == null) throw new Exception();

            Integer val = new Integer();
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Integer val = new Integer();
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }

    }
}
