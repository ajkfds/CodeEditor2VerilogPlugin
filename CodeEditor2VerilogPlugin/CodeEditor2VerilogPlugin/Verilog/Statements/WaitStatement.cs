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
        public static WaitStatement? ParseCreate(WordScanner word, NameSpace nameSpace)
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
            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (expression == null) return null;
            if (word.Text != ")")
            {
                word.AddError("expecting )");
                return null;
            }
            word.MoveNext();

            IStatement? statement = Statements.ParseCreateStatement(word, nameSpace);

            if (word.Text != ";")
            {
                word.AddError("expected ;");
            }
            else
            {
                word.MoveNext();
            }
            return new WaitStatement();
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
