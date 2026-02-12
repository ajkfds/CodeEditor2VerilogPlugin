using Avalonia.Input;
using CodeEditor2.Data;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static pluginVerilog.Verilog.DataObjects.DataTypes.Enum;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class Queue : DataObject,IArray
    {
        // defined in section 7.10
        protected Queue() { }

        public bool IsValidForNet { get { return false; } }
        public override CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Variable;
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

        public static Queue Create(DataObject dataObject, Expressions.Expression? maxSizeExpression)
        {
            Queue queue = new Queue() { DataObject = dataObject, Name = dataObject.Name, MaxSizeExpression = maxSizeExpression };

            { // function int size();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("size", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("size", returnVal, ports);
                queue.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void insert(input integer index, input element_t item);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("index", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("index", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                port = Port.Create("item", null, Port.DirectionEnum.Input, dataObject);
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("insert", null, ports);
                queue.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void delete([input integer index] );
            }

            //{ // function element_t pop_front();
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = dataObject;
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("pop_front", returnVal, ports);
            //    queue.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}

            //{ // function element_t pop_back();
            //    List<Port> ports = new List<Port>();
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("pop_back", null, ports);
            //    queue.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}

            { // function void push_front(input element_t item);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("item", null, Port.DirectionEnum.Input, dataObject);
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("push_front", null, ports);
                queue.NamedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void push_back(input element_t item);
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
