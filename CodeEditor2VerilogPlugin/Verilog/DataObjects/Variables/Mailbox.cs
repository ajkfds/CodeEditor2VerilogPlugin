using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    // SystemVerilog built-in mailbox variable (IEEE 1800-2017 section 15.4)
    public class Mailbox : Variable
    {
        protected Mailbox() { }

        public override Mailbox Clone()
        {
            return Clone(Name);
        }

        public override Mailbox Clone(string name)
        {
            Mailbox val = new Mailbox() { Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            val.DataType = DataType;
            return val;
        }

        public static new Mailbox Create(string name, DataTypes.IDataType dataType)
        {
            DataTypes.MailboxType? dType = dataType as DataTypes.MailboxType;
            if (dType == null) throw new Exception();

            Mailbox val = new Mailbox() { Name = name };
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
