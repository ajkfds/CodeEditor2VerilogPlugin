using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog.Expressions
{
    public class DataTypeReference : Primary
    {
        public IDataType? IDataType { get; set; } = null;
    }
}
