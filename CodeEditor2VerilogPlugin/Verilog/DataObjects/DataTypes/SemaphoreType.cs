using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    // SystemVerilog built-in semaphore type (IEEE 1800-2017 section 20.4)
    public class SemaphoreType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Semaphore;
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
            label.AppendText("semaphore ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public IDataType Clone()
        {
            return SemaphoreType.Create();
        }
        public static SemaphoreType Create()
        {
            SemaphoreType semaphoreType = new SemaphoreType();
            return semaphoreType;
        }
        public static SemaphoreType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            SemaphoreType dType = new SemaphoreType();
            if (word.Text != "semaphore") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }

        // IEEE 1800-2017 20.4 semaphore built-in methods
        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            { // function int get(int keyCount = 1);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("keyCount", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("keyCount", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("get", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("get", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int try_get(int keyCount = 1);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("keyCount", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("keyCount", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("try_get", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("try_get", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void put(int keyCount = 1);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("keyCount", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("keyCount", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("put", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
        }

    }
}
