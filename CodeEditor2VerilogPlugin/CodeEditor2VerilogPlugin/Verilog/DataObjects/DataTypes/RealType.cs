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
