namespace pluginVerilog.Verilog.Expressions
{
    public class Time : Primary
    {
        public Time() { }

        public required Number Number { get; set; }
        public required UnitEnum Unit { get; set; }
        public enum UnitEnum
        {
            s, ms, us, ns, ps, fs
        }
    }
}
