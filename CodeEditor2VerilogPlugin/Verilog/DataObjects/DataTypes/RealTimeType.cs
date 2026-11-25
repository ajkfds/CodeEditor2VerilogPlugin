
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class RealTimeType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Realtime;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public virtual bool IsValidForNet { get { return false; } }
        public virtual bool PartSelectable { get { return false; } }
        public int? BitWidth
        {
            get
            {
                int size = 64;
                foreach (Arrays.PackedArray array in PackedDimensions)
                {
                    if (array.Size == null) return null;
                    size = size * (int)array.Size;
                }
                return null;
            }
        }
        public VerilogColorType ColorType { get { return VerilogColorType.Variable; } }

        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public string CreateString()
        {
            SystemVerilogCore.Documents.VerilogLabel label = new SystemVerilogCore.Documents.VerilogLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }
        public void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("realtime", VerilogColorType.Keyword);
        }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return RealTimeType.Create(array);
        }
        public static RealTimeType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            RealTimeType realTimeType = new RealTimeType() { };
            if (packedDimensions == null)
            {
                realTimeType.PackedDimensions.Clear();
            }
            else
            {
                realTimeType.PackedDimensions = packedDimensions;
            }
            return realTimeType;
        }
        public static RealTimeType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            RealTimeType dType = new RealTimeType();
            if (word.Text != "realtime") throw new Exception();
            word.Color(VerilogColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }
    }
}
