using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Object : Variable
    {
        protected Object() { }

        public BuildingBlocks.Class Class;

        public static new Object Create(IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Class);
            BuildingBlocks.Class class_ = dataType as BuildingBlocks.Class;

            Object val = new Object();
            val.DataType = DataTypeEnum.Class;
            val.Class = class_;
            return val;
        }

        public override Variable Clone()
        {
            Object val = new Object();
            val.DataType = DataType;
            return val;
        }

    }
}
