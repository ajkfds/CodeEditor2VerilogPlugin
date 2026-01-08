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
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Reg val = new Reg() { Name = Name, Defined = Defined };
            if(DataType != null) val.DataType = DataType.Clone();
            val.Signed = Signed;
            val.Defined = Defined;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }



    }
}
