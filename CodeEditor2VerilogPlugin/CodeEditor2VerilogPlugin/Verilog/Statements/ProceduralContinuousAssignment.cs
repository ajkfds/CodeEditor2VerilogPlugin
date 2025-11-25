using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class ProceduralContinuousAssignment : IStatement
    {
        protected ProceduralContinuousAssignment() { }
        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        /*
        procedural_continuous_assignments ::=
            assign variable_assignment 
            | deassign variable_lvalue 
            | force variable_assignment 
            | force net_assignment 
            | release variable_lvalue 
            | release net_lvalue
        */
        public Expressions.Expression LValue;
        public Expressions.Expression Value;


        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
            Value.DisposeSubReference(true);
        }

        public static ProceduralContinuousAssignment? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {

            if (word.Text != "assign") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            Expressions.Expression? lvalue = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace,false);

            if (word.Text != "=") return null;
            WordReference equalPointer = word.CrateWordReference();
            word.MoveNext();

            Expressions.Expression? value = Expressions.Expression.ParseCreate(word, nameSpace);

            if (word.Text != ";")
            {
                word.AddError("; required");
            }
            else
            {
                word.MoveNext();
            }

            if (lvalue == null || value == null) return null;
            ProceduralContinuousAssignment ret = new ProceduralContinuousAssignment() { LValue = lvalue, Value = value };

            lvalue.SyncContext.PropageteClockDomainFrom(value.SyncContext,equalPointer);
            return ret;
        }
    }
}
