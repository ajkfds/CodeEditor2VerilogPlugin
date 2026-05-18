using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    /// <summary>
    /// SystemVerilog Event Type
    /// IEEE 1800-2017
    /// 
    /// event_declaration ::= event_identifier { , event_identifier } [ = event_expression ] ;
    /// event_expression ::= hierarchical_identifier 
    ///                    | event_expression or event_expression 
    ///                    | event_expression , event_expression 
    ///                    | @(event_control) 
    ///                    | wait ( expression ) 
    /// 
    /// The event type is used to declare event variables that can be used to trigger processes.
    /// </summary>
    public class EventType : IDataType
    {
        protected EventType() { }

        public static EventType Create()
        {
            return new EventType();
        }

        public DataTypeEnum Type { get { return DataTypeEnum.Event; } }

        public bool Packable { get { return false; } }

        public bool PartSelectable { get { return false; } }

        public bool IsVector { get { return false; } }

        public int? BitWidth { get { return null; } }

        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }

        public List<Arrays.PackedArray> PackedDimensions { get; } = new List<Arrays.PackedArray>();

        public bool IsValidForNet { get { return false; } }

        public string CreateString()
        {
            return "event";
        }

        public void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("event", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }

        public IDataType Clone()
        {
            return new EventType();
        }

        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            // event type has no child named elements
        }
    }
}
