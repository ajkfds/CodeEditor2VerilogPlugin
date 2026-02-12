using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ByteType : IntegerAtomType
    {
        public static IntegerAtomType Create(bool signed)
        {
            return IntegerAtomType.Create(DataTypeEnum.Byte, signed);
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
