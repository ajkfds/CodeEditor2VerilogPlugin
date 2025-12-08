using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    internal class SequenceExpression
    {
        /*
        sequence_expr ::=
              cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | sequence_expr cycle_delay_range sequence_expr { cycle_delay_range sequence_expr }
            | expression_or_dist [ boolean_abbrev ]
            | sequence_instance [ sequence_abbrev ]
            | ( sequence_expr {, sequence_match_item } ) [ sequence_abbrev ]
            | sequence_expr and sequence_expr
            | sequence_expr intersect sequence_expr
            | sequence_expr or sequence_expr
            | first_match ( sequence_expr {, sequence_match_item} )
            | expression_or_dist throughout sequence_expr
            | sequence_expr within sequence_expr
            | clocking_event sequence_expr 
         */
    }
}
