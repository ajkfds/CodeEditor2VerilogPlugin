using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class WaitStatement : IStatement
    {
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public void DisposeSubReference()
        {
            Expression?.DisposeSubReference(true);
            Statement?.DisposeSubReference();
            ElseStatement?.DisposeSubReference();
        }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public IStatement? Statement { get; protected set; }
        public IStatement? ElseStatement { get; protected set; }
        public Expressions.Expression? Expression { get; protected set; }
        public List<string>? Identifiers { get; protected set; }
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
        public static WaitStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text == "wait_order") return parseCreate_wait_order(word, nameSpace);

            if (word.Text != "wait") throw new Exception();

            IndexReference beginIndexReference = word.CreateIndexReference();
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
                return new WaitStatement() { BeginIndexReference = beginIndexReference, LastIndexReference = word.CreateIndexReferenceBefore() };
            }
            if (word.Text != "(")
            {
                word.AddError("expecting (");
                return null;
            }
            word.MoveNext();

            WaitStatement waitStatement = new WaitStatement() { BeginIndexReference = beginIndexReference };

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

            IStatement? statement = Statements.ParseCreateStatement(word, nameSpace);
            waitStatement.Statement = statement;

            // the statement ends with the sub-statement: adopt its region end
            StatementRegionUtility.SetLastIndexReference(waitStatement, statement, word);

            if (statement == null)
            {
                // sub-statement parse failed: recover to ";" for error recovery
                if (word.Text != ";")
                {
                    word.AddError("expected ;");
                }
                else
                {
                    word.MoveNext();
                }
            }
            // when a sub-statement exists, it consumes its own ";" (e.g. procedural_timing_control_statement)
            return waitStatement;
        }

        public static WaitStatement? parseCreate_wait_fork(WordScanner word, NameSpace nameSpace)
        {
            // | "wait fork" ";"
            if (word.Text != "wait_fork") throw new Exception(); ;
            IndexReference beginIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != ";")
            {
                word.AddError("expecting ;");
                return null;
            }
            word.MoveNext();
            return new WaitStatement() { BeginIndexReference = beginIndexReference, LastIndexReference = word.CreateIndexReferenceBefore() };
        }

        // wait_order "(" hierarchical_identifier { "," hierarchical_identifier } ")" action_block
        public static WaitStatement? parseCreate_wait_order(WordScanner word, NameSpace nameSpace)
        {
            // | "wait_order" "(" hierarchical_identifier { "," hierarchical_identifier } ")" action_block
            if (word.Text != "wait_order") throw new Exception();
            IndexReference beginIndexReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "(")
            {
                word.AddError("expecting (");
                return null;
            }
            word.MoveNext();

            WaitStatement waitOrderStatement = new WaitStatement() { BeginIndexReference = beginIndexReference };
            waitOrderStatement.Identifiers = new List<string>();

            // Parse first hierarchical_identifier
            if (word.Text.Length == 0 || (!General.IsIdentifier(word.Text) && !General.IsEscapedIdentifier(word.Text)))
            {
                word.AddError("expecting hierarchical_identifier");
                return null;
            }
            waitOrderStatement.Identifiers.Add(word.Text);
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse remaining hierarchical_identifiers (comma separated)
            while (word.Text == ",")
            {
                word.MoveNext();

                if (word.Text.Length == 0 || (!General.IsIdentifier(word.Text) && !General.IsEscapedIdentifier(word.Text)))
                {
                    word.AddError("expecting hierarchical_identifier");
                    return null;
                }
                waitOrderStatement.Identifiers.Add(word.Text);
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            if (word.Text != ")")
            {
                word.AddError("expecting )");
                return null;
            }
            word.MoveNext();

            // action_block ::= [ statement ] [ else statement ]
            IStatement? statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
            waitOrderStatement.Statement = statement;

            // Handle else clause
            if (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // else

                IStatement? elseStatement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                waitOrderStatement.ElseStatement = elseStatement;
                statement = elseStatement ?? statement;
            }

            // the statement ends with the (last) sub-statement: adopt its region end
            StatementRegionUtility.SetLastIndexReference(waitOrderStatement, statement, word);

            return waitOrderStatement;
        }
    }
}
