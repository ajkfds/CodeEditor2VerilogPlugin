using Avalonia.Input;
using CodeEditor2.Data;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class Queue : DataObject,IArray
    {
        // defined in section 7.10
        protected Queue() { }

        public int? Size { get; protected set; } = null;
        public bool Constant { get; protected set; } = false;

        public Expressions.Expression? MaxSizeExpression { get; protected set; }

        //public override bool CheckIndexRangeError(Expressions.Expression indexExpression)
        //{
        //    return false;
        //}
        //public override string CreateString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("[ $");
        //    if (MaxSizeExpression != null)
        //    {
        //        sb.Append(":");
        //        sb.Append(MaxSizeExpression.CreateString());
        //    }
        //    sb.Append(" ]");
        //    return sb.ToString();
        //}

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
                    return Create(dataObject,expression);
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

        /* queue methods
        function int size(); 
        function void insert(input integer index, input element_t item);
        function void delete( [input integer index] );
        function element_t pop_front();
        function element_t pop_back();
        function void push_front(input element_t item);
        function void push_back(input element_t item);
         */
        public static Queue Create(DataObject dataObject,Expressions.Expression? maxSizeExpression)
        {
            Queue queue= new Queue() { DataObject = dataObject, Name = dataObject.Name, MaxSizeExpression = maxSizeExpression};
            
            {
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("item", null, Port.DirectionEnum.Input, dataObject);
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("push_back", null, ports);
                queue.NamedElements.Add(builtInMethod.Name,builtInMethod);
            }


            return queue;
        }

        public override DataObject Clone()
        {
            Queue queue = Queue.Create(DataObject.Clone(), MaxSizeExpression);
           return queue;
        }

        public override DataObject Clone(string name)
        {
            Queue queue = Create(DataObject.Clone(name), MaxSizeExpression);
            return queue;
        }
    }


}
