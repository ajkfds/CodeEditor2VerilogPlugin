using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions
{
    public class DollarMark : Primary
    {
        /*
        The $ symbol in SystemVerilog, particularly within the context of A.8.4 Primaries, 
        primarily functions as a literal constant and a primary.
        
        It is used to denote:
        - Unbounded Range Specification:
            The $ value can be assigned to integer-type parameters to represent an unlimited range.
            This is frequently used in delay ranges for properties and sequences,
            indicating an unbounded maximum. For example, ##[r1:$] signifies a delay with no upper limit.
            The $isunbounded system function can test if a constant is $, which is useful for configuring behavior in generate statements.

        - Last Element or Maximum/Minimum Value:
          - Queues: 
            For SystemVerilog queues, $ serves as an index to refer to the last element.
            It can also specify an unbounded maximum size during queue declaration (e.g., byte q1[$]).
          - inside Operator:
            When used in range specifications with the inside operator (e.g., [low_bound:high_bound]),
            $ can represent the lowest or highest possible value for the type of the left-hand expression.
          - covergroup Bin Definitions: 
            In coverpoint bin definitions, $ can be legally used as a primary in covergroup_value_range
            (e.g., [expression : $] or [$ : expression]) to indicate an unbounded upper or lower limit for a bin range.
        - Arguments in Sequences and Properties: 
            When $ is passed as an actual argument in instances of named sequences or properties,
            the corresponding formal argument must be untyped.
            In such cases, its references must be an upper bound
            in a cycle_delay_const_range_expression or another actual argument in a sequence or property instance.         
         */


        private DollarMark() { }

        public override bool Constant { get => true; }
        public static new DollarMark ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "$") throw new Exception();
            DollarMark dollarMark = new DollarMark() { Reference = word.GetReference() };

            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            return dollarMark;
        }


    }
}
