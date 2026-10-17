
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    /// <summary>
    /// Variable whose data type is an interface class
    /// ("ihello iobj;" / "itest it_ref;").
    /// Member resolution (obj.hello()) follows the interface class's
    /// NamedElements via the lazy getter (same pattern as UserDefinedVariable /
    /// Variables.Object).
    /// </summary>
    public class InterfaceClassVariable : Variable
    {
        protected InterfaceClassVariable() { }

        public static new InterfaceClassVariable Create(string name, IDataType dataType)
        {
            if (dataType.Type != DataTypeEnum.InterfaceClass) throw new Exception();

            InterfaceClassVariable val = new InterfaceClassVariable()
            {
                Name = name
            };
            val.DataType = dataType;
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

        public override Variable Clone(string name)
        {
            InterfaceClassVariable val = new InterfaceClassVariable()
            {
                Name = name,
                Defined = Defined
            };
            if (DataType != null) val.DataType = DataType.Clone();
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
    }
}
