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


        public static new Reg Create(string name,IDataType dataType)
        {
            DataTypes.IntegerVectorType dType = (DataTypes.IntegerVectorType)dataType;

            Reg val = new Reg() { Name = name };
            val.PackedDimensions = dType.PackedDimensions;
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Reg val = new Reg() { Name = Name };
            val.DataType = DataType;
            val.PackedDimensions = PackedDimensions;
            val.Signed = Signed;
            return val;
        }



    }
}
