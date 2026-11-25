using pluginVerilog.Verilog.DataObjects.DataTypes;
using SystemVerilogCore.Documents;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class Realtime : Variable
    {
        protected Realtime() { }

        public static new Realtime Create(string name, IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.Realtime);

            Realtime val = new Realtime() { Name = name };
            val.DataType = dataType;
            return val;
        }

        public override Variable Clone(string name)
        {
            Realtime val = new Realtime() { Name = name, Defined = Defined };
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
            label.AppendText("realtime ", VerilogColorType.Keyword);
            label.AppendText(" ");
        }

    }
}
