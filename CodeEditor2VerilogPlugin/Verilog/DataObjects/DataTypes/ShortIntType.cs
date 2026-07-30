namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class ShortIntType : IntegerAtomType
    {
        protected ShortIntType() { }
        public static ShortIntType Create(bool signed)
        {
            return new ShortIntType() { Type = DataTypeEnum.Shortint, Signed = signed };
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
