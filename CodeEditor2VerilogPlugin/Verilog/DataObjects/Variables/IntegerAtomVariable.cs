using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class IntegerAtomVariable : ValueVariable, IPackedDataObject
    {
        //integer_atom_type::= byte | shortint | int | longint | integer | time
        public override VerilogColorType ColorType { get { return VerilogColorType.Variable; } }
        public bool Signed { get; set; }
        public List<DataObjects.Arrays.PackedArray> PackedDimensions
        {
            get
            {
                if (DataType == null) return new List<DataObjects.Arrays.PackedArray>();
                return DataType.PackedDimensions;
            }
        }

        public new static IntegerAtomVariable Create(string name, IDataType dataType)
        {
            switch (dataType.Type)
            {
                case DataTypeEnum.Byte:
                    return Byte.Create(name, dataType);
                case DataTypeEnum.Shortint:
                    return Shortint.Create(name, dataType);
                case DataTypeEnum.Int:
                    return Int.Create(name, dataType);
                case DataTypeEnum.Longint:
                    return Longint.Create(name, dataType);
                case DataTypeEnum.Integer:
                    return Integer.Create(name, dataType);
                case DataTypeEnum.Time:
                    return Time.Create(name, dataType);
                default:
                    throw new Exception();
            }
        }

        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            if (DataType == null) return;
            switch (DataType.Type)
            {
                case DataTypeEnum.Byte:
                    label.AppendText("byte ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Shortint:
                    label.AppendText("shortint ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Int:
                    label.AppendText("int ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Longint:
                    label.AppendText("longint ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Integer:
                    label.AppendText("integer ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Time:
                    label.AppendText("time ", VerilogColorType.Keyword);
                    break;
                case DataTypeEnum.Class:
                    label.AppendText("class ", VerilogColorType.Identifier);
                    break;
                default:
                    throw new Exception();
            }
            label.AppendText(" ");
            if (Signed)
            {
                label.AppendText("signed ", VerilogColorType.Keyword);
            }
        }



    }
}
