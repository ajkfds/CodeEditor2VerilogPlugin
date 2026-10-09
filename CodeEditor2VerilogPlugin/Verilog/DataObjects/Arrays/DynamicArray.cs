using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class DynamicArray : DataObject, IArray
    {
        protected DynamicArray() { }

        public bool IsValidForNet { get { return false; } }
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

        public override NamedElements NamedElements { get; } = new NamedElements();
        public static DynamicArray Create(DataObject dataObject)
        {
            return Create(dataObject, dataObject.Name);
        }

        public static DynamicArray Create(DataObject dataObject,string name)
        {
            DynamicArray dynamicArray = new DynamicArray() { DataObject = dataObject, Name = name };

            ArrayMethods.AddSizeOrNum(dynamicArray.NamedElements, "size");          // function int size();
            ArrayMethods.AddDelete(dynamicArray.NamedElements);                     // function void delete([input integer index]);
            ArrayMethods.AppendArrayLocatorMethods(dynamicArray.NamedElements, dataObject);  // section 7.12

            return dynamicArray;
        }

        public override DataObject Clone()
        {
            DynamicArray clone = Create(DataObject.Clone());
            // Defined フラグをコピーする (AssociativeArray と同じ理由で必要)
            clone.Defined = this.Defined;
            return clone;
        }

        public override DataObject Clone(string name)
        {
            DynamicArray clone = DynamicArray.Create(DataObject.Clone(name),name);
            clone.Defined = this.Defined;
            return clone;
        }
    }
}
