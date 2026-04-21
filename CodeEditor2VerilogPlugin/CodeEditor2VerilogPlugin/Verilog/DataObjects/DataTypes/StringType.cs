using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class StringType : IDataType, IPartSelectableDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.String;
            }
        }
        public bool IsValidForNet { get { return false; } }
        public int? BitWidth
        {
            get
            {
                return null;
            }
        }
        public bool Packable
        {
            get { return false; }
        }
        public virtual bool PartSelectable { get { return true; } }
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("string", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public IDataType Clone()
        {
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                array.Add(packedDimension.Clone());
            }
            return StringType.Create(array);
        }
        public static StringType Create(List<Arrays.PackedArray>? packedDimensions)
        {
            StringType stringType = new StringType() { };
            if (packedDimensions == null)
            {
                stringType.PackedDimensions.Clear();
            }
            else
            {
                stringType.PackedDimensions = packedDimensions;
            }
            return stringType;
        }
        public static StringType ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            StringType dType = new StringType();
            if (word.Text != "string") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();
            return dType;
        }
        public bool IsVector { get { return false; } }
        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            { //function int len();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Int.Create("len", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("len", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function void putc(int i, byte c);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                port = Port.Create("c", null, Port.DirectionEnum.Input, DataObjects.Variables.Byte.Create("c", DataTypes.ByteType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("putc", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function byte getc(int i);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Byte.Create("getc", DataTypes.ByteType.Create(false));
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
                Variable returnVal = DataObjects.Variables.Int.Create("compare", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("compare", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function int icompare(string s);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("s", null, Port.DirectionEnum.Input, DataObjects.Variables.String.Create("s", DataTypes.StringType.Create(null)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.Int.Create("icompare", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("icompare", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function string substr(int i, int j);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("i", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                port = Port.Create("j", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("j", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                Variable returnVal = DataObjects.Variables.String.Create("substr", DataTypes.StringType.Create(null));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("substr", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function integer atoi();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Integer.Create("atoi", DataTypes.IntegerType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("atoi", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function integer atohex();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Integer.Create("atohex", DataTypes.IntegerType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("atohex", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function integer atooct();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Integer.Create("atooct", DataTypes.IntegerType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("atooct", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function integer atobin();
                List<Port> ports = new List<Port>();
                Variable returnVal = DataObjects.Variables.Integer.Create("atobin", DataTypes.IntegerType.Create(false));
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
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("itoa", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function void hextoa(integer i);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("hextoa", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function void octtoa(integer i);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerType.Create(false)));
                if (port != null) ports.Add(port);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("octtoa", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }
            { //function void bintoa(integer i);
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("i", null, Port.DirectionEnum.Input, DataObjects.Variables.Integer.Create("i", DataTypes.IntegerType.Create(false)));
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

        }

        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            if (word.Eof || word.Text != "[") return null;

            RangeExpression? rangeExpression = RangeExpression.ParseCreate(word, nameSpace);
            if (rangeExpression == null) return null;

            if (rangeExpression is SingleBitRangeExpression)
            {
                SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                if (!word.Prototype && singleBitRangeExpression.BitIndex != null)
                {
                    if (singleBitRangeExpression.BitIndex < 0 || singleBitRangeExpression.BitIndex >= BitWidth)
                    {
                        singleBitRangeExpression.WordReference.AddError("index out of range");
                    }
                }

                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(1));
                return DataObjects.DataTypes.ByteType.Create(false);
            }
            else
            {
                if (!word.Prototype) rangeExpression.WordReference.AddError("use substr method");
                return null;
            }
        }

    }
}
