using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Struct : Variable
    {
        protected Struct() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public required StructType StructType { get; init; }
        public new static Struct Create(string name, IDataType dataType)
        {
            StructType structType = (StructType)dataType;

            Struct ret = new Struct() { StructType = structType, Name = name };
            foreach (var member in structType.Members.Values)
            {
                var dataObject = DataObject.Create(member.Identifier, member.DatType);
                ret.NamedElements.Add(dataObject.Name, dataObject);
            }
            return ret;
        }

        public override Variable Clone()
        {
            Struct ret = new Struct() { StructType = StructType, Name = Name };
            return ret;
        }

//        private Dictionary<string, DataObject> dataObjects = new Dictionary<string, DataObject>();

        //public override DataObject? GetDataObject(string identifier)
        //{
        //    if (!StructType.Members.ContainsKey(identifier)) return null;
        //    if (dataObjects.ContainsKey(identifier)) return dataObjects[identifier];
        //    StructType.Member member = StructType.Members[identifier];

        //    DataObject obj = DataObject.Create(identifier, member.DatType);
        //    dataObjects.Add(identifier, obj);
        //    return obj;
        //}
        //public override void AppendAutoCompleteItem(List<CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem> items)
        //{
        //}


    }
}
