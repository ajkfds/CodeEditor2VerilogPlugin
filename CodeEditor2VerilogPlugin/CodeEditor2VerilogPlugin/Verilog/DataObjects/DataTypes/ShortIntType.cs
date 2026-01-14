using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ShortIntType : IntegerAtomType
    {
        public static IntegerAtomType? ParseCreate(bool signed)
        {
            return IntegerAtomType.Create(DataTypeEnum.Shortint, signed);
        }
    }
}
