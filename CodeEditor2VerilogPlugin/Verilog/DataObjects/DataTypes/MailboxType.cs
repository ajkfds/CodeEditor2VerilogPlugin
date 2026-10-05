using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    // SystemVerilog built-in mailbox type (IEEE 1800-2017 section 15)
    public class MailboxType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Mailbox;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public int? BitWidth { get; } = null;
        public virtual bool PartSelectable { get { return false; } }
        public bool IsValidForNet { get { return false; } }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<Arrays.PackedArray>();
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("mailbox ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public IDataType Clone()
        {
            return MailboxType.Create();
        }
        public static MailboxType Create()
        {
            MailboxType mailboxType = new MailboxType();
            return mailboxType;
        }
        public static MailboxType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            MailboxType dType = new MailboxType();
            if (word.Text != "mailbox") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
       public bool IsVector { get { return false; } }

       // IEEE 1800-2017 15.4 mailbox methods
       public void AppendChiledNamedElements(NamedElements namedElements)
       {
           { // function int num();
               List<Port> ports = new List<Port>();
               Variable returnVal = DataObjects.Variables.Int.Create("num", DataTypes.IntType.Create(false));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("num", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void put(message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               BuiltInMethod builtInMethod = BuiltInMethod.Create("put", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void get(ref message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               BuiltInMethod builtInMethod = BuiltInMethod.Create("get", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function int try_put(message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               Variable returnVal = DataObjects.Variables.Int.Create("try_put", DataTypes.IntType.Create(false));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("try_put", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function int try_get(ref message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               Variable returnVal = DataObjects.Variables.Int.Create("try_get", DataTypes.IntType.Create(false));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("try_get", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function int try_peek(ref message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               Variable returnVal = DataObjects.Variables.Int.Create("try_peek", DataTypes.IntType.Create(false));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("try_peek", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void peek(ref message_t message);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("message", null, Port.DirectionEnum.Ref, DataObjects.Variables.Integer.Create("message", DataTypes.IntegerType.Create(false)));
               if (port != null) ports.Add(port);
               BuiltInMethod builtInMethod = BuiltInMethod.Create("peek", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }
       }

   }
}
