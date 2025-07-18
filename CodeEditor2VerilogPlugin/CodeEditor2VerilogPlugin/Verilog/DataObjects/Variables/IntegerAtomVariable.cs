﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class IntegerAtomVariable : ValueVariable
    {
        //integer_atom_type::= byte | shortint | int | longint | integer | time
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public bool Signed { get; set; }

        public new static IntegerAtomVariable Create(string name, IDataType dataType)
        {
            switch (dataType.Type)
            {
                case DataTypeEnum.Byte:
                    return Byte.Create(name,dataType);
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

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (DataType == null) return;
            switch (DataType.Type)
            {
                case DataTypeEnum.Byte:
                    label.AppendText("byte ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Shortint:
                    label.AppendText("shortint ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Int:
                    label.AppendText("int ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Longint:
                    label.AppendText("longint ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Integer:
                    label.AppendText("integer ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Time:
                    label.AppendText("time ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DataTypeEnum.Class:
                    label.AppendText("class ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
                    break;
                default:
                    throw new Exception();
            }
            label.AppendText(" ");
            if (Signed)
            {
                label.AppendText("signed ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            }
        }



    }
}
