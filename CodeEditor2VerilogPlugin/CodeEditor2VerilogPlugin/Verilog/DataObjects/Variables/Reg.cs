using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Reg : IntegerVectorValueVariable
    {
        protected Reg() { }

        public static new Reg Create(DataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Reg);
            DataTypes.IntegerVectorType dType = dataType as DataTypes.IntegerVectorType;

            Reg val = new Reg();
            val.PackedDimensions = dType.PackedDimensions;
            val.DataType = dType.Type;
            return val;
        }

        public override Variable Clone()
        {
            Reg val = new Reg();
            val.DataType = DataType;
            val.PackedDimensions = PackedDimensions;
            val.Signed = Signed;
            return val;
        }



    }
}
