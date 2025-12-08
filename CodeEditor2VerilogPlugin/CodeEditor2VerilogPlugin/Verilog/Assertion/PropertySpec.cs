using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    internal class PropertySpec
    {
        /*
        property_spec ::=
            [clocking_event ] [ "disable" "iff" "(" expression_or_dist ")" ] property_expr         
        
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
    }
}
