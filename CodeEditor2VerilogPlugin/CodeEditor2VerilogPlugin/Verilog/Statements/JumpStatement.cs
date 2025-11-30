using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    // jump_statement   ::= "return" [expression] ;
    //                      | "break" ;
    //                      | "continue" ;
    public class ReturnStatement : IStatement
    {
        protected ReturnStatement() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
        }

        public Expressions.Expression Expression;
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        public static ReturnStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "return") System.Diagnostics.Debugger.Break();
            ReturnStatement jumpStatement = new ReturnStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == ";")
            {
                word.MoveNext();
                return jumpStatement;
            }

            jumpStatement.Expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }
            return jumpStatement;
        }
    }

    public class BreakStatement : IStatement
    {
        protected BreakStatement() { }
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

        public static BreakStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "break") System.Diagnostics.Debugger.Break();
            BreakStatement jumpStatement = new BreakStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }
            return jumpStatement;
        }

    }

    public class ContinueStatement : IStatement
    {
        protected ContinueStatement() { }
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

        public static ContinueStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "continue") System.Diagnostics.Debugger.Break();
            ContinueStatement jumpStatement = new ContinueStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }

            return jumpStatement;
        }

    }

}
