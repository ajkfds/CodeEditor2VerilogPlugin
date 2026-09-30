using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Expressions;
using System;

namespace pluginVerilog.Verilog.Statements
{
    public class VoidFunctionCall : IStatement
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
        }

        public FunctionCall? FunctionCall { get; private set; } = null!; // Initialized in Create method
        public static VoidFunctionCall Create(FunctionCall functionCall, IndexReference? beginIndexReference = null)
        {
            VoidFunctionCall voidFunctionCall = new VoidFunctionCall() { BeginIndexReference = beginIndexReference! };
            voidFunctionCall.FunctionCall = functionCall;
            return voidFunctionCall;
        }
        public static VoidFunctionCall? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "void") throw new Exception();
            VoidFunctionCall voidFunctionCall = new VoidFunctionCall() { BeginIndexReference = word.CreateIndexReference() };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (word.Text != "'") throw new Exception();
            word.MoveNext();
            if (word.Eof || word.Text != "(")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            FunctionCall? func = FunctionCall.ParseCreate(word, nameSpace, nameSpace);
            voidFunctionCall.FunctionCall = func;

            if (word.Eof || func == null || word.Text != ")")
            {
                word.AddError("illegal cast");
                return null;
            }
            word.MoveNext();

            voidFunctionCall.LastIndexReference = word.CreateIndexReferenceBefore();
            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }

            return voidFunctionCall;
        }
    }
}
