using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class TimeType : IntegerAtomType
    {
        protected TimeType() { }
        public static TimeType Create(bool signed)
        {
            return new TimeType() { Type = DataTypeEnum.Time, Signed = signed };
        }
        public override bool IsValidForNet
        {
            get
            {
                return true;
            }
        }
    }
}
