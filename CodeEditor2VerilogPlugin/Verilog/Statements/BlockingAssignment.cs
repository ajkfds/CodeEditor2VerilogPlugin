using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Statements
{
    public class BlockingAssignment : IStatement, Items.IDocumentRegeion
    {
        protected BlockingAssignment() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;

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
        public Expressions.Expression? LValue { get; protected set; }
        public Expressions.Expression? Expression { get; protected set; }
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

        public static BlockingAssignment? ParseCreate(
        WordScanner word, NameSpace nameSpace,
        CompletionContext? completionContext
        )
        {
            IndexReference expressionIref = word.CreateIndexReference();
            Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);
            if (expression == null) return null;
            switch (word.Text)
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
                    return null;
            }
            return ParseCreateAfterAssignmentOperator(word, nameSpace, expression, expressionIref, completionContext);
        }
        public static BlockingAssignment? ParseCreateAfterAssignmentOperator(
            WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression,
            IndexReference expressionIref,
            CompletionContext? completionContext
            )
        {
            switch (word.Text)
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
                return parseCreateClassNewAssignment(word, nameSpace, lExpression, expressionIref);
            }

            // delay or event control

            Expressions.Expression? expression;

            if (word.Text == "'" && word.NextText == "{")
            {
                Expressions.AssignmentPattern assignmentPattern = Expressions.AssignmentPattern.ParseCreate(word, nameSpace, false) as Expressions.AssignmentPattern;
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference=expressionIref};
                assignment.LValue = lExpression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }
            else
            {
                IndexReference expIref = word.CreateIndexReference();
                expression = Expressions.Expression.ParseCreate(word, nameSpace,completionContext);
                if (expression == null)
                {
                    // classname :: new ();
                    BlockingAssignment? assignment = parseCreateClassNewAssignment(word, nameSpace, lExpression, expIref);
                    if (assignment != null)
                    {
                        assignment.LastIndexReference = word.CreateIndexReferenceBefore();
                        if (!word.Prototype) nameSpace.DocumentRegions.Add(assignment);
                        return assignment;
                    }

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
                    lExpression.BitWidth != expression.BitWidth &&
                    expression is not Expressions.ConstantString
                    )
                {
                    WordReference wRef = WordReference.CreateReferenceRange(
                        lExpression.Reference,
                        expression.Reference
                        );
                    wRef.AddWarning("bit width mismatch " + lExpression.BitWidth + " <- " + expression.BitWidth);
                }
            }

            if (!word.Prototype && lExpression != null && expression != null)
            {
                lExpression.SyncContext.PropageteClockDomainFrom(expression.SyncContext, equalPointer, nameSpace.BuildingBlock.SameSync);
            }

            if (lExpression != null)
            {
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference=expressionIref};
                assignment.LValue = lExpression;
                assignment.Expression = expression;
                if (Assigned != null) Assigned(word, nameSpace, assignment);

                assignment.LastIndexReference = word.CreateIndexReferenceBefore();
                if (!word.Prototype) nameSpace.DocumentRegions.Add(assignment);
                return assignment;
            }
            else
            {
                return null;
            }
        }

        public static BlockingAssignment? parseCreateClassNewAssignment(WordScanner word, NameSpace nameSpace, Expressions.Expression lExpression,IndexReference expIref)
        {
            // class_new ::= [ class_scope ] "new" [ ( list_of_arguments ) ] | "new" expression
            // dynamic_array_new ::= "new" [ expression ] [ ( expression ) ]

            // Check if this is a dynamic array allocation (new followed by [size])
            bool isDynamicArrayNew = word.Text == "new" && word.NextText == "[";

            // If not dynamic array new, try to resolve class reference
            if (!isDynamicArrayNew)
            {
                BuildingBlocks.Class? class_ = word.ProjectProperty.UnitNameSpace.Get(word.Text) as BuildingBlocks.Class;
                if (class_ != null)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                    if (word.Text != "::")
                    {
                        word.AddError(":: required");
                        return null;
                    }
                    word.MoveNext();
                }
                else
                {
                    if (lExpression is Expressions.DataObjectReference)
                    {
                        Expressions.DataObjectReference dref = (Expressions.DataObjectReference)lExpression;
                        if (dref.TargetDataObject is DataObjects.Variables.Object)
                        {
                            DataObjects.Variables.Object? obj = (DataObjects.Variables.Object)dref.TargetDataObject;
                            class_ = obj.GetSourceClass();
                        }
                    }
                }
            }

            if (word.Text != "new") return null;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Handle dynamic array new: new [expression] [(expression)]
            if (word.Text == "[")
            {
                word.MoveNext(); // [
                Expressions.Expression? sizeExpression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (sizeExpression == null)
                {
                    word.AddError("size expression expected");
                    return null;
                }
                if (word.Text != "]")
                {
                    word.AddError("] expected");
                    return null;
                }
                word.MoveNext(); // ]

                // Optional constructor arguments
                if (word.Text == "(")
                {
                    word.MoveNext();
                    while (word.Text != ")" && !word.Eof)
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

                if (word.Text != ";")
                {
                    word.AddError("; expected");
                    return null;
                }
                BlockingAssignment assignment = new BlockingAssignment() { BeginIndexReference= expIref };
                assignment.LValue = lExpression;
                assignment.Expression = null; // new
                if (Assigned != null) Assigned(word, nameSpace, assignment);
                return assignment;
            }

            // Handle class new: new [(arguments)] or new expression
            if (word.Text == "(")
            {
                word.MoveNext();
                while (word.Text != ")" && !word.Eof)
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
                if (exp != null)
                {
                    // shallow clone
                }
            }

            if (word.Text != ";")
            {
                word.AddError("; expected");
                return null;
            }
            BlockingAssignment blockingAssignment = new BlockingAssignment() { BeginIndexReference=expIref};
            blockingAssignment.LValue = lExpression;
            blockingAssignment.Expression = null; // new
            if (Assigned != null) Assigned(word, nameSpace, blockingAssignment);
            return blockingAssignment;

        }
    }
}
