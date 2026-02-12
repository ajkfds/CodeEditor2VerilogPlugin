using AjkAvaloniaLibs.Controls;
using DynamicData;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }
        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("realtime", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
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
            RealTimeType realTimeType = new RealTimeType() {};
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
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }
    }
}
