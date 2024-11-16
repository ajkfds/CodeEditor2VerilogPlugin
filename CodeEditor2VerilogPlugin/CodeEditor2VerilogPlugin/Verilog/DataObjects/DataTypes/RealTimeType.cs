using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class RealTimeType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Realtime;
            }
        }

        public static RealTimeType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            RealTimeType dType = new RealTimeType();
            if (word.Text != "realtime") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public virtual string CreateString()
        {
            return "realtime";
        }
        public bool IsVector { get { return false; } }
    }
}
