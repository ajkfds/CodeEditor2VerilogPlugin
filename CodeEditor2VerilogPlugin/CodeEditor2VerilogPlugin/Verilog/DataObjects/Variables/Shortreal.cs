using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Shortreal : ValueVariable
    {
        protected Shortreal() { }

        public static new Shortreal Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Shortreal );

            Shortreal val = new Shortreal() { Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone()
        {
            Shortreal val = new Shortreal() { Name = Name, Defined = Defined };
            val.DataType = DataType;
            return val;
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("shortreal ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }

    }
}
