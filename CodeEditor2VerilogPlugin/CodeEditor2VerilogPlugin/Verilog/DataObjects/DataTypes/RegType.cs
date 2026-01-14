using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class RegType : IntegerVectorType
    {
        public static IntegerVectorType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            IntegerVectorType logicType = IntegerVectorType.Create(DataTypeEnum.Reg, signed, packedDimensions);
            return logicType;
        }
    }
}
