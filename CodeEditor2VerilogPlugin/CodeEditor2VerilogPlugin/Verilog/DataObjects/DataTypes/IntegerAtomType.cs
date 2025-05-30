﻿using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntegerAtomType : IDataType
    {
        public required virtual DataTypeEnum Type { get; init; }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public virtual bool Signed { get; protected set; }

        // data_type            ::=   integer_atom_type[signing]
        //                          | ...
        // integer_atom_type    ::=   "byte" | "shortint" | "int" | "longint" | "integer" | "time"
        // signing              ::=   "signed" | "unsigned"


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

        public virtual int? BitWidth
        {
            get
            {
                switch (Type)
                {
                    case DataTypeEnum.Byte:
                        return 8;
                    case DataTypeEnum.Shortint:
                        return 16;
                    case DataTypeEnum.Int:
                        return 32;
                    case DataTypeEnum.Longint:
                        return 64;
                    case DataTypeEnum.Integer:
                        return 32;
                    case DataTypeEnum.Time:
                        return 64;
                }
                return null;
            }
        }

        public bool IsVector { get { return false; } }

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

        public static IntegerAtomType Create(DataTypeEnum dataType,bool signed)
        {
            IntegerAtomType integerAtomType = new IntegerAtomType() { Type = dataType };
            integerAtomType.Signed = signed;
            return integerAtomType;
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
    }
}
