
using Newtonsoft.Json;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    public class String : ValueVariable
    {
        protected String() { 
        }

        public static new String Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.String);

            String val = new String() { Name = name };
            val.DataType = dataType;

            //{ //function int len();
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = DataObjects.Variables.Int.Create("len", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false));
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("len", returnVal, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            //{ //function void putc(int i, byte c);
            //    List<Port> ports = new List<Port>();
            //    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
            //    if (port != null) ports.Add(port);
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("putc", null, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            //{ //function byte getc(int i);
            //}
            ////function string toupper();
            ////function string tolower();
            ////function int compare(string s);
            ////function int icompare(string s);
            //{ //function string substr(int i, int j);
            //    val.NamedElements.Add(substr.Name, substr);
            //}
            //{ //function integer atoi();
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = DataObjects.Variables.Integer.Create("atoi", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("atoi", returnVal, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            //{ //function integer atohex();
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = DataObjects.Variables.Integer.Create("atohex", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("atohex", returnVal, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            //{ //function integer atooct();
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = DataObjects.Variables.Integer.Create("atooct", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("atooct", returnVal, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            ////function integer atobin();
            //{
            //    List<Port> ports = new List<Port>();
            //    Variable returnVal = DataObjects.Variables.Integer.Create("atobin", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
            //    BuiltInMethod builtInMethod = BuiltInMethod.Create("atobin", returnVal, ports);
            //    val.NamedElements.Add(builtInMethod.Name, builtInMethod);
            //}
            ////function real atoreal();
            ////function void itoa(integer i);
            ////function void hextoa(integer i);
            ////function void octtoa(integer i);
            ////function void bintoa(integer i);
            ////function void realtoa(real r);
            return val;
        }

        // substrの戻り値がStringを持つため、遅延評価される必要がある。
        private NamedElements? namedElements = null;
        public override NamedElements NamedElements
        {
            get
            {
                if (namedElements != null) return namedElements;

                namedElements = new NamedElements();
                { //function string substr(int i, int j);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    port = Port.Create("j", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("j", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    Variable returnVal = DataObjects.Variables.String.Create("substr", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.String, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("substr", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }

                return namedElements;
            }
        }

        public override Variable Clone()
        {
            String val = new String() { Name = Name };
            val.DataType = DataType;
            return val;
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText("string ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText(" ");
        }

    }
}
