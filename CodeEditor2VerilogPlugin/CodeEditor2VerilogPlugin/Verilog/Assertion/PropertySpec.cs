using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Assertion
{
    public class PropertySpec
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
        public static async Task<PropertySpec> ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            EventControl? eventControl = EventControl.ParseCreate(word, nameSpace);
            Expressions.Expression? disableIffExpression = null;

            if (word.Text == "disable")
            {
                do
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text == "iff")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("iff missing");
                        break;
                    }
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("illegal property spec");
                        break;
                    }
                    disableIffExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("illegal property spec");
                        break;
                    }
                } while (false);
            }


            PropertySpec propertySpec = new PropertySpec() 
            { 
                EventControl = eventControl,
                DisableIffExpression = disableIffExpression,
            };

            // must implement
            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);



            return propertySpec;
        }

        public EventControl? EventControl { get; set; }
        public Expressions.Expression? DisableIffExpression { get; set; }
    }
}
