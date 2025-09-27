using pluginVerilog.Verilog.Expressions;
using pluginVerilog.Verilog.Expressions.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public static class Statements
    {

        /* Systemverilog IEEE 1800-2017
        statement ::= [ block_identifier : ] { attribute_instance } statement_item
        statement_item ::=    blocking_assignment ;
        | nonblocking_assignment ;
        | procedural_continuous_assignment ;         
        | case_statement
        | conditional_statement
        | inc_or_dec_expression ;
        | subroutine_call_statement
        | disable_statement
        | event_trigger
        | loop_statement
        | jump_statement
        | par_block
        | procedural_timing_control_statement
        | seq_block
        | wait_statement
        | procedural_assertion_statement
        | clocking_drive ;
        | randsequence_statement
        | randcase_statement
        | expect_property_statement         
        function_statement ::= statement
        function_statement_or_null ::=
        function_statement
        | { attribute_instance } ;
        variable_identifier_list ::= variable_identifier { , variable_identifier }         
        */
        /*
        A.6.4 Statements
        statement   ::= { attribute_instance } blocking_assignment ;
                        | { attribute_instance } case_statement
                        | { attribute_instance } conditional_statement
                        | { attribute_instance } disable_statement
                        | { attribute_instance } event_trigger
                        | { attribute_instance } loop_statement
                        | { attribute_instance } nonblocking_assignment ;
                        | { attribute_instance } par_block
                        | { attribute_instance } procedural_continuous_assignments ;
                        | { attribute_instance } procedural_timing_control_statement
                        | { attribute_instance } seq_block
                        | { attribute_instance } system_task_enable
                        | { attribute_instance } task_enable
                        | { attribute_instance } wait_statement

        statement_or_null   ::= statement
                                | { attribute_instance } ;

        function_statement  ::= { attribute_instance } function_blocking_assignment ;
                                | { attribute_instance } function_case_statement
                                | { attribute_instance } function_conditional_statement
                                | { attribute_instance } function_loop_statement
                                | { attribute_instance } function_seq_block
                                | { attribute_instance } disable_statement
                                | { attribute_instance } system_task_enable  
        */
        public static IStatement? ParseCreateStatement(WordScanner word, NameSpace nameSpace)
        {
            /*
            A.6.4 Statements
            statement   ::= 
                            | { attribute_instance } conditional_statement

                            /// done
                            | { attribute_instance } blocking_assignment ;
                            | { attribute_instance } nonblocking_assignment ;
                            | { attribute_instance } procedural_timing_control_statement
                            | { attribute_instance } loop_statement
                            | { attribute_instance } case_statement
                            | { attribute_instance } disable_statement
                            | { attribute_instance } seq_block
                            | { attribute_instance } par_block
                            | { attribute_instance } procedural_continuous_assignments ;
                            | { attribute_instance } system_task_enable
                            | { attribute_instance } task_enable

                            | { attribute_instance } event_trigger
                            | { attribute_instance } wait_statement
            procedural_timing_control_statement ::= delay_or_event_control statement_or_null 
            */
            /* # SystemVerilog
            statement_or_null ::= statement | { attribute_instance } ;
            statement ::= [ block_identifier : ] { attribute_instance } statement_item
            statement_item ::=    blocking_assignment ;
	                            | nonblocking_assignment ;
	                            | procedural_continuous_assignment ;
	                            | case_statement 
	                            | conditional_statement 
	                            | inc_or_dec_expression ;
	                            | subroutine_call_statement 
	                            | disable_statement 
	                            | event_trigger 
	                            | loop_statement 
	                            | jump_statement 
	                            | par_block 
	                            | procedural_timing_control_statement 
	                            | seq_block 
	                            | wait_statement 
	                            | procedural_assertion_statement 
	                            | clocking_drive ;
	                            | randsequence_statement 
	                            | randcase_statement 
	                            | expect_property_statement
            subroutine_call_statement ::= 
                subroutine_call ; 
                | void ' ( function_subroutine_call ) ; 
            */

            // inc_or_dec_expression ;
            // procedural_assertion_statement 
            // clocking_drive ;
            // randsequence_statement 
            // randcase_statement 
            // expect_property_statement 

            string? statement_label = null;

            if(word.NextText == ":" && !General.ListOfKeywords.Contains(word.Text) && !General.IsIdentifier(word.Text))
            {
                word.MoveNext();
                statement_label = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext(); //:
            }

            switch (word.Text)
            {
                case "(*":
                    Attribute attribute = Attribute.ParseCreate(word, nameSpace);
                    return Statements.ParseCreateStatement(word, nameSpace);

                // unique_priority
                case "unique":
                case "unique0":
                case "priority":
                    return ParseUniquePriority(word, nameSpace, statement_label);

                // conditional_statement 
                case "if":
                    return ConditionalStatement.ParseCreate(word, nameSpace, statement_label);

                // procedural_timing_control_statement 
                case "#":
                case "@":
                    return ProceduralTimingControlStatement.ParseCreate(word, nameSpace, statement_label);

                // seq_block 
                case "begin":
                    return SequentialBlock.ParseCreate(word, nameSpace, statement_label);

                // par_block 
                case "fork":
                    return ParallelBlock.ParseCreate(word, nameSpace, statement_label);

                // ## loop_statement 
                // forever statement_or_null
                case "forever":
                    return ForeverStatement.ParseCreate(word, nameSpace, statement_label);
                // repeat(expression) statement_or_null
                case "repeat":
                    return RepeatStatement.ParseCreate(word, nameSpace, statement_label);
                // while (expression) statement_or_null
                case "while":
                    return WhileStatememt.ParseCreate(word, nameSpace, statement_label);
                // ([for_initialization]; [expression]; [for_step]) statement_or_null
                case "for":
                    return ForStatememt.ParseCreate(word, nameSpace, statement_label);
                // do statement_or_null while (expression);
                case "do":
                    return DoStatement.ParseCreate(word, nameSpace, statement_label);
                // foreach (ps_or_hierarchical_array_identifier[loop_variables] ) statement
                case "foreach":
                    return ForeachStatement.ParseCreate(word, nameSpace, statement_label);

                // case_statement 
                case "case":
                case "casex":
                case "casez":
                    return CaseStatement.ParseCreate(word, nameSpace, statement_label);

                // disable_statement 
                case "disable":
                    return DisableStatement.ParseCreate(word,nameSpace, statement_label);

                case "force":
                    return ForceStatement.ParseCreate(word,nameSpace, statement_label);
                case "release":
                    return ReleaseStatement.ParseCreate(word, nameSpace, statement_label);

                // jump_statement 
                case "return":
                    return ReturnStatement.ParseCreate(word, nameSpace, statement_label);
                case "break":
                    return BreakStatement.ParseCreate(word, nameSpace, statement_label);
                case "continue":
                    return ContinueStatement.ParseCreate(word, nameSpace, statement_label);

                // procedural_continuous_assignment ;
                case "assign":
                    return ProceduralContinuousAssignment.ParseCreate(word, nameSpace, statement_label);

                case "deassign":
                    return DeassignStatement.ParseCreate(word, nameSpace, statement_label);
                // event_trigger 
                case "->":
                    return EventTrigger.ParseCreate(word, nameSpace, statement_label);
                case ";":
                    word.AddError("illegal module item");
                    word.MoveNext();
                    return null;
                // wait_statement 
                case "wait":
                case "wait_order":
                    return WaitStatement.ParseCreate(word, nameSpace, statement_label);
                default:

                    // subroutine_call_statement 
                    /*
                        SystemVerilog IEEE1800-2017
                    subroutine_call_statement ::=     subroutine_call ;
                                                    | void ' ( function_subroutine_call ) ;
                    function_subroutine_call ::= subroutine_call

                    subroutine_call ::=               tf_call
                                                    | system_tf_call
                                                    | method_call
                                                    | [ std :: ] randomize_call

                    tf_call ::=                     ps_or_hierarchical_tf_identifier { attribute_instance } [ ( list_of_arguments ) ]
                    list_of_arguments ::=             [ expression ] { , [ expression ] } { , . identifier ( [ expression ] ) }
                                                    | .identifier ( [ expression ] ) { , . identifier ( [ expression ] ) }
                    ps_or_hierarchical_tf_identifier ::=      [ package_scope ] tf_identifier
                                                            | hierarchical_tf_identifier
                    hierarchical_tf_identifier ::= hierarchical_identifier
                    hierarchical_identifier ::= [ $root . ] { identifier constant_bit_select . } identifier
                    tf_identifier ::= identifier
                     */

                    string nextText = word.NextText;
                    if (nextText == "(" || nextText == ";")
                    {
                        if (word.Text.StartsWith("$"))
                        {
                            if (!word.RootParsedDocument.ProjectProperty.SystemTaskParsers.ContainsKey(word.Text))
                            {
                                word.AddError("unsupported system task");
                                return SystemTask.SkipArguments.ParseCreate(word, nameSpace);
                            }else if(word.RootParsedDocument.ProjectProperty.SystemTaskParsers[word.Text] != null)
                            {
                                return word.RootParsedDocument.ProjectProperty.SystemTaskParsers[word.Text](word, nameSpace);
                            }
                            else
                            {
                                return SystemTask.SystemTask.ParseCreate(word, nameSpace);
                            }
                        }
                        else if (General.IsIdentifier(word.Text)){
                            return TaskEnable.ParseCreate(word, nameSpace,nameSpace);
                        }
                    }else if (word.Text =="void" && nextText == "'")
                    {
                        return VoidFunctionCall.ParseCreate(word, nameSpace);
                    }

                    IncOrDecExpression? incOrDecExpression = IncOrDecExpression.ParseCreate(word, nameSpace,false);
                    if (incOrDecExpression != null)
                    {
                        if (word.Text != ";")
                        {
                            word.AddError("; missing");
                        }
                        else
                        {
                            word.MoveNext();
                        }
                        return incOrDecExpression;
                    }

                    Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace,false);
                    if(expression != null && expression is Expressions.TaskReference)// Expressions.TaskReference)
                    {
                        Expressions.TaskReference taskReference = (Expressions.TaskReference)expression;
                        return TaskEnable.ParseCreate(taskReference, word, nameSpace);
                    }
                    if(expression != null && expression is Expressions.FunctionCall)
                    {
                        Expressions.FunctionCall functionCall = (Expressions.FunctionCall)expression;
                        if (word.Text == ";")
                        {
                            word.MoveNext();
                        }
                        else
                        {
                            word.AddError("; missing");
                        }
                        if (functionCall.Function?.ReturnVariable == null) return VoidFunctionCall.Create(functionCall);
                    }
                    if(expression != null && expression is BuiltinMethodCall)
                    {
                        Expressions.BuiltinMethodCall methodCall = (Expressions.BuiltinMethodCall)expression;
                        if (word.Text == ";")
                        {
                            word.MoveNext();
                        }
                        else
                        {
                            word.AddError("; missing");
                        }
                        if (methodCall.BuiltInMethod.ReturnVariable == null) return VoidBuiltInMethodCall.Create(methodCall);
                    }

                    if (expression == null)
                    {
                        word.AddError("illegal statement");
                        return null;
                    }


                    IStatement? statement;
                    switch (word.Text)
                    {
                        // blocking_assignment ;
                        case "=":
                            statement = BlockingAssignment.ParseCreate(word, nameSpace, expression);
                            break;
                        // nonblocking_assignment ;
                        case "<=":
                            statement = NonBlockingAssignment.ParseCreate(word, nameSpace, expression);
                            break;
                        case "+=":
                        case "-=":
                        case "*=":
                        case "/=":
                        case "%=":
                        case "&=":
                        case "|=":
                        case "^=":
                        case "<<=":
                        case ">>=":
                        case "<<<=":
                        case ">>>=":
                            statement = BlockingAssignment.ParseCreate(word, nameSpace, expression);
                            break;
                        case "++":
                        case "--":
                            word.AddError("illegal operator");
                            word.MoveNext();
                            return null;
                        default:
                            expression.Reference.AddError("illegal statement");
                            return null;
                    }
                    if(word.GetCharAt(0) != ';')
                    {
                        word.AddError("; expected");
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    return statement;
            }
        }

        private static IStatement ParseUniquePriority(WordScanner word, NameSpace nameSpace,string? statement_label)
        {
            switch (word.Text)
            {
                case "unique":
                case "unique0":
                case "priority":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                default:
                    throw new Exception();
            }

            if (word.Eof)
            {
                word.AddError("if or case required");
                return null;
            }

            switch (word.Text)
            {
                // conditional_statement 
                case "if":
                    return ConditionalStatement.ParseCreate(word, nameSpace,statement_label);

                // case_statement 
                case "case":
                case "casex":
                case "casez":
                    return CaseStatement.ParseCreate(word, nameSpace, statement_label);

                default:
                    word.AddError("if or case required");
                    return null;
            }
        }
        private static NameSpace getSpace(string identifier, NameSpace nameSpace)
        {
            if (nameSpace.NamedElements.ContainsKey(identifier) && nameSpace.NamedElements[identifier] is NameSpace)
            {
                return (NameSpace)nameSpace.NamedElements[identifier];
            }
            if (nameSpace.Parent == null) return null;

            return getSpace(identifier, nameSpace.Parent);
        }

        public static IStatement? ParseCreateStatementOrNull(WordScanner word, NameSpace nameSpace)
        {
            if(word.GetCharAt(0) == ';')
            {
                word.MoveNext();
                return null;
            }
            return ParseCreateStatement(word, nameSpace);
        }


        public static IStatement? ParseCreateFunctionStatement(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreateStatement(word,nameSpace);
        }
    }


}
