namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public interface IPartSelectableDataType
    {
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace);
    }
}
