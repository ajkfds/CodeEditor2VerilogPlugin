using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Statements
{

    public class NonBlockingAssignment : IStatement, Items.IDocumentRegeion
    {
        protected NonBlockingAssignment() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public Expressions.Expression? LValue { get; protected set; }
        public Expressions.Expression? Expression { get; protected set; }
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

        public static NonBlockingAssignment? ParseCreate(
        WordScanner word, NameSpace nameSpace,
        CompletionContext? completionContext,
        List<string>? clockDomains = null
        )
        {
            IndexReference expressionIref = word.CreateIndexReference();
            Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);
            if (word.Text != "<=") return null;
            if (expression == null) return null;
            return ParseCreateAfterAssignmentOperator(word, nameSpace, expression, expressionIref, completionContext, clockDomains);
        }

        public static NonBlockingAssignment? ParseCreateAfterAssignmentOperator(
            WordScanner word, NameSpace nameSpace,
            Expressions.Expression lExpression,
            IndexReference expressionIref,
            CompletionContext? completionContext,
            List<string>? clockDomains = null
            )
        {
            if (word.Text != "<=")
            {
                System.Diagnostics.Debugger.Break();
                return null;
            }
            word.MoveNext();    // <=

            if (word.GetCharAt(0) == '#')
            {
                DelayControl? delayControl = DelayControl.ParseCreate(word, nameSpace);
            }
            else if (word.GetCharAt(0) == '@')
            {
                EventControl? eventControl = EventControl.ParseCreate(word, nameSpace);
            }

            Expressions.Expression? expression;

            if (word.Text == "'" && word.NextText == "{")
            {
                expression = Expressions.AssignmentPattern.ParseCreate(word, nameSpace, false);
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

            if (!word.Prototype)
            {
                if (
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

            NonBlockingAssignment assignment = new NonBlockingAssignment() { BeginIndexReference = expressionIref };
            assignment.LValue = lExpression;
            assignment.Expression = expression;

            if (!word.Prototype && clockDomains != null && lExpression != null)
            {
                List<DataObject> dataObjects = new List<DataObject>();
                lExpression.AppendRefrencedDataObjects(dataObjects);
                lExpression.AssertAssigned();

                foreach (DataObject dataObject in dataObjects)
                {
                    DataObject? targetDataObject = nameSpace.GetNamedElementUpward(dataObject.Name) as DataObject;
                    if (targetDataObject == null) continue;

                    foreach (string clockDomain in clockDomains)
                    {
                        INamedElement? namedElemect = nameSpace.GetNamedElementUpward(clockDomain);
                        if (namedElemect is DataObject clkObject)
                        {
                            if (clkObject.SyncContext.IsReset) continue;
                        }

                        targetDataObject.SyncContext.AddClockDomain(clockDomain, lExpression.Reference,nameSpace.BuildingBlock.SameSync);
                    }
                }
            }
            if (Assigned != null) Assigned(word, nameSpace, assignment);

            assignment.LastIndexReference = word.CreateIndexReferenceBefore();
            if (!word.Prototype) nameSpace.DocumentRegions.Add(assignment);
            return assignment;
        }
    }

}
