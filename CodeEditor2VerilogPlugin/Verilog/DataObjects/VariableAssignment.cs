using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.DataObjects
{
    public class VariableAssignment
    {
        protected VariableAssignment() { }

        public void DisposeSubReference()
        {
            Expression.DisposeSubReference(true);
            NetLValue.DisposeSubReference(true);
        }
        public Expressions.Expression NetLValue { get; protected set; }
        public Expressions.Expression Expression { get; protected set; }

        public static VariableAssignment? ParseCreate(WordScanner word, NameSpace nameSpace, bool acceptImplicitNet, CompletionContext? completionContext = null)
        {
            // variable_assignment  ::= variable_lvalue = expression
            // variable_lvalue      ::= hierarchical_variable_identifier
            //                          | hierarchical_variable_identifier[expression] { [expression] }
            //                          | hierarchical_variable_identifier[expression] { [expression] } [range_expression]    
            //                          | hierarchical_variable_identifier[range_expression]   
            //                          | variable_concatenation

            VariableAssignment variableAssign = new VariableAssignment();
            Expressions.Expression? lExpression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, acceptImplicitNet);
            if (lExpression == null)
            {
                return null;
            }
            variableAssign.NetLValue = lExpression;

            if (variableAssign.NetLValue.IncrementDecrement) return variableAssign;
            if (word.Text != "=")
            {
                word.AddError("= expected.");
                return null;
            }
            WordReference equalPointer = word.CrateWordReference();
            word.MoveNext();

            Expressions.Expression? expression;

            if (word.Text == "'" && word.NextText == "{")
            {
                // the resolved left-hand-side data object is handed over to the
                // parse, so the detailed syntax check against its data type is
                // performed while parsing the assignment pattern
                // (IEEE 1800-2017 10.9)
                expression = AssignmentPattern.ParseCreate(
                    word,
                    nameSpace,
                    false,
                    false,
                    Items.ContinuousAssign.getAssignedDataObject(lExpression)
                    );
            }
            else
            {
                expression = Expressions.Expression.ParseCreate(word, nameSpace, completionContext);
            }

            if (expression == null) return null;
            variableAssign.Expression = expression;

            lExpression.SyncContext.PropageteClockDomainFrom(expression.SyncContext, equalPointer,nameSpace.BuildingBlock.SameSync);

            if (!word.Prototype)
            {
                if (
                    variableAssign.NetLValue != null &&
                    variableAssign.NetLValue.BitWidth != null &&
                    variableAssign.Expression.BitWidth != null &&
                    variableAssign.NetLValue.BitWidth != variableAssign.Expression.BitWidth
                    )
                {
                    WordReference wRef = WordReference.CreateReferenceRange(variableAssign.NetLValue.Reference, variableAssign.Expression.Reference);
                    //                    wRef.AddWarning("bit width mismatch " + variableAssign.NetLValue.BitWidth + " <- " + variableAssign.Expression.BitWidth);
                    wRef.ApplyRule(word.ProjectProperty.RuleSet.AssignmentBitwidthMismatch, "bit width mismatch " + variableAssign.NetLValue.BitWidth + " <- " + variableAssign.Expression.BitWidth);
                }
            }


            return variableAssign;
        }

    }
}
