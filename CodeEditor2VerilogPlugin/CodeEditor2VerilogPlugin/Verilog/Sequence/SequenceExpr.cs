using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Sequence
{
    public class SequenceExpr : pluginVerilog.Verilog.Property.PropertyPrimary
    {
        /*
        sequence_expr ::=
              cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | sequence_expr cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | expression_or_dist [ boolean_abbrev ]
            | sequence_instance [ sequence_abbrev ]
            | ( sequence_expr {, sequence_match_item } ) [ sequence_abbrev ]
            | sequence_expr "and" sequence_expr
            | sequence_expr "intersect" sequence_expr
            | sequence_expr "or" sequence_expr
            | "first_match (" sequence_expr {, sequence_match_item} ")"
            | expression_or_dist "throughout" sequence_expr
            | sequence_expr "within" sequence_expr
            | clocking_event sequence_expr
        cycle_delay_range ::=
              "##" constant_primary
            | "## [" cycle_delay_const_range_expression "]"
            | "##[*]"
            | "##[+]"
        clocking_event ::=
              "@" identifier
            | "@ (" event_expression ")"
        expression_or_dist ::=
            expression [ "dist {" dist_list "}" ]
        boolean_abbrev ::=
              consecutive_repetition
            | non_consecutive_repetition
            | goto_repetition
        consecutive_repetition ::=
            　[* const_or_range_expression ]
            | [*]
            | [+]
        non_consecutive_repetition ::= 
            [= const_or_range_expression ]
        goto_repetition ::= 
            [-> const_or_range_expression ]
        const_or_range_expression ::=
            constant_expression
            | cycle_delay_const_range_expression
        cycle_delay_const_range_expression ::=
            constant_expression : constant_expression
            | constant_expression : $
        */

        public static SequenceExpr? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            List<SequenceExpr> exprs = new List<SequenceExpr>();
            while (!word.Eof)
            {
                SequenceOperator? op = SequenceOperator.ParseCreate(word, nameSpace);
                if (op != null)
                {
                    exprs.Add(op);
                    continue;
                }
                SequenceBooleanExpression? booleanExpression = SequenceBooleanExpression.ParseCreate(word, nameSpace);
                if (booleanExpression != null)
                {
                    exprs.Add(booleanExpression);
                    continue;
                }
                break;
            }
            if (exprs.Count == 0) return null;
            return new SequenceExpr();
        }

    }
}
