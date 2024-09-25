using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class AssociativeArray : VariableArray
    {
        public AssociativeArray(DataTypes.IDataType? indexDataType)
        {
            IndexDataType = indexDataType;

            DataTypes.IntegerVectorType? integerVectorType = indexDataType as DataTypes.IntegerVectorType;
            if(integerVectorType != null)
            {
                Size = integerVectorType.BitWidth;
                return;
            }

            DataTypes.IntegerAtomType? integerAtomType = indexDataType as DataTypes.IntegerAtomType;
            if(integerAtomType != null)
            {
                Size = integerAtomType.BitWidth;
            }
        }
        public DataTypes.IDataType? IndexDataType { get; set; } = null;

    }
}
