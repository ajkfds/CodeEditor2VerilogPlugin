using AjkAvaloniaLibs.Controls;
using DynamicData;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class StringType : IDataType, IPartSelectableDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.String;
            }
        }
        public bool IsValidForNet { get { return false; } }
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
        public virtual bool PartSelectable { get { return true; } }
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

        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            if (word.Eof || word.Text != "[") return null;

            RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
            if (rangeExpression == null) return null;

            if (rangeExpression is SingleBitRangeExpression)
            {
                SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                if (!word.Prototype && singleBitRangeExpression.BitIndex != null)
                {
                    if (singleBitRangeExpression.BitIndex < 0 || singleBitRangeExpression.BitIndex >= BitWidth)
                    {
                        singleBitRangeExpression.WordReference.AddError("index out of range");
                    }
                }

                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(1));
                return DataObjects.DataTypes.ByteType.Create(false);
            }
            else
            {
                if (!word.Prototype) rangeExpression.WordReference.AddError("use substr method");
                return null;
            }
        }

    }
}
