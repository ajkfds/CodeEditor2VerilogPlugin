using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class LogicType : IntegerVectorType
    {
        public static IntegerVectorType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            IntegerVectorType logicType = IntegerVectorType.Create(DataTypeEnum.Logic, signed, packedDimensions);
            return logicType;
        }
        public override bool IsValidForNet { 
            get {
                foreach(var array in PackedDimensions)
                {
                    if(!array.IsValidForNet) return false;
                }
                return true; 
            } 
        }
    }
}
