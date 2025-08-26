using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class AssociativeArray : DataObject,IArray
    {
        protected AssociativeArray() { }
        public int? Size { get; protected set; } = null;
        public bool Constant { get; protected set; } = false;

        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;

        public static AssociativeArray Create(DataObject dataObject, DataTypes.IDataType? indexDataType)
        {
            AssociativeArray associativeArray = new AssociativeArray() { Name = dataObject.Name };
            associativeArray.IndexDataType = indexDataType;

            DataTypes.IntegerVectorType? integerVectorType = indexDataType as DataTypes.IntegerVectorType;
            if (integerVectorType != null)
            {
                associativeArray.Size = integerVectorType.BitWidth;
                return associativeArray;
            }

            DataTypes.IntegerAtomType? integerAtomType = indexDataType as DataTypes.IntegerAtomType;
            if (integerAtomType != null)
            {
                associativeArray.Size = integerAtomType.BitWidth;
            }
            return associativeArray;
        }

        public DataTypes.IDataType? IndexDataType { get; set; } = null;

        public override DataObject Clone()
        {
            throw new NotImplementedException();
        }

        public override DataObject Clone(string name)
        {
            throw new NotImplementedException();
        }
    }
}
