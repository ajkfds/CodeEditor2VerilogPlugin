using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class UserDefinedType : IDataType, IPartSelectableDataType
    {
        public DataTypeEnum Type { get{ return DataTypeEnum.UserDefined; }}

        public required Typedef Typedef { get; init; }
        public IDataType OriginalDataType { get
            {
                return Typedef.VariableType;
            }
        }
        public virtual bool PartSelectable { get { return OriginalDataType.PartSelectable; } }

        public bool Packable
        {
            get
            {
                return OriginalDataType.Packable;
            }
        }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = OriginalDataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }

        public int? BitWidth
        {
            get
            {
                int? bitWidth = OriginalDataType.BitWidth;
                if (bitWidth == null) return null;
                foreach(var packedArray in PackedDimensions)
                {
                    bitWidth = bitWidth * packedArray.Size;
                }
                return bitWidth;
            }
        }

        public CodeDrawStyle.ColorType ColorType
        {
            get
            {
                return OriginalDataType.ColorType;
            }
        }

        public bool IsVector
        {
            get
            {
                return OriginalDataType.IsVector;
            }
        }

        public List<PackedArray> PackedDimensions { get; init; } = new List<PackedArray>();

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText(Typedef.Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            label.AppendText(" ");
            foreach(var packedArray in PackedDimensions)
            {
                packedArray.AppendLabel(label);
                label.AppendText(" ");
            }
        }

        public static UserDefinedType Create(Typedef typedef, List<Arrays.PackedArray>? packedDimensions)
        {
            UserDefinedType userDefinedType = new UserDefinedType { Typedef = typedef };
            foreach (var packedArray in typedef.VariableType.PackedDimensions)
            {
                userDefinedType.PackedDimensions.Add(packedArray.Clone());
            }
            if(packedDimensions != null)
            {
                foreach (var packedArray in packedDimensions)
                {
                    userDefinedType.PackedDimensions.Add(packedArray.Clone());
                }
            }
            return userDefinedType;

        }
        public IDataType Clone()
        {
            UserDefinedType userDefinedType = new UserDefinedType { Typedef = Typedef };
            foreach(var packedArray in PackedDimensions)
            {
                userDefinedType.PackedDimensions.Add(packedArray.Clone());
            }
            return userDefinedType;
        }

        public string CreateString()
        {
            ColorLabel colorLabel = new ColorLabel();
            AppendTypeLabel(colorLabel);
            return colorLabel.CreateString();
        }


    }
}
