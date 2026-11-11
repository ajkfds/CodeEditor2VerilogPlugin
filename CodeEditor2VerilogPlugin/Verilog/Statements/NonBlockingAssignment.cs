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
        List<string>? clockDomains = null
        )
        {
            // EOF at LHS position (e.g. "x|"): suggest DataObject / function candidates
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            IndexReference expressionIref = word.CreateIndexReference();
            Expressions.Expression? expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false);
            if (word.Text != "<=") return null;
            if (expression == null) return null;
            return ParseCreateAfterAssignmentOperator(word, nameSpace, expression, expressionIref, clockDomains);
        }

        public static NonBlockingAssignment? ParseCreateAfterAssignmentOperator(
            WordScanner word, NameSpace nameSpace,
            Expressions.Expression lExpression,
            IndexReference expressionIref,
            List<string>? clockDomains = null
            )
        {
            if (word.Text != "<=")
            {
                System.Diagnostics.Debugger.Break();
                return null;
            }
            word.MoveNext();    // <=

            // delay_or_event_control ::= delay_control | event_control
            //                         | repeat ( expression ) event_control
            BlockingAssignment.ParseDelayOrEventControl(word, nameSpace);

            // EOF just after "<=" (e.g. "x <= |"): suggest expression candidates for RHS
            if (word.CompletionContext != null && word.Eof)
            {
                word.CompletionContext.AppendExpression();
                return null;
            }

            Expressions.Expression? expression;

            DataObjects.AssignmentPattern? assignmentPattern = null;
            if (word.Text == "'" && word.NextText == "{")
            {
                // use the DataObjects.AssignmentPattern implementation (nested
                // repetition / keyed / comma list) instead of the legacy
                // Expressions.AssignmentPattern stub. The resolved left-hand-side
                // data object is handed over to the parse, so the detailed syntax
                // check against its type is performed while parsing.
                assignmentPattern = DataObjects.AssignmentPattern.ParseCreate(
                    word,
                    nameSpace,
                    false,
                    false,
                    Items.ContinuousAssign.getAssignedDataObject(lExpression)
                    );
                expression = assignmentPattern;
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

            // a procedural assignment shall not be mixed with a continuous
            // assignment to the same variable (IEEE 1800-2017 6.5)
            if (!word.Prototype && word.CompletionContext == null && lExpression != null)
            {
                markProceduralAssignment(word, lExpression);
            }

            if (!word.Prototype && clockDomains != null && lExpression != null)
            {
                lExpression.AssertAssigned();
                BlockingAssignment.addEnclosingClockDomains(word, nameSpace, lExpression, clockDomains);
            }
            // Compare the sync context of the right-hand side with the left-hand
            // side (both the declared `// @sync:` domain and the clock domain the
            // enclosing event control drives it with, which the block above has
            // just added). A non-blocking assignment that carries a value from
            // another clock domain crosses clock domains without a synchroniser:
            // "sync mismatch".
            //
            // BlockingAssignment / VariableAssignment (continuous assign) already
            // performed this comparison; the non-blocking path only injected the
            // always-clock domain into the left-hand side and never looked at the
            // right-hand side, so `always @(posedge CLK2_I) Q <= D_CLK1;` passed
            // silently. (This is the reason
            // verilog_sample/mySample/mod2.v - DATA_I is @sync:CLK_I while
            // DATA1_O is @sync:CLK2_I and driven by CLK2_I - produced no
            // warning.)
            if (!word.Prototype && word.CompletionContext == null && lExpression != null && expression != null)
            {
                lExpression.SyncContext.PropageteClockDomainFrom(
                    expression.SyncContext,
                    lExpression.Reference,
                    nameSpace.BuildingBlock.SameSync
                    );
            }

            if (Assigned != null) Assigned(word, nameSpace, assignment);

            assignment.LastIndexReference = word.CreateIndexReferenceBefore();
            if (!word.Prototype &&word.CompletionContext == null) nameSpace.DocumentRegions.Add(assignment);
            return assignment;
        }

        /// <summary>
        /// Marks the assigned data object as procedurally assigned and reports
        /// the conflict when it is also the target of a continuous assignment
        /// (IEEE 1800-2017 6.5).
        /// </summary>
        private static void markProceduralAssignment(WordScanner word, Expressions.Expression lExpression)
        {
            DataObject? dataObject = Items.ContinuousAssign.getAssignedDataObject(lExpression);
            if (dataObject == null) return;
            if (dataObject.ContinuousAssigned)
            {
                lExpression.Reference.AddError(
                    "'" + dataObject.Name + "' is assigned by both a continuous and a procedural assignment");
            }
            dataObject.ProceduralAssigned = true;
        }
    }

}
