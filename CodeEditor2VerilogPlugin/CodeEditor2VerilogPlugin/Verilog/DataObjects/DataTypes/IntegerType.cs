namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class IntegerType : IntegerAtomType
    {
        protected IntegerType() { }
        public static IntegerType Create(bool signed)
        {
            return new IntegerType() { Type = DataTypeEnum.Integer, Signed = signed };
        }
        public override bool IsValidForNet
        {
            get
            {
                foreach (var array in PackedDimensions)
                {
                    if (!array.IsValidForNet) return false;
                }
                return true;
            }
        }

    }
}
