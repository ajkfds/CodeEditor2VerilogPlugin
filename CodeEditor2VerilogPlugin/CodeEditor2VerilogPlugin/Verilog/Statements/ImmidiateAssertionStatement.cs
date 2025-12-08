using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static pluginVerilog.Verilog.Statements.ConditionalStatement;

namespace pluginVerilog.Verilog.Statements
{
    public class ImmidiateAssertionStatement : IStatement
    {
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            ConditionalExpression.DisposeSubReference(true);
            Statement.DisposeSubReference();
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        public Expressions.Expression? ConditionalExpression;
        public IStatement? Statement;
        public IStatement? ElseStatement;
        public static async Task<ImmidiateAssertionStatement> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            System.Diagnostics.Debug.Assert(word.Text == "assert" | word.Text == "assume" | word.Text == "cover");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // if

            ImmidiateAssertionStatement conditionalStatement = new ImmidiateAssertionStatement() { Name = "" };
            if (statement_label != null) { conditionalStatement.Name = statement_label; }

            if (word.GetCharAt(0) != '(')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext(); // (

            Expressions.Expression conditionExpression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (conditionExpression == null)
            {
                word.AddError("illegal conditional expression");
                return null;
            }
            conditionalStatement.ConditionalExpression = conditionExpression;

            if (word.GetCharAt(0) != ')')
            {
                word.AddError("( expected");
                return null;
            }
            word.MoveNext(); // )

            IStatement? statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
            conditionalStatement.Statement = statement;
            
            while (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // else

                statement = await Statements.ParseCreateStatementOrNull(word, nameSpace);
                conditionalStatement.ElseStatement = statement;
                break;
            }
            return conditionalStatement;
        }
    }
}
