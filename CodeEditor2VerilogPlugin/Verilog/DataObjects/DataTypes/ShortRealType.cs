
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ShortRealType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Shortreal;
            }
        }
        public bool Packable { get { return false; } }
        public bool IsVector { get { return false; } }
        public bool IsValidForNet { get { return false; } }

        public int? BitWidth { get; } = 32;
        public virtual bool PartSelectable { get { return false; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public VerilogColorType ColorType { get { return VerilogColorType.Variable; } }
        public string CreateString()
        {
            SystemVerilogCore.Documents.VerilogLabel label = new SystemVerilogCore.Documents.VerilogLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("shortreal", VerilogColorType.Keyword);
        }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return ShortRealType.Create(array);
        }
        public static ShortRealType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            ShortRealType shortrealType = new ShortRealType() { };
            if (packedDimensions == null)
            {
                shortrealType.PackedDimensions.Clear();
            }
            else
            {
                shortrealType.PackedDimensions = packedDimensions;
            }
            return shortrealType;
        }
        public static ShortRealType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ShortRealType dType = new ShortRealType();
            if (word.Text != "shortreal") throw new Exception();
            word.Color(VerilogColorType.Keyword);
            word.MoveNext();
            return dType;
        }

    }
}
