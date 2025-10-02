using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class StringType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.String;
            }
        }
        public int? BitWidth
        {
            get
            {
                return null;
            }
        }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public static StringType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            StringType dType = new StringType();
            if (word.Text != "string") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public virtual string CreateString()
        {
            return "string";
        }
        public bool IsVector { get { return false; } }
    }
}
