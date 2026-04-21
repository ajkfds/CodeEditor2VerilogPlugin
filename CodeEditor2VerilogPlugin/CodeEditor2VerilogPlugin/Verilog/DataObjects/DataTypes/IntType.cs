namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntType : IntegerAtomType
    {
        protected IntType() { }
        public static IntType Create(bool signed)
        {
            return new IntType() { Type = DataTypeEnum.Int };
        }
        public override bool IsValidForNet { get { return false; } }
    }
}
