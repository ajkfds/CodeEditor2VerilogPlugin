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

        public static new Longint Create(string name, IDataType dataType)
        {
            DataTypes.IntegerAtomType dType = (DataTypes.IntegerAtomType)dataType;

            Longint val = new Longint() { Name = name };
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Longint val = new Longint() { Name = Name };
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }


    }
}
