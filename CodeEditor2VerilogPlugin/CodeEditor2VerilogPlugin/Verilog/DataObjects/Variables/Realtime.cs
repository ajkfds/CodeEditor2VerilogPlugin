﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Realtime : Variable
    {
        protected Realtime() { }

        public static new Realtime Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Realtime);

            Realtime val = new Realtime() { Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone()
        {
            Realtime val = new Realtime() { Name = Name };
            val.DataType = DataType;
            return val;
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("realtime ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }

    }
}
