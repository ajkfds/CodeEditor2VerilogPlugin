using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class CoverSequenceStatement : Statements.IStatement
    {
        protected CoverSequenceStatement() { }

        /*
        cover_sequence_statement ::=
            "cover" "sequence" "(" [ clocking_event ] [ "disable" "iff" "(" expression_or_dist ")" ] sequence_expr ")" statement_or_null

        clocking_event ::=
              @ identifier
            | @ ( event_expression )

        event_expression ::=
            [ edge_identifier ] expression [ iff expression ]
            | sequence_instance [ iff expression ]
            | event_expression or event_expression
            | event_expression , event_expression
            | ( event_expression )
        */

        public static CoverSequenceStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "cover" || word.NextText != "sequence")
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                throw new Exception();
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // cover

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // sequence

            CoverSequenceStatement coverSequenceStatement = new CoverSequenceStatement();

            if (word.Eof || word.Text != "(")
            {
                return ExitTask(word, nameSpace, coverSequenceStatement);
            }
            word.MoveNext();

            // Parse optional clocking_event
            if (word.Text == "@")
            {
                coverSequenceStatement.EventControl = Statements.EventControl.ParseCreate(word, nameSpace);
            }

            // Parse optional disable iff
            if (word.Text == "disable")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text != "iff")
                {
                    word.AddError("iff missing");
                }
                else
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("illegal cover sequence statement");
                    }

                    coverSequenceStatement.DisableIffExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("illegal cover sequence statement");
                    }
                }
            }

            // Parse sequence expression
            coverSequenceStatement.SequenceExpr = Sequence.SequenceExpr.ParseCreate(word, nameSpace);
            if (coverSequenceStatement.SequenceExpr == null)
            {
                word.AddError("illegal sequence expression");
            }

            if (word.Eof || word.Text != ")")
            {
                return ExitTask(word, nameSpace, coverSequenceStatement);
            }
            word.MoveNext();

            // Parse statement_or_null (no action_block, just a single statement or null)
            coverSequenceStatement.CoverStatement = Statements.Statements.ParseCreateStatementOrNull(word, nameSpace);

            return coverSequenceStatement;
        }

        private static CoverSequenceStatement ExitTask(WordScanner word, NameSpace nameSpace, CoverSequenceStatement statement)
        {
            word.AddError("illegal cover sequence statement");
            word.SkipToKeyword(";");
            if (word.Text == ";") word.MoveNext();
            return statement;
        }

        public Statements.EventControl? EventControl { get; set; }
        public Expressions.Expression? DisableIffExpression { get; set; }
        public Sequence.SequenceExpr? SequenceExpr { get; set; }
        public Statements.IStatement? CoverStatement { get; set; }

        public string Name { get; set; } = "";

        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Keyword;

        public NamedElements NamedElements => new NamedElements();

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return null;
        }

        public void DisposeSubReference()
        {
        }
    }
}
