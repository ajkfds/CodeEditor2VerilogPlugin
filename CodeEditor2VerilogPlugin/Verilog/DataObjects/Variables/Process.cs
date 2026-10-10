using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    // SystemVerilog built-in process class variable (IEEE 1800-2017 section 9.7)
    public class Process : Variable
    {
        protected Process() { }

        public override Process Clone()
        {
            return Clone(Name);
        }

        public override Process Clone(string name)
        {
            Process val = new Process() { Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataType = DataType;
            return val;
        }

        public static new Process Create(string name, DataTypes.IDataType dataType)
        {
            DataTypes.ProcessType? dType = dataType as DataTypes.ProcessType;
            if (dType == null) throw new Exception();

            Process val = new Process() { Name = name };
            val.DataType = dType;
            return val;
        }

        // built-in methods are lazily resolved via DataType.AppendChiledNamedElements
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

    }
}
