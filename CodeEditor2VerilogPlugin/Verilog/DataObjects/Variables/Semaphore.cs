using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    // SystemVerilog built-in semaphore variable (IEEE 1800-2017 section 20.4)
    public class Semaphore : Variable
    {
        protected Semaphore() { }

        public override Semaphore Clone()
        {
            return Clone(Name);
        }

        public override Semaphore Clone(string name)
        {
            Semaphore val = new Semaphore() { Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataType = DataType;
            return val;
        }

        public static new Semaphore Create(string name, DataTypes.IDataType dataType)
        {
            DataTypes.SemaphoreType? dType = dataType as DataTypes.SemaphoreType;
            if (dType == null) throw new Exception();

            Semaphore val = new Semaphore() { Name = name };
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
