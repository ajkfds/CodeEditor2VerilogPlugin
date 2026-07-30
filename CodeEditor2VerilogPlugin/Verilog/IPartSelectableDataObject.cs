using pluginVerilog.Verilog.DataObjects.DataTypes;

namespace pluginVerilog.Verilog
{
    public interface IPartSelectableDataObject
    {
        public bool PartSelectable { get; }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace);
    }
}
