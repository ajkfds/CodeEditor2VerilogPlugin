using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public interface IPartSelectableDataObject
    {
        public int GetBitWidthPartSelectReference(Expressions.DataObjectReference val, RangeExpression? rangeExpression,bool prototype)
        {
            if (val.DataObject is not IPartSelectableDataObject) throw new Exception();

            if (rangeExpression is SingleBitRangeExpression)
            {
                SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                if (!prototype && singleBitRangeExpression.BitIndex != null)
                {
                    if (singleBitRangeExpression.BitIndex < 0 || singleBitRangeExpression.BitIndex >= val.DataObject.BitWidth)
                    {
                        singleBitRangeExpression.WordReference.AddError("index out of range");
                    }
                }

                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(1));
                val.DataObject = DataObjects.Variables.Logic.Create(val.DataObject.Name, DataObjects.DataTypes.LogicType.Create(false, packedDimensions));
                return 1;
            }
            else
            {
                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(rangeExpression.BitWidth));
                val.DataObject = DataObjects.Variables.Logic.Create(val.DataObject.Name, DataObjects.DataTypes.LogicType.Create(false, packedDimensions));
                return rangeExpression.BitWidth;
            }
        }
    }
}
