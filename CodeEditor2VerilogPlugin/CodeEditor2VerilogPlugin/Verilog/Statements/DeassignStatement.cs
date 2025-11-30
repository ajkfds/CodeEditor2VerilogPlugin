using CodeEditor2.CodeEditor.CodeComplete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    public class DeassignStatement : IStatement
    {
        protected DeassignStatement() { }
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

        public void DisposeSubReference()
        {
            LValue.DisposeSubReference(true);
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }
        public static DeassignStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            DeassignStatement ret = new DeassignStatement();

            if (word.Text != "deassign") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            ret.LValue = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace,false);

            if (word.Text != ";")
            {
                word.AddError("; required");
            }
            else
            {
                word.MoveNext();
            }

            return ret;
        }
    }
}
