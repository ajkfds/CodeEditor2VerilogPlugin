using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public interface IDataType
    {
        public DataTypeEnum Type { get; }
        public string CreateString();

        public int? BitWidth { get; }
        public CodeDrawStyle.ColorType ColorType { get; }
        public bool IsVector { get; }

        public List<Arrays.PackedArray> PackedDimensions { get; }

    }
}
