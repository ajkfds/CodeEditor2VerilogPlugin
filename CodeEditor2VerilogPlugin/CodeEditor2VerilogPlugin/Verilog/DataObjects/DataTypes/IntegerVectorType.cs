using DynamicData;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntegerVectorType : IDataType
    {
        public required virtual DataTypeEnum Type { get; init; }

        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Register; } }

        public bool Packable
        {
            get
            {
                return true;
            }
        }
        public virtual List<Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();
        public virtual bool Signed { get; init; }

        //      data_type::= integer_vector_type[signing] { packed_dimension }
        //                   ...
        //      integer_vector_type::= "bit" | "logic" | "reg"


        // reg          4state  >=1bit      
        // logic        4state  >=1bit      
        // bit          2state  >=1bit      

        public int? BitWidth {
            get 
            {
                int size = 0;
                switch (Type)
                {
                    case DataTypeEnum.Bit:
                        size = 1;
                        break;
                    case DataTypeEnum.Logic:
                        size = 1;
                        break;
                    case DataTypeEnum.Reg:
                        size = 1;
                        break;
                    default:
                        return null;
                }

                foreach(Arrays.PackedArray array in PackedDimensions)
                {
                    if (array.Size == null) return null;
                    size = size * (int)array.Size;
                }
                return size;
            } 
        }
        public bool IsVector { get { return true; } }

        public static IntegerVectorType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            switch (word.Text)
            {
                case "bit":
                    return parse(word, nameSpace, DataTypeEnum.Bit);
                case "logic":
                    return parse(word, nameSpace, DataTypeEnum.Logic);
                case "reg":
                    return parse(word, nameSpace, DataTypeEnum.Reg);
                default:
                    return null;
            }
        }
        public void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(CreateString() + " ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }

        public virtual string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            switch (Type)
            {
                case DataTypeEnum.Bit:
                    sb.Append("bit");
                    break;
                case DataTypeEnum.Logic:
                    sb.Append("logic");
                    break;
                case DataTypeEnum.Reg:
                    sb.Append("reg");
                    break;
            }
            foreach(Arrays.PackedArray range in PackedDimensions)
            {
                sb.Append(" " + range.CreateString());
            }
            return sb.ToString();
        }


        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach(var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return IntegerVectorType.Create(Type, Signed, array);
        }
        protected static IntegerVectorType Create(DataTypeEnum dataType, bool signed,List<Arrays.PackedArray>? packedDimensions)
        {
            switch (dataType)
            {
                case DataTypeEnum.Bit:
                case DataTypeEnum.Logic:
                case DataTypeEnum.Reg:
                    break;
                default:
                    if(System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                    throw new Exception();
            }
            IntegerVectorType integerVectorType = new IntegerVectorType() { Type = dataType,Signed = signed };
            if(packedDimensions == null)
            {
                integerVectorType.PackedDimensions.Clear();
            }
            else
            {
                integerVectorType.PackedDimensions = packedDimensions;
            }
            return integerVectorType;
        }

        public static IntegerVectorType? parse(WordScanner word,NameSpace nameSpace,DataTypeEnum dataType)
        {

            bool signed = false;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            if(dataType == DataTypeEnum.Bit | dataType == DataTypeEnum.Logic)
            {
                word.AddSystemVerilogError();
            }
            word.MoveNext();


            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }
            if (word.Text == "signed")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                signed = true;
            }
            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }
            var integerVectorType = new IntegerVectorType() { Type = dataType,Signed = signed };

            while (word.GetCharAt(0) == '[')
            {
                PackedArray? range = PackedArray.ParseCreate(word, nameSpace);
                if (word.Eof || range == null)
                {
                    word.AddError("illegal reg declaration");
                    return null;
                }

                PackedArray? packedArray = range as Arrays.PackedArray;

                if(packedArray != null)
                {
                    integerVectorType.PackedDimensions.Add(packedArray);
                }
            }
            return integerVectorType;
        }

    }
}
