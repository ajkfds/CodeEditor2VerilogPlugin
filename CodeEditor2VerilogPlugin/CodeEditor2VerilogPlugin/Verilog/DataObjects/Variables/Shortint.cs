using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Shortint : IntegerAtomVariable
    {
        protected Shortint() { }

        public static new Shortint Create(string name,IDataType dataType)
        {
            DataTypes.IntegerAtomType dType = (DataTypes.IntegerAtomType)dataType;

            Shortint val = new Shortint() { Name = name };
            val.DataType = dType;
            return val;
        }

        public override Variable Clone()
        {
            Shortint val = new Shortint() { Name = Name, Defined = Defined };
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }


    }
}
