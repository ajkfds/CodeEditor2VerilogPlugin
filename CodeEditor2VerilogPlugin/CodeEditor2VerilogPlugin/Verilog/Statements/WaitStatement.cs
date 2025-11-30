using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class WaitStatement : IStatement
    {
        public void DisposeSubReference()
        {
        }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public IStatement? Statement { get; protected set; }
        public Expressions.Expression? Expression {  get; protected set; }
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
        /*
        wait_statement ::=
            "wait" "(" expression ")" statement_or_null
            | "wait fork" ";"
            | "wait_order" "(" hierarchical_identifier { "," hierarchical_identifier } ")" action_block
        */
        /*
        action_block ::=
              statement_or_null
            | [ statement ] "else" statement_or_null         
         */
        public static async Task<WaitStatement?> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if(word.Text =="wait_order") return parseCreate_wait_fork(word, nameSpace);

            if (word.Text != "wait") throw new Exception();

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            if (word.Text == "fork")
            {
                word.MoveNext();
                if (word.Text != ";")
                {
                    word.AddError("expecting ;");
                    return null;
                }
                word.MoveNext();
                return new WaitStatement();
            }
            if (word.Text != "(")
            {
                word.AddError("expecting (");
                return null;
            }
            word.MoveNext();

            WaitStatement waitStatement = new WaitStatement();

            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (expression == null) return null;

            waitStatement.Expression = expression;
            if (word.Text != ")")
            {
                word.AddError("expecting )");
                return null;
            }
            word.MoveNext();

            if (word.Text == ";")
            {
                word.MoveNext();
                return waitStatement;
            }

            IStatement? statement = await Statements.ParseCreateStatement(word, nameSpace);
            waitStatement.Statement = statement;

            if (word.Text != ";")
            {
                word.AddError("expected ;");
            }
            else
            {
                word.MoveNext();
            }
            return waitStatement;
        }

        public static WaitStatement? parseCreate_wait_fork(WordScanner word, NameSpace nameSpace)
        {
            // | "wait fork" ";"
            if (word.Text != "wait_fork") throw new Exception(); ;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != ";")
            {
                word.AddError("expecting ;");
                return null;
            }
            word.MoveNext();
            return new WaitStatement();
        }

    }
    }
