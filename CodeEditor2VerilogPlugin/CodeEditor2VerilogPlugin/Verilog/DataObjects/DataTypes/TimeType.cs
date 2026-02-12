using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class TimeType : IntegerAtomType
    {
        public static IntegerAtomType? ParseCreate(bool signed)
        {
            return IntegerAtomType.Create(DataTypeEnum.Time, signed);
        }
        public bool IsValidForNet
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
