using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Constants;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class Enum : IDataType, IPartSelectableDataType
    {
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Enum;
            }
        }
        public bool Packable
        {
            get { return true; }
        }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public bool IsValidForNet { get { return true; } }
        public IDataType? BaseType { get; protected set; } = null;
        public List<Item> Items = new List<Item>();
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();
        public int? BitWidth
        {
            get
            {
                return BaseType?.BitWidth;
            }
        }
        public virtual bool PartSelectable { get { return true; } }

        public void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(CreateString() + " ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
        }

        public virtual string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("enum ");

            if (BaseType != null)
            {
                sb.Append(BaseType.CreateString());
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public bool IsVector { get { return false; } }

        public IDataType Clone()
        {
            Enum enum_ = new Enum() { BaseType = BaseType.Clone() };
            List<PackedArray> array = new List<PackedArray>();
            foreach (var packedDimension in PackedDimensions)
            {
                enum_.PackedDimensions.Add(packedDimension.Clone());
            }
            foreach (var item in Items)
            {
                enum_.Items.Add(item.Clone());
            }

            return enum_;
        }
        public static Enum ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "enum") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.AddSystemVerilogError();
            word.MoveNext();

            Enum type = new Enum();

            // baseType
            switch (word.Text)
            {
                //integer_vector_type::= bit | logic | reg
                case "bit":
                case "logic":
                case "reg":
                    type.BaseType = IntegerVectorType.ParseCreate(word, nameSpace);
                    break;
                //integer_atom_type::= byte | shortint | int | longint | integer | time
                case "byte":
                case "shortint":
                case "int":
                case "longint":
                case "integer":
                case "time":
                    type.BaseType = IntegerAtomType.ParseCreate(word, nameSpace);
                    break;
                default:
                    // In the absence of a data type declaration, the default data type shall be int.
                    // Any other data type used with enumerated types shall require an explicit data type declaration.
                    type.BaseType = IntType.Create(false);
                    break;
            }

            if (word.Eof | word.Text != "{")
            {
                word.AddError("{ required");
                return null;
            }
            word.MoveNext(); // "{"

            int index = 0;
            while (!word.Eof | word.Text != "}")
            {
                if (!parseItem(type, word, nameSpace, ref index)) break;

                if (word.Text == ",")
                {
                    word.MoveNext();
                    if (word.Text == "}") word.AddError("illegal comma");
                }
            }

            if (word.Eof | word.Text != "}")
            {
                word.AddError("{ required");
                return null;
            }
            word.MoveNext();

            return type;
        }

        private static bool parseItem(Enum enum_, WordScanner word, NameSpace nameSpace, ref int index)
        {
            /*
            enum_name_declaration::=
                enum_identifier[ [integral_number[ : integral_number]] ] [ = constant_expression ]
            */
            if (word.Text == "}" | word.Text == ",") return false;
            if (!General.IsIdentifier(word.Text)) return false;

            Item item = new Item();
            item.Identifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Parameter);

            WordReference wordReference = word.GetReference();

            word.MoveNext();

            PackedArray? range = null;
            if (word.Text == "[")
            {
                range = PackedArray.ParseCreate(word, nameSpace);
            }

            Expressions.Expression? exp = null;
            if (word.Text == "=")
            {
                word.MoveNext();    // =
                exp = Expressions.Expression.ParseCreate(word, nameSpace);
            }

            if (exp != null)
            {
                int.TryParse(exp.ConstantValueString(), out index);
            }

            item.Index = index;
            if (enum_.BaseType != null)
            {
                EnumConstants constants = EnumConstants.Create(item.Identifier, enum_.BaseType, wordReference, Expressions.Expression.CreateTempExpression(index.ToString()));
                if (word.Prototype)
                {
                    if (!nameSpace.NamedElements.ContainsKey(constants.Name))
                    {
                        nameSpace.NamedElements.Add(constants.Name, constants);
                    }
                    else
                    {
                        wordReference.AddError("duplicated");
                    }
                }
                else
                {
                    constants.Defined = true;
                    if (!nameSpace.NamedElements.ContainsKey(constants.Name))
                    {
                        nameSpace.NamedElements.Add(constants.Name, constants);
                    }
                    else
                    {
                        nameSpace.NamedElements.Replace(constants.Name, constants);
                    }
                }
            }

            enum_.Items.Add(item);

            index++;
            return true;
        }
        /*
。
5. num()
プロトタイプ: 
引数: なし。
戻り値: その列挙型で定義されている要素の数を整数（int）で返します
。
6. name()
プロトタイプ: function string name();
引数: なし。
戻り値: 現在の列挙値の**名前を表す文字列（string）**を返します
。
現在の値が列挙型のメンバーではない場合、空の文字列を返します
。 
 */
        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            { // function void first();
                List<Port> ports = new List<Port>();
                BuiltInMethod builtInMethod = BuiltInMethod.Create("first", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function void last();
                List<Port> ports = new List<Port>();
                BuiltInMethod builtInMethod = BuiltInMethod.Create("last", null, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function enum next( int unsigned N = 1 )
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("N", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("N", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                Variables.Variable returnVal = DataObjects.Variables.Enum.Create("next", this);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("next", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function enum prev( int unsigned N = 1 )
                List<Port> ports = new List<Port>();
                Port? port = Port.Create("N", null, Port.DirectionEnum.Input, DataObjects.Variables.Int.Create("N", DataTypes.IntType.Create(false)));
                if (port != null) ports.Add(port);
                Variables.Variable returnVal = DataObjects.Variables.Enum.Create("prev", this);
                BuiltInMethod builtInMethod = BuiltInMethod.Create("prev", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function int num()
                List<Port> ports = new List<Port>();
                Variables.Variable returnVal = DataObjects.Variables.Int.Create("num", DataTypes.IntType.Create(false));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("num", returnVal, ports);
                namedElements.Add(builtInMethod.Name, builtInMethod);
            }

            { // function string name()
                List<Port> ports = new List<Port>();
                Variables.Variable returnVal = DataObjects.Variables.String.Create("name", DataTypes.StringType.Create(null));
                BuiltInMethod builtInMethod = BuiltInMethod.Create("name", returnVal, ports);
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
                return DataObjects.DataTypes.LogicType.Create(false, packedDimensions);
            }
            else
            {
                List<PackedArray> packedDimensions = new List<PackedArray>();
                packedDimensions.Add(new PackedArray(rangeExpression.BitWidth));
                return DataObjects.DataTypes.LogicType.Create(false, packedDimensions);
            }
        }
        public class Item
        {
            public string Identifier;
            public int Index;

            public Item Clone()
            {
                return new Item() { Identifier = Identifier, Index = Index };
            }
        }

    }
}
