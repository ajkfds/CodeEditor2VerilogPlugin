using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ShortRealType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Shortreal;
            }
        }
        public static ShortRealType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ShortRealType dType = new ShortRealType();
            if (word.Text != "shortreal") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public virtual string CreateString()
        {
            return "shortreal";
        }
        public bool IsVector { get { return false; } }
    }
}
