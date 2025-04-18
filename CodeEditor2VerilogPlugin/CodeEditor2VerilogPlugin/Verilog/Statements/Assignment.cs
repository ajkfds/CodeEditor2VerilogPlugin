﻿using pluginVerilog.Verilog.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{

    public class NonBlockingAssignment : IStatement
    {
        protected NonBlockingAssignment() { }

        public Expressions.Expression LValue { get; protected set; }
        public Expressions.Expression Expression { get; protected set; }

        /*
        A.6.2 Procedural blocks and assignments
        initial_construct   ::= initial statement
        always_construct    ::= always statement
        blocking_assignment ::= variable_lvalue = [ delay_or_event_control ] expression
        nonblocking_assignment ::= variable_lvalue <= [ delay_or_event_control ] expression
        procedural_continuous_assignments   ::= assign variable_assignment
                                                | deassign variable_lvalue
                                                | force variable_assignment
                                                | force net_assignment
                                                | release variable_lvalue
                                                | release net_lvalue
         */
        public delegate void NonBlockingAssignedAction(WordScanner word, NameSpace nameSpace, NonBlockingAssignment blockingAssignment);
        public static NonBlockingAssignedAction? Assigned;

        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
            Expression.DisposeSubReference(true);
        }
        public static NonBlockingAssignment? ParseCreate(WordScanner word,NameSpace nameSpace,Expressions.Expression lExpression)
        {
            if(word.Text != "<=")
            {
                System.Diagnostics.Debugger.Break();
                return null;
            }
            word.MoveNext();    // <=

            if (word.GetCharAt(0) == '#')
            {
                DelayControl delayControl = DelayControl.ParseCreate(word, nameSpace);
            }
            else if (word.GetCharAt(0) == '@')
            {
                EventControl eventControl = EventControl.ParseCreate(word, nameSpace);
            }

            Expressions.Expression? expression;

            if(word.Text == "'" && word.NextText == "{")
            {
                expression = AssignmentPattern.ParseCreate(word, nameSpace,false);
            }
            else
            {
                expression = Expressions.Expression.ParseCreate(word, nameSpace);
            }

            if (expression == null)
            {
                word.SkipToKeyword(";");
                word.AddError("illegal non blocking assignment");
                return null;
            }

            if (!word.Prototype) {
                if(
                    lExpression != null && 
                    lExpression.BitWidth != null && 
                    expression.BitWidth != null &&
                    lExpression.BitWidth != expression.BitWidth
                    )
                {
                    WordReference wordReference = WordReference.CreateReferenceRange(
                        lExpression.Reference,
                        expression.Reference
                        );
                    wordReference.AddWarning("bit width mismatch " + lExpression.BitWidth + " <- " + expression.BitWidth);
                }
            }

            NonBlockingAssignment assignment = new NonBlockingAssignment();
            assignment.LValue = lExpression;
            assignment.Expression = expression;
            if (Assigned != null) Assigned(word, nameSpace, assignment);
            return assignment;
        }
    }
    public class BlockingAssignment : IStatement
    {
        protected BlockingAssignment() { }

        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
            Expression.DisposeSubReference(true);
        }
        public Expressions.Expression LValue { get; protected set; }
        public Expressions.Expression Expression { get; protected set; }
        /*
        A.6.2 Procedural blocks and assignments
        initial_construct   ::= initial statement
        always_construct    ::= always statement
        blocking_assignment ::= variable_lvalue = [ delay_or_event_control ] expression
        nonblocking_assignment ::= variable_lvalue <= [ delay_or_event_control ] expression
        procedural_continuous_assignments   ::= assign variable_assignment
                                                | deassign variable_lvalue
                                                | force variable_assignment
                                                | force net_assignment
                                                | release variable_lvalue
                                                | release net_lvalue
         */

        public delegate void BlockingAssignedAction(WordScanner word, NameSpace nameSpace, BlockingAssignment blockingAssignment);
        public static BlockingAssignedAction? Assigned;
        public static BlockingAssignment? ParseCreate(WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression)
        {
            switch(word.Text)
            {
                case "=":
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
                    break;
                default:
                    throw new Exception();
            }


                word.MoveNext();    // <=

            if (word.GetCharAt(0) == '#')
            {
                DelayControl delayControl = DelayControl.ParseCreate(word, nameSpace);
            }
            else if (word.GetCharAt(0) == '@')
            {
                EventControl eventControl = EventControl.ParseCreate(word, nameSpace);
            }

            if (word.Text == "new")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                if (word.Text != ";")
                {
                    word.AddError("; expected");
                    return null;
                }
                BlockingAssignment assignment = new BlockingAssignment();
                assignment.LValue = lExpression;
                assignment.Expression = null; // new
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;

            }

            // delay or event control

            Expressions.Expression? expression;


            if (word.Text=="'" && word.NextText == "{")
            {
                AssignmentPattern assignmentPattern = AssignmentPattern.ParseCreate(word, nameSpace,false);
                BlockingAssignment assignment = new BlockingAssignment();
                assignment.LValue = lExpression;
//                assignment.Expression = expression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }
            else
            {
                expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword(";");
                    word.AddError("illegal non blocking assignment");
                    return null;
                }
            }


            if (!word.Prototype)
            {
                if (
                    lExpression != null &&
                    lExpression.BitWidth != null &&
                    expression.BitWidth != null &&
                    lExpression.BitWidth != expression.BitWidth
                    )
                {
                    WordReference wRef = WordReference.CreateReferenceRange(
                        lExpression.Reference,
                        expression.Reference
                        );
                    wRef.AddWarning("bit width mismatch " + lExpression.BitWidth + " <- " + expression.BitWidth);
                }
            }

            {
                BlockingAssignment assignment = new BlockingAssignment();
                assignment.LValue = lExpression;
                assignment.Expression = expression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }
        }
    }
}
