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

        NamedElements NamedElements { get; } = new NamedElements();
        public static DynamicArray Create(DataObject dataObject)
        {
            return Create(dataObject, dataObject.Name);
        }

        public static DynamicArray Create(DataObject dataObject,string name)
        {
            DynamicArray dynamicArray = new DynamicArray() { DataObject = dataObject, Name = name };

            { // function int size();
                List<Port> ports = new List<Port>();
                Variables.Variable returnVal = DataObjects.Variables.Int.Create("size", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("size", returnVal, ports);
                dynamicArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void delete();
                List<Port> ports = new List<Port>();
                BuiltInMethod builtInMethod = BuiltInMethod.Create("delete", null, ports);
                dynamicArray.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            return dynamicArray;
        }

        public override DataObject Clone()
        {
            DynamicArray clone = Create(DataObject.Clone());
            return clone;
        }

        public override DataObject Clone(string name)
        {
            DynamicArray clone = DynamicArray.Create(DataObject.Clone(name),name);
            return clone;
        }
    }
}
