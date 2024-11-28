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

        public virtual List<Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();
        public virtual bool Signed { get; protected set; }

        //      data_type::= integer_vector_type[signing] { packed_dimension }
        //                   ...
        //      integer_vector_type::= "bit" | "logic" | "reg"

        // reg          4state  >=1bit      
        // logic        4state  >=1bit      
        // bit          2state  >=1bit      

        public virtual int? BitWidth {
            get 
            {
                if (PackedDimensions.Count == 0) return 0;
                return PackedDimensions[0].Size;
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


        public static IntegerVectorType Create(DataTypeEnum dataType, bool signed,List<Arrays.PackedArray>? packedDimensions)
        {
            IntegerVectorType integerVectorType = new IntegerVectorType() { Type = dataType };
            integerVectorType.Signed = signed;
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
            var integerVectorType = new IntegerVectorType() { Type = dataType };

            word.Color(CodeDrawStyle.ColorType.Keyword);
            if(dataType == DataTypeEnum.Bit | dataType == DataTypeEnum.Logic)
            {
                word.AddSystemVerilogError();
            }
            word.MoveNext();

            integerVectorType.Signed = false;

            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }
            if (word.Text == "signed")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                integerVectorType.Signed = true;
            }
            if (word.Eof)
            {
                word.AddError("illegal reg declaration");
                return null;
            }


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
