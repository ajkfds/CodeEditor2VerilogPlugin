using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class RegType : IntegerVectorType
    {
        protected RegType() { }
        public static RegType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            RegType type = new RegType() { Type = DataTypeEnum.Reg, Signed = signed };
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
        public override bool IsValidForNet
        {
            get
            {
                foreach (var array in PackedDimensions)
                {
                    if (!array.IsValidForNet) return false;
                }
                return true;
            }
        }
    }
}
