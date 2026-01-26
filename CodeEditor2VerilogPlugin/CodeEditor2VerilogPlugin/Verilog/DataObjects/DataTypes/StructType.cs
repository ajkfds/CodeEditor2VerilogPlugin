using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Constants;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class StructType : IDataType, IPartSelectableDataType
    { 
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Struct;
            }
        }
        public bool Packable
        {
            get { return Packed; }
        }
        public virtual bool PartSelectable { get { return Packable; } }

        public int? BitWidth
        {
            get
            {
                int size = 0;
                foreach(Member member in Members.Values)
                {
                    if (member.DatType.BitWidth == null)
                    {
                        return null;
                    }
                    size = size + (int)member.DatType.BitWidth;
                }
                return size;
            }
        }
        public CodeDrawStyle.ColorType ColorType { get { return CodeDrawStyle.ColorType.Variable; } }
        public virtual List<DataObjects.Arrays.PackedArray> PackedDimensions { get; protected set; } = new List<DataObjects.Arrays.PackedArray>();

        public bool Tagged = false; 
        public bool Packed = false;
        public bool Signed = false;

        public Dictionary<string, Member> Members = new Dictionary<string, Member>();

        /*
        data_type ::= // from A.2.2.1
        ... 
        | struct_union [ packed [ signing ] ] "{" struct_union_member { struct_union_member } "}" { packed_dimension }

        struct_union_member ::=   { attribute_instance } [random_qualifier] data_type_or_void list_of_variable_decl_assignments ;
        data_type_or_void ::= data_type | "void"
        struct_union ::= "struct" | "union" [ "tagged" ]          
         */
        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("struct", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if(Packed) label.AppendText(" packed", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if(Signed) label.AppendText(" signed", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if(Members.Count != 0)
            {
                label.AppendText("{\n");
                foreach(Member member in Members.Values)
                {
                    label.AppendText("\t");
                    member.AppendTypeLabel(label);
                    label.AppendText("\n");
                }
                label.AppendText("}");
            }
        }
        public bool IsVector { get { return false; } }

        public IDataType Clone()
        {
            StructType structType = new StructType() { Signed = Signed, Tagged = Tagged };
            foreach (Member member in Members.Values)
            {
                structType.Members.Add(member.Identifier, member.Clone());
            }
            foreach(var packedDimention in PackedDimensions)
            {
                structType.PackedDimensions.Add(packedDimention.Clone());
            }
            return structType;
        }

        public static StructType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "struct" ) System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.AddSystemVerilogError();
            word.MoveNext();

            StructType type = new StructType();

            if(word.Text == "tagged")
            {
                type.Tagged = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (word.Text == "packed")
            {
                type.Packed = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                switch (word.Text)
                {
                    case "signed":
                        type.Signed = true;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        break;
                    case "unsigned":
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        break;
                }
            }

            if (word.Eof | word.Text != "{")
            {
                word.AddError("{ required");
                return null;
            }
            word.MoveNext(); // "{"

            while (!word.Eof | word.Text != "}")
            {
                if (!parseMembers(type, word, nameSpace)) break;

                if (word.Text == ";")
                {
                    word.MoveNext();
//                    if (word.Text == "}") word.AddError("illegal ;");
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

        private static bool parseMembers(StructType struct_, WordScanner word, NameSpace nameSpace)
        {
            /*
            struct_union_member ::=   { attribute_instance } [random_qualifier] data_type_or_void list_of_variable_decl_assignments;
            random_qualifier ::= "rand" | "randc"

            list_of_variable_decl_assignments ::= variable_decl_assignment { , variable_decl_assignment }

            variable_decl_assignment ::=
                variable_identifier { variable_dimension } [ = expression ]
                | dynamic_array_variable_identifier unsized_dimension { variable_dimension }
                [ = dynamic_array_new ]
                | class_variable_identifier [ = class_new ]
            */

            if (word.Text == "rand")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (word.Text == "randc")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            IDataType? dataType = null;
            if (word.Text == "void")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                dataType = DataTypeFactory.ParseCreate(word, nameSpace, null);
            }

            while (!word.Eof)
            {
                if (!General.IsSimpleIdentifier(word.Text)) return false;
                string identifier = word.Text;
                if (dataType != null) word.Color(dataType.ColorType);
                word.MoveNext();

                List<PackedArray> dimensions = new List<PackedArray>();
                if (word.Text == "[")
                {
                    var packedArray = PackedArray.ParseCreate(word, nameSpace);
                    if(packedArray != null) dimensions.Add(packedArray);
                }

                Expressions.Expression? exp = null;
                if (word.Text == "=")
                {
                    word.MoveNext();    // =
                    exp = Expressions.Expression.ParseCreate(word, nameSpace);
                }

                if (dataType == null) return false;

                Member member = new Member()
                {
                    DatType = dataType,
                    Identifier = identifier,
                    Dimentions = dimensions,
                    Value = exp
                };

                if (struct_.Members.ContainsKey(identifier))
                {
                    word.AddError("duplicated");
                }
                else
                {
                    struct_.Members.Add(identifier,member);
                }

                if (word.Text != ",") return true;
                word.MoveNext();
            }
            return true;
        }
        public IDataType? ParsePartSelect(WordScanner word, NameSpace nameSpace)
        {
            if (word.Eof || word.Text != "[") return null;
            if (!Packed) return null;

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

        public class Member
        {
            public Member() { }
            public required string Identifier { get; init; }
            public List<PackedArray> Dimentions = new List<PackedArray>();
            public required IDataType DatType { get; init; }
            public Expressions.Expression? Value;

            public Member Clone()
            {
                Member member = new Member() { Identifier = Identifier, DatType = DatType, Value = Value };
                foreach(var packedArray in Dimentions)
                {
                    member.Dimentions.Add(packedArray.Clone());
                }
                return member;
            }
            public void AppendTypeLabel(ColorLabel label)
            {
                DatType.AppendTypeLabel(label);
                foreach(var packedArray in Dimentions)
                {
                    packedArray.AppendLabel(label);
                    label.AppendText(" ");
                }
                label.AppendText(Identifier, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            }
        }

    }
}

