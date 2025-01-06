using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
 
        public static VariableAssignment? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // variable_assignment  ::= variable_lvalue = expression
            // variable_lvalue      ::= hierarchical_variable_identifier
            //                          | hierarchical_variable_identifier[expression] { [expression] }
            //                          | hierarchical_variable_identifier[expression] { [expression] } [range_expression]    
            //                          | hierarchical_variable_identifier[range_expression]   
            //                          | variable_concatenation

            VariableAssignment variableAssign = new VariableAssignment();
            Expressions.Expression? lExpression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace);
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
            WordScanner equalPointer = word.Clone();
            word.MoveNext();

            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);

            if (expression == null) return null;
            variableAssign.Expression = expression;

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
                    wRef.AddWarning("bit width mismatch " + variableAssign.NetLValue.BitWidth + " <- " + variableAssign.Expression.BitWidth);
                }
            }

            return variableAssign;
        }

    }
}
