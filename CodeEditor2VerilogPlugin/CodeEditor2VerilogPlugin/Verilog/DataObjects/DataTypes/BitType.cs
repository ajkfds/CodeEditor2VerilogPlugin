using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class BitType : IntegerVectorType
    {
        public static BitType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            BitType type = new BitType() { Type = DataTypeEnum.Bit, Signed = signed};
            if (packedDimensions == null)
            {
                type.PackedDimensions.Clear();
            }
            else
            {
                type.PackedDimensions = packedDimensions;
            }
            return type;
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
