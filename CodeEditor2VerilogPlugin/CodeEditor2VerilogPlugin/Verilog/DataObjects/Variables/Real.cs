using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Real : ValueVariable
    {
        protected Real() { }

        public static new Real Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Real);

            Real val = new Real() { Name = name };
            val.DataType = dataType;
            return val;
        }


        public override Variable Clone(string name)
        {
            Real val = new Real() { Name = name, Defined = Defined };
            val.DataType = DataType;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
        public override Variable Clone()
        {
            return Clone(Name);
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("real ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }

    }
}
