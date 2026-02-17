using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class LongIntType : IntegerAtomType
    {
        protected LongIntType() { }
        public static LongIntType Create(bool signed)
        {
            return new LongIntType() { Type = DataTypeEnum.Longint, Signed= signed };
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
