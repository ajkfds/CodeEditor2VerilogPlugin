namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ByteType : IntegerAtomType
    {
        protected ByteType() { }
        public static ByteType Create(bool signed)
        {
            return new ByteType() { Type = DataTypeEnum.Byte, Signed = signed };
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
