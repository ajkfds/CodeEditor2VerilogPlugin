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
    public class ShortRealType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Shortreal;
            }
        }
        public int? BitWidth { get; } = 32;
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
            label.AppendText("shortreal", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
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
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }
    }
}
