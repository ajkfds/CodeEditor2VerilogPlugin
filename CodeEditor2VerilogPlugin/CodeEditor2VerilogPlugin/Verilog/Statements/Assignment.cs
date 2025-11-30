using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects;
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
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public Expressions.Expression LValue { get; protected set; }
        public Expressions.Expression Expression { get; protected set; }

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
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
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
        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
            Expression.DisposeSubReference(true);
        }
        public Expressions.Expression LValue { get; protected set; }
        public Expressions.Expression Expression { get; protected set; }
        /* IEEE1800-2017
        blocking_assignment ::=   variable_lvalue = delay_or_event_control expression
                                | nonrange_variable_lvalue = dynamic_array_new
                                | [ implicit_class_handle . | class_scope | package_scope ] hierarchical_variable_identifier select = class_new
                                | operator_assignment         
         */
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
            WordReference equalPointer = word.CrateWordReference();


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
                return parseCreateClassNewAssignment(word, nameSpace,lExpression);
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
                    // classname :: new ();
                    BlockingAssignment? assignment = parseCreateClassNewAssignment(word, nameSpace, lExpression);
                    if(assignment != null) return assignment;

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

            if (!word.Prototype && lExpression != null && expression != null) {
                lExpression.SyncContext.PropageteClockDomainFrom(expression.SyncContext, equalPointer);
            }

            if(lExpression!= null)
            {
                BlockingAssignment assignment = new BlockingAssignment();
                assignment.LValue = lExpression;
                assignment.Expression = expression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }
            else
            {
                return null;
            }
        }

        public static BlockingAssignment? parseCreateClassNewAssignment(WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression)
        {
            // class_new ::= [ class_scope ] "new" [ ( list_of_arguments ) ] | "new" expression
            // dynamic_array_new ::= "new" [ expression ] [ ( expression ) ]

            BuildingBlocks.Class? class_ = word.ProjectProperty.GetBuildingBlock(word.Text) as BuildingBlocks.Class;
            if(class_ != null)
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                if(word.Text != "::")
                {
                    word.AddError(":: required");
                    return null;
                }
                word.MoveNext();
            }
            else
            {
                if(lExpression is Expressions.DataObjectReference)
                {
                    Expressions.DataObjectReference dref = (Expressions.DataObjectReference)lExpression;
                    if(dref.DataObject is DataObjects.Variables.Object)
                    {
                        DataObjects.Variables.Object? obj = (DataObjects.Variables.Object)dref.DataObject;
                        class_ = obj.Class;
                    }
                }
            }

            if (word.Text != "new") return null;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            if (word.Text == "(")
            {
                word.MoveNext();
                while(word.Text!=")" && !word.Eof)
                {
                    Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                    if (expression == null) break;
                    if (word.Text != ",") break;
                    word.MoveNext();
                }
                if (word.Text != ")")
                {
                    word.AddError(") expected");
                    return null;
                }
                word.MoveNext();
            }
            else
            {
                Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);
                if(exp != null)
                {
                    // shallow clone
                }
            }

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
    }
}
