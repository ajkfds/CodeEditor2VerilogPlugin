using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class DynamicArray : DataObject, IArray
    {
        protected DynamicArray() { }

        public required DataObject DataObject { init; get; }
        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;

        public int? Size => null;

        public bool Constant
        {
            get
            {
                return false;
            }
        }

        public static DynamicArray Create(DataObject dataObject)
        {
            return new DynamicArray() { DataObject = dataObject, Name = dataObject.Name };
        }
        public override DataObject Clone()
        {
            return new DynamicArray() { DataObject = DataObject,Name = DataObject.Name };
        }

        public override DataObject Clone(string name)
        {
            return new DynamicArray() { DataObject = DataObject.Clone(name), Name = name };
        }
    }
}
