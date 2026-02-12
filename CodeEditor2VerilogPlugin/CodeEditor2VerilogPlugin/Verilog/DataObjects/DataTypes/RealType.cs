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
    public class RealType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Real;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
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
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("real", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return RealType.Create(array);
        }
        public static RealType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            RealType realType = new RealType() { };
            if (packedDimensions == null)
            {
                realType.PackedDimensions.Clear();
            }
            else
            {
                realType.PackedDimensions = packedDimensions;
            }
            return realType;
        }
        public static RealType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            RealType dType = new RealType();
            if (word.Text != "real") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }

        public bool IsValidForNet { get { return false; } }
    }
}
