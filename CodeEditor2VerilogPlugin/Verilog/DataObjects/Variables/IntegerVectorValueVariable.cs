using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class IntegerVectorValueVariable : ValueVariable, IPackedDataObject
    {
        //integer_vector_type::= bit | logic | reg
        public override VerilogColorType ColorType { get { return VerilogColorType.Register; } }
        public bool Signed { get; set; }

        public List<DataObjects.Arrays.PackedArray> PackedDimensions
        {
            get
            {
                if (DataType == null) return new List<DataObjects.Arrays.PackedArray>();
                return DataType.PackedDimensions;
            }
        }

        public new static IntegerVectorValueVariable Create(string name, IDataType dataType)
        {
            switch (dataType.Type)
            {
                case DataTypeEnum.Bit:
                    return Bit.Create(name, dataType);
                case DataTypeEnum.Logic:
                    return Logic.Create(name, dataType);
                case DataTypeEnum.Reg:
                    return Reg.Create(name, dataType);
                default:
                    throw new Exception();
            }
        }

        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            if (DataType == null) return;

            switch (DataType.Type)
            {
                case DataTypeEnum.Bit:
                    label.AppendText("bit ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Logic:
                    label.AppendText("logic ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Reg:
                    label.AppendText("reg ", VerilogColorType.Keyword);
                    break;
                default:
                    System.Diagnostics.Debugger.Break();
                    break;
            }
            label.AppendText(" ");
            if (Signed)
            {
                label.AppendText("signed ", VerilogColorType.Keyword);
            }

            foreach (DataObjects.Arrays.PackedArray dimension in PackedDimensions)
            {
                label.AppendLabel(dimension.GetLabel());
                label.AppendText(" ");
            }
        }


    }
}
