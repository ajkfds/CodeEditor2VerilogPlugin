namespace pluginVerilog.Verilog.DataObjects.Constants
{
    public class Specparam : Constants
    {
        public override DataObject Clone(string name)
        {
            return new Specparam { DefinedReference = DefinedReference, Expression = Expression, Name = name, Defined = Defined, DataType = DataType?.Clone() };
        }
    }
}
