using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Logic : IntegerVectorValueVariable
    {
        protected Logic() { }

 //       public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;

        public static new Logic Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Logic);
            DataTypes.IntegerVectorType? dType = dataType as DataTypes.IntegerVectorType;
            if (dType == null) throw new Exception();

            Logic val = new Logic() { Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone(string name)
        {
            Logic val = new Logic() {
                Name = name,
                DataType = DataType,
                Defined = Defined
            };
            return val;
        }
        public override Variable Clone()
        {
            Logic val = new Logic() { Name = Name };
            val.DataType = DataType;
            if (DataType != null) val.DataType = DataType.Clone();
            val.Signed = Signed;
            return val;
        }

    }
}
