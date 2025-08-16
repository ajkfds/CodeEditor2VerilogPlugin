﻿using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Byte : IntegerAtomVariable
    {
        protected Byte() { }

        public override Byte Clone()
        {
            return Clone(Name);
        }

        public override Byte Clone(string name)
        {
            Byte val = new Byte() { Name = name };
            val.DataType = DataType;
            val.Signed = Signed;
            return val;
        }
        public static new Byte Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Byte);
            DataTypes.IntegerAtomType? dType = dataType as DataTypes.IntegerAtomType;
            if (dType == null) throw new Exception();

            Byte val = new Byte() { Name = name };
            val.DataType = dType;
            return val;
        }


    }
}
