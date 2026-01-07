
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

        public override int? BitWidth {
            get
            {
                if (Length == null) return null;
                return Length * 8;
            }
        }

        public int? Length = null;
        public static new String Create(string name,IDataType dataType)
        {
            System.Diagnostics.Debug.Assert(dataType.Type == DataTypeEnum.String);

            String val = new String() { Name = name };
            val.DataType = dataType;

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
                { //function int len();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Int.Create("len", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("len", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void putc(int i, byte c);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    port = Port.Create("c", null, Port.DirectionEnum.Input, DataObjects.Variables.Byte.Create("c", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Byte, false)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("putc", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function byte getc(int i);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    Variable returnVal = DataObjects.Variables.Byte.Create("getc", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Byte, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("getc", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function string toupper();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.String.Create("toupper", DataTypes.StringType.Create(null));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("toupper", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function string tolower();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.String.Create("tolower", DataTypes.StringType.Create(null));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("tolower", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function int compare(string s);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("s", null, Port.DirectionEnum.Input, DataObjects.Variables.String.Create("s", DataTypes.StringType.Create(null)));
                    if (port != null) ports.Add(port);
                    Variable returnVal = DataObjects.Variables.Int.Create("compare", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("compare", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function int icompare(string s);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("s", null, Port.DirectionEnum.Input, DataObjects.Variables.String.Create("s", DataTypes.StringType.Create(null)));
                    if (port != null) ports.Add(port);
                    Variable returnVal = DataObjects.Variables.Int.Create("icompare", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("icompare", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function string substr(int i, int j);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    port = Port.Create("j", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("j", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Int, false)));
                    if (port != null) ports.Add(port);
                    Variable returnVal = DataObjects.Variables.String.Create("substr", DataTypes.StringType.Create(null));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("substr", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function integer atoi();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Integer.Create("atoi", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("atoi", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function integer atohex();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Integer.Create("atohex", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("atohex", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function integer atooct();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Integer.Create("atooct", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("atooct", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function integer atobin();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Integer.Create("atobin", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("atobin", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function real atoreal();
                    List<Port> ports = new List<Port>();
                    Variable returnVal = DataObjects.Variables.Real.Create("atoreal", DataTypes.RealType.Create(null));
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("atoreal", returnVal, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void itoa(integer i);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("itoa", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void hextoa(integer i);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("hextoa", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void octtoa(integer i);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("octtoa", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void bintoa(integer i);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerAtomType.Create(DataTypes.DataTypeEnum.Integer, false)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("bintoa", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                { //function void realtoa(real r);
                    List<Port> ports = new List<Port>();
                    Port? port = Port.Create("r", null, Port.DirectionEnum.Input, DataObjects.Variables.Real.Create("i", DataTypes.RealType.Create(null)));
                    if (port != null) ports.Add(port);
                    BuiltInMethod builtInMethod = BuiltInMethod.Create("realtoa", null, ports);
                    namedElements.Add(builtInMethod.Name, builtInMethod);
                }
                return namedElements;
            }
        }

        public override Variable Clone()
        {
            String val = new String() { Name = Name,Defined=Defined };
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
