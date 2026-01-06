using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class IntegerVectorValueVariable : ValueVariable
    {
        //integer_vector_type::= bit | logic | reg
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Register; } }
        public bool Signed { get; set; }

        public List<DataObjects.Arrays.PackedArray> PackedDimensions
        {
            get
            {
                if(DataType == null) return new List<DataObjects.Arrays.PackedArray>();
                return DataType.PackedDimensions;
            }
        }

        public new static IntegerVectorValueVariable Create(string name,IDataType dataType)
        {
            switch (dataType.Type)
            {
                case DataTypeEnum.Bit:
                    return Bit.Create(name,dataType);
                case DataTypeEnum.Logic:
                    return Logic.Create(name, dataType);
                case DataTypeEnum.Reg:
                    return Reg.Create(name, dataType);
                default:
                    throw new Exception();
            }
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (DataType == null) return;

            switch (DataType.Type)
            {
                case DataTypeEnum.Bit:
                    label.AppendText("bit ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Logic:
                    label.AppendText("logic ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Reg:
                    label.AppendText("reg ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
            label.AppendText(" ");
            if (Signed)
            {
                label.AppendText("signed ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            }

            foreach (DataObjects.Arrays.PackedArray dimension in PackedDimensions)
            {
                label.AppendLabel(dimension.GetLabel());
                label.AppendText(" ");
            }
        }


    }
}
