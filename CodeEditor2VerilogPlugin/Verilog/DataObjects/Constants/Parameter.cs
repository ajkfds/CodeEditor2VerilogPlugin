namespace pluginVerilog.Verilog.DataObjects.Constants
{
    public class Parameter : Constants
    {
        public override DataObject Clone(string name)
        {
            return new Parameter { DefinedReference = DefinedReference, Expression = Expression, Name = name, Defined = Defined, DataType = DataType?.Clone() };
        }
    }
}
