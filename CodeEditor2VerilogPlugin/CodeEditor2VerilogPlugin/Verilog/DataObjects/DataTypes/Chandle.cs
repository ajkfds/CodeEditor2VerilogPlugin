using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class Chandle : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Chandle;
            }
        }
        public int? BitWidth { get; } = null;
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();
        public static Chandle ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Chandle dType = new Chandle();
            if (word.Text != "chandle") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public virtual string CreateString()
        {
            return "chandle";
        }
        public static Chandle Create(IDataType dataType)
        {
            Chandle chandle = new Chandle();
            return chandle;
        }
        public bool IsVector { get { return false; } }

    }
}
