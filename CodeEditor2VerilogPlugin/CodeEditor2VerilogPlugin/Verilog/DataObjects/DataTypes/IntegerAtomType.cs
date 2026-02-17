using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntegerAtomType : IDataType, IPartSelectableDataType
    {
        protected IntegerAtomType() {
            int? bitWidth = BitWidth;
            if (bitWidth == null) bitWidth = 1;
            PackedDimensions.Add(new Arrays.PackedArray((int)bitWidth - 1, 0));
        }
        public required virtual DataTypeEnum Type { get; init; }
        public bool Packable { get { return true; } }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public virtual bool Signed { get; protected set; }

        // data_type            ::=   integer_atom_type[signing]
        //                          | ...
        // integer_atom_type    ::=   "byte" | "shortint" | "int" | "longint" | "integer" | "time"
        // signing              ::=   "signed" | "unsigned"
        public virtual bool PartSelectable { get { return true; } }
        public virtual bool IsValidForNet { get { return false; } }

        public int? BitWidth
        {
            get
            {
                int size = 0;
                switch (Type)
                {
                    case DataTypeEnum.Byte:
                        size = 8;
                        break;
                    case DataTypeEnum.Shortint:
                        size = 16;
                        break;
                    case DataTypeEnum.Int:
                        size = 32;
                        break;
                    case DataTypeEnum.Longint:
                        size = 64;
                        break;
                    case DataTypeEnum.Integer:
                        size = 32;
                        break;
                    case DataTypeEnum.Time:
                        size = 64;
                        break;
                    default:
                        return null;
                }

                //foreach (Arrays.PackedArray array in PackedDimensions)
                //{
                //    if (array.Size == null) return null;
                //    size = size * (int)array.Size;
                //}
                return size;
            }
        }

        public static IntegerAtomType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "byte":
                    return parse(word,nameSpace,DataTypeEnum.Byte);
                case "shortint":
                    return parse(word, nameSpace, DataTypeEnum.Shortint);
                case "int":
                    return parse(word, nameSpace, DataTypeEnum.Int);
                case "longint":
                    return parse(word, nameSpace, DataTypeEnum.Longint);
                case "integer":
                    return parse(word, nameSpace, DataTypeEnum.Integer);
                case "time":
                    return parse(word, nameSpace, DataTypeEnum.Time);
                default:
                    return null;
            }
        }

        public bool IsVector { get { return false; } }

        public void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(CreateString() + " ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public virtual string CreateString()
        {
            StringBuilder sb = new StringBuilder();

            switch (Type)
            {
                case DataTypeEnum.Byte:
                    sb.Append("byte");
                    break;
                case DataTypeEnum.Shortint:
                    sb.Append("shortint");
                    break;
                case DataTypeEnum.Int:
                    sb.Append("int");
                    break;
                case DataTypeEnum.Longint:
                    sb.Append("longint");
                    break;
                case DataTypeEnum.Integer:
                    sb.Append("integer");
                    break;
                case DataTypeEnum.Time:
                    sb.Append("time");
                    break;
            }

            return sb.ToString();
        }

        protected static IntegerAtomType Create(DataTypeEnum dataType,bool signed)
        {
            switch (dataType)
            {
                case DataTypeEnum.Byte:
                    return ByteType.Create(signed);
                case DataTypeEnum.Shortint:
                    return ShortIntType.Create(signed);
                case DataTypeEnum.Int:
                    return IntType.Create(signed);
                case DataTypeEnum.Longint:
                    return LongIntType.Create(signed);
                case DataTypeEnum.Integer:
                    return IntegerType.Create(signed);
                case DataTypeEnum.Time:
                    return TimeType.Create(signed);
                default:
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                    throw new Exception();
            }
            //IntegerAtomType integerAtomType = new IntegerAtomType() { Type = dataType };
            //integerAtomType.Signed = signed;
            //int? bitWidth = integerAtomType.BitWidth;
            //if (bitWidth == null) bitWidth = 1;
            //integerAtomType.PackedDimensions.Add(new Arrays.PackedArray((int)bitWidth - 1,0));
            //return integerAtomType;
        }

        public IDataType Clone()
        {
            IDataType dataType = IntegerAtomType.Create(Type, Signed);
            foreach(var packedArray in PackedDimensions)
            {
                dataType.PackedDimensions.Add(packedArray.Clone());
            }
            return dataType;
        }

        protected static IntegerAtomType? parse(WordScanner word, NameSpace nameSpace, DataTypeEnum dataType)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            IntegerAtomType integerAtomType = new IntegerAtomType() { Type = dataType };

            integerAtomType.Signed = false;

            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }
            if (word.Text == "signed")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                integerAtomType.Signed = true;
            }else if (word.Text == "unsigned")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                integerAtomType.Signed = false;
            }


            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }

            return integerAtomType;
        }

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
                return DataObjects.DataTypes.LogicType.Create(false, packedDimensions);
            }
            else
            {
                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(rangeExpression.BitWidth));
                return DataObjects.DataTypes.LogicType.Create(false, packedDimensions);
            }
        }
    }
}
