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
        protected LogicType() { }
        public static LogicType Create(bool signed, List<Arrays.PackedArray>? packedDimensions)
        {
            LogicType type = new LogicType() { Type = DataTypeEnum.Logic, Signed = signed };
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
