using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class RealType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Real;
            }
        }

        public int? BitWidth
        {
            get
            {
                int size = 64;
                foreach (Arrays.PackedArray array in PackedDimensions)
                {
                    if (array.Size == null) return null;
                    size = size * (int)array.Size;
                }
                return null;
            }
        }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public static RealType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            RealType dType = new RealType();
            if (word.Text != "real") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public virtual string CreateString()
        {
            return "real";
        }
        public bool IsVector { get { return false; } }
    }
}
