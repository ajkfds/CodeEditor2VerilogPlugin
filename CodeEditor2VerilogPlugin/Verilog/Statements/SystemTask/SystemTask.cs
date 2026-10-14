using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.Statements.SystemTask
{
    public class SystemTask : IStatement
    {
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public void DisposeSubReference()
        {
            return;
        }
        public static SystemTask ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SystemTask taskEnable = new SystemTask() { BeginIndexReference = word.CreateIndexReference() };

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                if (word.Text == ")")
                {
                    word.MoveNext();
                    word.AddWarning("remove ()");
                }
                else
                {

                    while (!word.Eof)
                    {
                        // empty actual argument: list_of_arguments allows omitted arguments
                        // (IEEE 1800-2017 22.5.1 / A.2.1 e.g. $display(a,,b,,c);)
                        if (word.Text == ",")
                        {
                            word.MoveNext();
                            continue;
                        }
                        if (word.Text == ")") break;

                        Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expression == null)
                        {
                            word.AddError("missed expression");
                            word.SkipToKeyword(";");
                            if (word.Text == ";")
                            {
                                word.MoveNext();
                            }
                            return null;
                        }
                        if (word.Text == ")")
                        {
                            break;
                        }
                        else if (word.Text == ",")
                        {
                            word.MoveNext();
                            continue;
                        }
                        else
                        {
                            word.AddError("illegal expression");
                            return null;
                        }
                    }
                    if (word.Text == ")") word.MoveNext();
                    else word.AddError(") required");
                }
            }

            taskEnable.LastIndexReference = word.CreateIndexReferenceBefore();
            if (word.Text == ";") word.MoveNext();
            else word.AddError("; required");

            return taskEnable;

        }
    }
}