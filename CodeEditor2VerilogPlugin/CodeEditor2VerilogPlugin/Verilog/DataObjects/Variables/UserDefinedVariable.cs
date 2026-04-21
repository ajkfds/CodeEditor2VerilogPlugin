using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;

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

            UserDefinedVariable val = new UserDefinedVariable() { Name = name, UserDefinedType = userDefinedType };
            IDataType originalType = userDefinedType.OriginalDataType;

            val.DataType = dataType;
            return val;
        }

        public override Variable Clone(string name)
        {
            UserDefinedVariable val = new UserDefinedVariable() { Name = name, Defined = Defined, UserDefinedType = (UserDefinedType)UserDefinedType.Clone() };

            if (DataType != null) val.DataType = DataType.Clone();
            if (DataType is not DataObjects.DataTypes.UserDefinedType) throw new Exception();
            UserDefinedType userDefinedType = (UserDefinedType)DataType;
            IDataType originalType = userDefinedType.OriginalDataType;
            val.Defined = Defined;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }

            return val;
        }

        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;
                namedElements = new NamedElements();

                if (DataType == null) return namedElements;
                DataType.AppendChiledNamedElements(namedElements);
                return namedElements;
            }
        }

        public override Variable Clone()
        {
            return Clone(Name);
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

        public override int? BitWidth => UserDefinedType.BitWidth;

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
