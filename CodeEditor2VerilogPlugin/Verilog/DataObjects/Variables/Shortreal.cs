using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Shortreal : ValueVariable
    {
        protected Shortreal() { }

        public static new Shortreal Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Shortreal);

            Shortreal val = new Shortreal() { Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone(string name)
        {
            Shortreal val = new Shortreal() { Name = name, Defined = Defined };
            val.DataType = DataType;
            foreach (var unpackedArray in UnpackedArrays)
            {
                val.UnpackedArrays.Add(unpackedArray.Clone());
            }
            return val;
        }
        public override Variable Clone()
        {
            return Clone(Name);
        }

        public override void AppendTypeLabel(SystemVerilogCore.Documents.VerilogLabel label)
        {
            label.AppendText("shortreal ", CodeDrawStyle.ColorType.Keyword);
            label.AppendText(" ");
        }

    }
}
