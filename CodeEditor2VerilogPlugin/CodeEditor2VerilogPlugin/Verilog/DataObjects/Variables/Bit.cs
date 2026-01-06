using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Bit : IntegerVectorValueVariable
    {
        protected Bit() { }

        public override Bit Clone()
        {
            return Clone(Name);
        }

        public override Bit Clone(string name)
        {
            Bit val = new Bit() { Name = name };
            if (DataType != null) val.DataType = DataType.Clone();
            val.Signed = Signed;
            val.Defined = Defined;
            return val;
        }

        public static new Bit Create(string name,DataTypes.IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypes.DataTypeEnum.Bit);
            DataTypes.IntegerVectorType dType = (DataTypes.IntegerVectorType)dataType;

            Bit val = new Bit() { Name = name };
            val.DataType = dType;
            return val;
        }

    }
}
