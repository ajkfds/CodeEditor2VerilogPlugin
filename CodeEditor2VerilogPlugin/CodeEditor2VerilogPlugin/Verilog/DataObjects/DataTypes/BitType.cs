using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class BitType : IntegerVectorType
    {
        public static IntegerVectorType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            IntegerVectorType logicType = IntegerVectorType.Create(DataTypeEnum.Bit, signed, packedDimensions);
            return logicType;
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
