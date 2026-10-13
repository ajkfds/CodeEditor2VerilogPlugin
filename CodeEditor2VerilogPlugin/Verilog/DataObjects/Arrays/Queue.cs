
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class Queue : DataObject, IArray
    {
        // defined in section 7.10
        protected Queue() { }

        public bool IsValidForNet { get { return false; } }
        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;
        public int? Size { get; protected set; } = null;
        public bool Constant { get; protected set; } = false;

        public Expressions.Expression? MaxSizeExpression { get; protected set; }

        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("[ $");
            if (MaxSizeExpression != null)
            {
                label.AppendText(": ");
                label.AppendLabel(MaxSizeExpression.GetLabel());
            }
            label.AppendText(" ]");
        }
        public required DataObject DataObject { init; get; }
        public static Queue? ParseCreate(DataObject dataObject, WordScanner word, NameSpace nameSpace)
        {
            // queue_dimension          ::= [ $ [ : constant_expression] ]

            if (word.Text != "$") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // $

            if (word.Text == "]")
            {
                word.MoveNext(); // ]
                return Create(dataObject, null);
            }

            if (word.Text == ":")
            {
                word.MoveNext();
                Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (word.Text == "]")
                {
                    word.MoveNext(); // ]
                    return Create(dataObject, expression);
                }
                else
                {
                    word.AddError("] expected");
                    return Create(dataObject, null);
                }
            }
            word.AddError("] expected");
            return null;
        }

        public static Queue Create(DataObject dataObject, Expressions.Expression? maxSizeExpression)
        {
            Queue queue = new Queue() { DataObject = dataObject, Name = dataObject.Name, MaxSizeExpression = maxSizeExpression };

            ArrayMethods.AddSizeOrNum(queue.NamedElements, "size");          // function int size();
            ArrayMethods.AddDeleteWithOptionalIndex(queue.NamedElements);     // function void delete([input integer index]);
            ArrayMethods.AddInsert(queue.NamedElements, dataObject);          // function void insert(input integer index, input element_t item);
            ArrayMethods.AddPop(queue.NamedElements, "pop_front");            // function element_t pop_front();
            ArrayMethods.AddPop(queue.NamedElements, "pop_back");             // function element_t pop_back();
            ArrayMethods.AddPush(queue.NamedElements, "push_front", dataObject);  // function void push_front(input element_t item);
            ArrayMethods.AddPush(queue.NamedElements, "push_back", dataObject);   // function void push_back(input element_t item);
            ArrayMethods.AppendArrayLocatorMethods(queue.NamedElements, dataObject);  // section 7.12

            return queue;
        }
        public override DataObject Clone()
        {
            Queue queue = Queue.Create(DataObject.Clone(), MaxSizeExpression);
            // Defined フラグをコピーする (AssociativeArray と同じ理由で必要)
            queue.Defined = this.Defined;
            return queue;
        }

        public override DataObject Clone(string name)
        {
            Queue queue = Create(DataObject.Clone(name), MaxSizeExpression);
            queue.Defined = this.Defined;
            return queue;
        }
    }


}
