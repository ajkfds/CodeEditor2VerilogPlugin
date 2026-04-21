namespace pluginVerilog.Verilog
{
    public interface INamedElement
    {
        public string Name { get; }
        public CodeDrawStyle.ColorType ColorType { get; }

        public NamedElements NamedElements { get; }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem();

    }
}
