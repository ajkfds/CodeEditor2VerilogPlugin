using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    // SystemVerilog built-in process class type (IEEE 1800-2017 section 9.7)
    public class ProcessType : IDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Process;
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
            label.AppendText("process ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public IDataType Clone()
        {
            return ProcessType.Create();
        }
        public static ProcessType Create()
        {
            ProcessType processType = new ProcessType();
            return processType;
        }
        public static ProcessType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            ProcessType dType = new ProcessType();
            if (word.Text != "process") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
       public bool IsVector { get { return false; } }

       // IEEE 1800-2017 9.7 process class members (shared with Verilog.ProcessClass static scope)
       public void AppendChiledNamedElements(NamedElements namedElements)
       {
           AppendBuiltinMethods(namedElements);
       }

       public static void AppendBuiltinMethods(NamedElements namedElements)
       {
           { // static function process self();
               List<Port> ports = new List<Port>();
               Variable returnVal = DataObjects.Variables.Process.Create("self", ProcessType.Create());
               BuiltInMethod builtInMethod = BuiltInMethod.Create("self", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // process state constants: FINISHED / RUNNING / WAITING / SUSPENDED / KILLED
               // (modeled as zero-argument built-in methods with int return value;
               //  they can be referenced without parentheses like process::FINISHED)
               foreach (string stateName in new string[] { "FINISHED", "RUNNING", "WAITING", "SUSPENDED", "KILLED" })
               {
                   List<Port> ports = new List<Port>();
                   Variable returnVal = DataObjects.Variables.Int.Create(stateName, DataTypes.IntType.Create(false));
                   BuiltInMethod builtInMethod = BuiltInMethod.Create(stateName, returnVal, ports);
                   namedElements.Add(builtInMethod.Name, builtInMethod);
               }
           }

           { // status property : function process status() equivalent (int return)
               List<Port> ports = new List<Port>();
               Variable returnVal = DataObjects.Variables.Int.Create("status", DataTypes.IntType.Create(false));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("status", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // task await();
               List<Port> ports = new List<Port>();
               BuiltInMethod builtInMethod = BuiltInMethod.Create("await", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void kill();
               List<Port> ports = new List<Port>();
               BuiltInMethod builtInMethod = BuiltInMethod.Create("kill", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void suspend();
               List<Port> ports = new List<Port>();
               BuiltInMethod builtInMethod = BuiltInMethod.Create("suspend", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void resume();
               List<Port> ports = new List<Port>();
               BuiltInMethod builtInMethod = BuiltInMethod.Create("resume", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // task srandom(int seed);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("seed", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("seed", DataTypes.IntType.Create(false)));
               if (port != null) ports.Add(port);
               BuiltInMethod builtInMethod = BuiltInMethod.Create("srandom", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function string get_randstate();
               List<Port> ports = new List<Port>();
               Variable returnVal = DataObjects.Variables.String.Create("get_randstate", DataTypes.StringType.Create(null));
               BuiltInMethod builtInMethod = BuiltInMethod.Create("get_randstate", returnVal, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }

           { // function void set_randstate(string s);
               List<Port> ports = new List<Port>();
               Port? port = Port.Create("s", null, Port.DirectionEnum.Input, DataObjects.Variables.String.Create("s", DataTypes.StringType.Create(null)));
               if (port != null) ports.Add(port);
               BuiltInMethod builtInMethod = BuiltInMethod.Create("set_randstate", null, ports);
               namedElements.Add(builtInMethod.Name, builtInMethod);
           }
       }

   }
}
