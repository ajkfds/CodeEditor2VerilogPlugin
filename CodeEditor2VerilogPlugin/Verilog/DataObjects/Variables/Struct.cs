using pluginVerilog.Verilog.DataObjects.DataTypes;
using System.Collections.Generic;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Struct : Variable, IPackedDataObject, IPartSelectableDataType
    {
        protected Struct() { }
        public override VerilogColorType ColorType { get { return VerilogColorType.Variable; } }
        public required StructType StructType { get; init; }
        public List<DataObjects.Arrays.PackedArray> PackedDimensions
        {
            get
            {
                if (StructType == null) return new List<DataObjects.Arrays.PackedArray>();
                return StructType.PackedDimensions;
            }
        }

        public new static Struct Create(string name, IDataType dataType)
        {
            StructType structType = (StructType)dataType;

            // Set DataType to the underlying StructType so that checks like
            // "DataType is StructType" work correctly when the variable is
            // declared with a typedef (UserDefinedType wrapping StructType)
            Struct ret = new Struct() { StructType = structType, Name = name, DataType = structType };
            return ret;
        }

        public override Variable Clone(string name)
        {
            Struct ret = new Struct() { StructType = StructType, Name = name, Defined = Defined };
            foreach (var unpackedArray in UnpackedArrays)
            {
                ret.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return ret;
        }

        // アクセス時に遅延生成する
        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;
                namedElements = new NamedElements();

                foreach (var member in StructType.Members.Values)
                {
                    var dataObject = DataObject.Create(member.Identifier, member.DatType);
                    dataObject.Defined = true;
                    namedElements.Add(dataObject.Name, dataObject);
                }
                return namedElements;
            }
        }


        public override Variable Clone()
        {
            return Clone(Name);
        }
        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            if (StructType == null) return;
            StructType.AppendTypeLabel(label);
            //label.AppendText("struct ", VerilogColorType.Keyword);
            //label.AppendText( StructType.na, VerilogColorType.Keyword);
        }

        public override bool PartSelectable
        {
            get { return StructType.PartSelectable; }
        }

        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            IPartSelectableDataType? type = DataType as IPartSelectableDataType;
            if (type == null) return null;
            return type.ParsePartSelect(word, nameSpace);
        }


    }
}
