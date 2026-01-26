using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class UserDefinedVariable : ValueVariable, IPackedDataObject, IPartSelectableDataObject
    {
        protected UserDefinedVariable() { }

        public required UserDefinedType UserDefinedType { get; init; }
        public List<PackedArray> PackedDimensions
        {
            get
            {
                if (DataType == null) return new List<PackedArray>();
                return DataType.PackedDimensions;
            }
        }

        public new static UserDefinedVariable Create(string name, IDataType dataType)
        {
            if (dataType.Type != DataTypeEnum.UserDefined) throw new Exception();
            UserDefinedType userDefinedType = (UserDefinedType)dataType;

            UserDefinedVariable val = new UserDefinedVariable() { Name = name,UserDefinedType = userDefinedType };
            IDataType originalType = userDefinedType.OriginalDataType;

            switch (originalType.Type)
            {
                case DataTypeEnum.Struct:
                    StructType structType = (StructType)originalType;
                    foreach (var member in structType.Members.Values)
                    {
                        var dataObject = DataObject.Create(member.Identifier, member.DatType);
                        dataObject.Defined = true;
                        val.NamedElements.Add(dataObject.Name, dataObject);
                    }
                    break;
                case DataTypeEnum.UserDefined:
                case DataTypeEnum.Enum:
                case DataTypeEnum.Class:
                    break;
            }

            val.DataType = dataType;
            return val;
        }

        public override Variable Clone()
        {
            UserDefinedVariable val = new UserDefinedVariable() { Name = Name, Defined = Defined, UserDefinedType = (UserDefinedType)UserDefinedType.Clone() };

            if (DataType != null) val.DataType = DataType.Clone();
            if (DataType is not DataObjects.DataTypes.UserDefinedType) throw new Exception();
            UserDefinedType userDefinedType = (UserDefinedType)DataType;
            IDataType originalType = userDefinedType.OriginalDataType;
            val.Defined = Defined;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            switch (originalType.Type)
            {
                case DataTypeEnum.Struct:
                    StructType structType = (StructType)originalType;
                    foreach (var member in structType.Members.Values)
                    {
                        var dataObject = DataObject.Create(member.Identifier, member.DatType);
                        dataObject.Defined = true;
                        val.NamedElements.Add(dataObject.Name, dataObject);
                    }
                    break;
                case DataTypeEnum.UserDefined:
                case DataTypeEnum.Enum:
                case DataTypeEnum.Class:
                    break;
            }

            return val;
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            UserDefinedType.AppendTypeLabel(label);
        }
        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            base.AppendLabel(label);
            UserDefinedType.Typedef.AppendLabel(label);
        }

        public override bool PartSelectable
        {
            get { return UserDefinedType.PartSelectable; }
        }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = DataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }


    }
}
