﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Constants
{
    public class EnumConstants : Constants
    {
        public static EnumConstants Create(string name,DataTypes.IDataType dataType)
        {
            EnumConstants constants = new EnumConstants() { Name = name };
            constants.DataType = dataType;
            constants.ConstantType = ConstantTypeEnum.enum_;
            return constants;
        }
    }
}
