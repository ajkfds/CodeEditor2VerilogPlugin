using pluginVerilog.Verilog.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Property
{
    public class PropertyExpr
    {
        /*
        property_expr ::=
              sequence_expr
            | strong ( sequence_expr )
            | weak ( sequence_expr )
            | ( property_expr )
            | not property_expr
            | property_expr or property_expr
            | property_expr and property_expr
            | sequence_expr |-> property_expr
            | sequence_expr |=> property_expr
            | if ( expression_or_dist ) property_expr [ else property_expr ]
            | case ( expression_or_dist ) property_case_item { property_case_item } endcase
            | sequence_expr #-# property_expr
            | sequence_expr #=# property_expr
            | nexttime property_expr
            | nexttime [ constant _expression ] property_expr
            | s_nexttime property_expr
            | s_nexttime [ constant_expression ] property_expr
            | always property_expr
            | always [ cycle_delay_const_range_expression ] property_expr
            | s_always [ constant_range ] property_expr
            | s_eventually property_expr
            | eventually [ constant_range ] property_expr
            | s_eventually [ cycle_delay_const_range_expression ] property_expr
            | property_expr until property_expr
            | property_expr s_until property_expr
            | property_expr until_with property_expr
            | property_expr s_until_with property_expr
            | property_expr implies property_expr
            | property_expr iff property_expr
            | accept_on ( expression_or_dist ) property_expr
            | reject_on ( expression_or_dist ) property_expr
            | sync_accept_on ( expression_or_dist ) property_expr
            | sync_reject_on ( expression_or_dist ) property_expr
            | property_instance
            | clocking_event property_expr
        property_case_item ::=
            expression_or_dist { , expression_or_dist } : property_expr ;
            | default [ : ] property_expr ;
        assertion_variable_declaration ::=
            var_data_type list_of_variable_decl_assignments ;
        property_instance ::=
            ps_or_hierarchical_property_identifier [ ( [ property_list_of_arguments ] ) ]
        property_list_of_arguments ::=
            [property_actual_arg] { , [property_actual_arg] } { , . identifier ( [property_actual_arg] ) }
            | . identifier ( [property_actual_arg] ) { , . identifier ( [property_actual_arg] ) }
        property_actual_arg ::=
            property_expr
            | sequence_actual_arg         
         */
        public static PropertyExpr? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            List<PropertyPrimary> primaries = new List<PropertyPrimary>();
            while (!word.Eof)
            {
                SequenceExpr? sequenceExpr = SequenceExpr.ParseCreate(word, nameSpace);
                if(sequenceExpr != null)
                {
                    primaries.Add(sequenceExpr);
                    continue;
                }
                PropertyOperator? propertyOperator = PropertyOperator.ParseCreate(word, nameSpace);
                if (propertyOperator != null)
                {
                    primaries.Add(propertyOperator);
                    continue;
                }
                break;
            }
            return null;
        }
    }
}
