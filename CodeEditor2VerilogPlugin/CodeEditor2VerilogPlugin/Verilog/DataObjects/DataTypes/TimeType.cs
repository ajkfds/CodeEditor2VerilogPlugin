namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class TimeType : IntegerAtomType
    {
        protected TimeType() { }
        public static TimeType Create(bool signed)
        {
            return new TimeType() { Type = DataTypeEnum.Time, Signed = signed };
        }
        public override bool IsValidForNet
        {
            get
            {
                return true;
            }
        }
    }
}
