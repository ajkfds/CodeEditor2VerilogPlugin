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
    public class StringType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.String;
            }
        }
        public int? BitWidth
        {
            get
            {
                return null;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("string", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return StringType.Create(array);
        }
        public static StringType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            StringType stringType = new StringType() { };
            if (packedDimensions == null)
            {
                stringType.PackedDimensions.Clear();
            }
            else
            {
                stringType.PackedDimensions = packedDimensions;
            }
            return stringType;
        }
        public static StringType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            StringType dType = new StringType();
            if (word.Text != "string") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }
    }
}
