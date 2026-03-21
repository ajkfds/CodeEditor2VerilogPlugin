using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    class TimingControlStatements
    {
    }


    public class DelayControl
    {
        protected DelayControl() { }

        public Expressions.Expression DelayValue { get; protected set; }
        public static DelayControl? ParseCreate(WordScanner word,NameSpace nameSpace)
        {
            /*
            delay_control   ::= # delay_value
                                | # ( mintypmax_expression )  
            delay_value     ::= unsigned_number
                                | parameter_identifier
                                | specparam_identifier
                                | mintypmax_expression 
            */
            /*
            delay3 ::= # delay_value | # ( mintypmax_expression [ , mintypmax_expression [ , mintypmax_expression ] ] )
            delay2 ::= # delay_value | # ( mintypmax_expression [ , mintypmax_expression ] )
            delay_value ::=
                unsigned_number
                | real_number
                | ps_identifier
                | time_literal
                | 1step             
             */
            System.Diagnostics.Debug.Assert(word.Text == "#");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if(word.Text != "(")
            {
                Expressions.Expression? expression = Primary.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    word.AddError("illegal delay control");
                    return null;
                }
            }
            else
            {
                word.MoveNext();
                Expressions.Expression? expression1 = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression1 == null)
                {
                    word.AddError("illegal delay control");
                    return null;
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                    Expressions.Expression? expression2 = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression2 == null)
                    {
                        word.AddError("illegal delay control");
                        return null;
                    }

                    if (word.Text == ",")
                    {
                        word.MoveNext();
                        Expressions.Expression? expression3 = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (expression3 == null)
                        {
                            word.AddError("illegal delay control");
                            return null;
                        }
                    }
                }
                if(word.Text != ")")
                {
                    word.AddError("illegal delay control");
                    return null;
                }
                word.MoveNext();
            }

            DelayControl delayControl = new DelayControl();
//            delayControl.DelayValue = expression;
            return delayControl;
        }
    }


    public class EventControl
    {
        protected EventControl() { }
        public List<EventExpression> EventExpressions { get; protected set; }

        public static EventControl? ParseCreate(WordScanner word, NameSpace nameSpace, List<string>? clockDomains = null)
        {
            /*
            event_control       ::= @ event_identifier
                                    | @ ( event_expression )
                                    | @*          
                                    | @ (*)
            event_expression    ::= expression
                                    | hierarchical_identifier
                                    | posedge expression
                                    | negedge expression
                                    | event_expression or event_expression
                                    | event_expression , event_expression
            */
            System.Diagnostics.Debug.Assert(word.Text == "@");
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            List<EventExpression> eventExpressions = new List<EventExpression>();

            if (word.GetCharAt(0) == '(')
            {
                word.MoveNext(); // (
                if(word.GetCharAt(0) == '*')
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext(); // *
                    if(word.GetCharAt(0) != ')')
                    {
                        word.AddError("illegal event contol");
                        return null;
                    }
                    word.MoveNext(); // )
                }
                else
                {
                    eventExpressions = EventExpression.ParseCreate(word, nameSpace,clockDomains);
                    if (word.GetCharAt(0) != ')' || eventExpressions.Count == 0)
                    {
                        word.AddError("illegal event contol");
                        return null;
                    }
                    word.MoveNext(); // )
                }
            }
            else
            {
                if (word.GetCharAt(0) == '*')
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext(); // *
                }
                else
                {
                    eventExpressions = EventExpression.ParseCreate(word, nameSpace, clockDomains);
                    if (eventExpressions.Count == 0)
                    {
                        word.AddError("illegal event contol");
                        return null;
                    }
                }
            }

            EventControl eventControl = new EventControl();
            eventControl.EventExpressions = eventExpressions;
            return eventControl;
        }
    }

    public class EventExpression
    {
        protected EventExpression() { }

        public EventTypeEnum EventType { get; protected set; }
        public Expressions.Expression? Expression { get; protected set; }

        public enum EventTypeEnum
        {
            Both,
            Posedge,
            Negedge
        }

        public static List<EventExpression> ParseCreate(WordScanner word, NameSpace nameSpace, List<string>? clockDomains = null)
        {
            /*
            event_expression    ::= expression
                                    | hierarchical_identifier
                                    | posedge expression
                                    | negedge expression
                                    | event_expression or event_expression
                                    | event_expression , event_expression
            */

            List<EventExpression> eventExpressions = new List<EventExpression>();

            EventExpression? eventExpression = EventExpression.ParseCreateSingle(word, nameSpace, clockDomains);
            eventExpressions.Add(eventExpression);

            while (!word.Eof)
            {
                switch (word.Text)
                {
                    case ",":
                        word.MoveNext();
                        eventExpression = EventExpression.ParseCreateSingle(word, nameSpace, clockDomains);
                        break;
                    case "or":
                        word.MoveNext();
                        eventExpression = EventExpression.ParseCreateSingle(word, nameSpace, clockDomains);
                        break;
                    default:
                        eventExpression = null;
                        break;
                }
                if(eventExpression == null)
                {
                    break;
                }
                eventExpressions.Add(eventExpression);
            }
            return eventExpressions;
        }

        public static EventExpression? ParseCreateSingle(WordScanner word, NameSpace nameSpace, List<string>? clockDomains = null)
        {
            EventExpression? eventExpression = new EventExpression();
            switch (word.Text)
            {
                case "posedge":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    eventExpression.EventType = EventTypeEnum.Posedge;
                    eventExpression.Expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if(clockDomains != null && eventExpression.Expression != null) addClockDomain(eventExpression.Expression, clockDomains);
                    break;
                case "negedge":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    eventExpression.EventType = EventTypeEnum.Negedge;
                    eventExpression.Expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (clockDomains != null && eventExpression.Expression != null) addClockDomain(eventExpression.Expression, clockDomains);
                    break;
                default:
                    eventExpression.EventType = EventTypeEnum.Both;
                    eventExpression.Expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (clockDomains != null && eventExpression.Expression != null) addClockDomain(eventExpression.Expression, clockDomains);
                    break;
            }
            if (eventExpression.Expression == null)
            {
                return null;
            }
            return eventExpression;
        }

        private static void addClockDomain(Expressions.Expression expression, List<string> clockDomains)
        {
            List<Verilog.DataObjects.DataObject> refrencedObjects = new System.Collections.Generic.List<DataObjects.DataObject>();
            expression.AppendRefrencedDataObjects(refrencedObjects);
            foreach (var dataObject in refrencedObjects)
            {
                if (dataObject.SyncContext.IsReset) continue;
                clockDomains.Add(dataObject.Name);
            }
        }
    }


    /*
    A.6.5 Timing control statements
    delay_or_event_control  ::= delay_control
                                | event_control
                                | repeat ( expression ) event_control

    disable_statement   ::= disable hierarchical_task_identifier ;
                            | disable hierarchical_block_identifier ;
    event_trigger       ::= -> hierarchical_event_identifier ;
    procedural_timing_control_statement ::= delay_or_event_control
    statement_or_null wait_statement    ::= wait ( expression ) statement_or_null
    */
}
