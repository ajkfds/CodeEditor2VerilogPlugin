
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class Chandle : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Chandle;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public int? BitWidth { get; } = null;
        public virtual bool PartSelectable { get { return false; } }
        public bool IsValidForNet { get { return false; } }
        public VerilogColorType ColorType { get { return VerilogColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();
        public string CreateString()
        {
            SystemVerilogCore.Documents.VerilogLabel label = new SystemVerilogCore.Documents.VerilogLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("chandle ", VerilogColorType.Keyword);
        }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return Chandle.Create(array);
        }
        public static Chandle Create(List<Arrays.PackedArray>? packedDimensions)
        {
            Chandle chandleType = new Chandle() { };
            if (packedDimensions == null)
            {
                chandleType.PackedDimensions.Clear();
            }
            else
            {
                chandleType.PackedDimensions = packedDimensions;
            }
            return chandleType;
        }
        public static Chandle ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Chandle dType = new Chandle();
            if (word.Text != "chandle") throw new Exception();
            word.Color(VerilogColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public static Chandle Create(IDataType dataType)
        {
            Chandle chandle = new Chandle();
            return chandle;
        }
        public bool IsVector { get { return false; } }

    }
}
