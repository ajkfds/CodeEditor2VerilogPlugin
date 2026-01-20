using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Struct : Variable,IPackedDataObject
    {
        protected Struct() { }
        public override CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public required StructType StructType { get; init; }
        public List<DataObjects.Arrays.PackedArray> PackedDimensions
        {
            get
            {
                if(StructType == null) return new List<DataObjects.Arrays.PackedArray>();
                return StructType.PackedDimensions;
            }
        }

        public new static Struct Create(string name, IDataType dataType)
        {
            StructType structType = (StructType)dataType;

            Struct ret = new Struct() { StructType = structType, Name = name };
            foreach (var member in structType.Members.Values)
            {
                var dataObject = DataObject.Create(member.Identifier, member.DatType);
                dataObject.Defined = true;
                ret.NamedElements.Add(dataObject.Name, dataObject);
            }
            return ret;
        }

        public override Variable Clone()
        {
            Struct ret = new Struct() { StructType = StructType, Name = Name, Defined = Defined };
            //foreach (var packedArray in PackedDimensions)
            //{
            //    ret.PackedDimensions.Add(packedArray.Clone());
            //}
            foreach (var unpackedArray in UnpackedArrays)
            {
                ret.UnpackedArrays.Add(unpackedArray.Clone());
            }
            foreach (var member in StructType.Members.Values)
            {
                var dataObject = DataObject.Create(member.Identifier, member.DatType);
                dataObject.Defined = true;
                ret.NamedElements.Add(dataObject.Name, dataObject);
            }
            return ret;
        }
        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            if (StructType == null) return;
            StructType.AppendTypeLabel(label);
            //label.AppendText("struct ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            //label.AppendText( StructType.na, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }



    }
}
