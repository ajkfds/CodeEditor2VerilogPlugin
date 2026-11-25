using SystemVerilogCore.Documents;
namespace pluginVerilog.Verilog
{
    public interface INamedElement
    {
        public string Name { get; }
        public VerilogColorType ColorType { get; }

        public NamedElements NamedElements { get; }

        // NOTE: `CreateAutoCompleteItem()` was removed here.
        //
        // It returned the editor's `CodeEditor2...AutocompleteItem`, which tied
        // every parser element to the editor assembly. Its only caller was
        // ParsedDocument.appendAutoCompleteINamedElements, which nothing called
        // (the live completion path is CompletionContext.appendNamedElements ->
        // appendItemsUpward). Completion candidates are built there directly, so
        // the member was dead code and is gone rather than abstracted.
    }
}
