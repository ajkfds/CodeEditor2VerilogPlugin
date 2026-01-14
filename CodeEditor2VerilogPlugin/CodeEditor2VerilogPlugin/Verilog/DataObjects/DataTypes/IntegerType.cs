using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntegerType : IntegerAtomType
    {
        public static IntegerAtomType Create(bool signed)
        {
            return IntegerAtomType.Create(DataTypeEnum.Integer, signed);
        }
    }
}
