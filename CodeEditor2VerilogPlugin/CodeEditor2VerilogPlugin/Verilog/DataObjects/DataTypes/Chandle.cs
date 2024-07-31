using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class Chandle : DataType
    {
        public static Chandle ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Chandle dType = new Chandle();
            if (word.Text != "chandle") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            dType.Type = DataTypeEnum.Chandle;
            word.MoveNext();
            return dType;
        }
        public override string CreateString()
        {
            return "chandle";
        }
        public static Chandle Create(IDataType dataType)
        {
            Chandle chandle = new Chandle();
            chandle.Type = dataType.Type;
            return chandle;
        }

    }
}
