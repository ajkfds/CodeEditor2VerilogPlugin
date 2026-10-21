using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class StructType : IDataType, IPartSelectableDataType
    {
        protected StructType() { }
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

        public bool IsValidForNet
        {
            get
            {
                foreach (var member in Members.Values)
                {
                    if (!member.DatType.IsValidForNet) return false;
                }
                return true;
            }
        }
        public virtual int? BitWidth
        {
            get
            {
                int size = 0;
                foreach (Member member in Members.Values)
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
        public bool IsUnion = false;

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

        public virtual void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText(IsUnion ? "union" : "struct", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (Packed) label.AppendText(" packed", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (Signed) label.AppendText(" signed", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            if (Members.Count != 0)
            {
                label.AppendText("{\n");
                foreach (Member member in Members.Values)
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
            foreach (var packedDimention in PackedDimensions)
            {
                structType.PackedDimensions.Add(packedDimention.Clone());
            }
            return structType;
        }

        public static StructType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "struct") System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.AddSystemVerilogError();
            word.MoveNext();

            StructType type = new StructType();
            type.IsUnion = false;
            return parseCommon(type, word, nameSpace);
        }

        protected static StructType? parseCommon(StructType type, WordScanner word, NameSpace nameSpace)
        {

            if (word.Text == "tagged")
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

            while (!word.Eof)
            {
                if (word.Text == "]") break;
                IndexReference iref = word.CreateIndexReference();

                if (!parseMembers(type, word, nameSpace)) break;

                if (word.Text == ";")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("illegal ;");
                }

                // Stop only on structural end keywords; type keywords such as
                // "reg" / "logic" legitimately start the next member and must not
                // terminate the struct body.
                if (word.Text == "endmodule" || word.Text == "endpackage"
                    || word.Text == "endinterface" || word.Text == "endclass"
                    || word.Text == "endprogram" || word.Text == "endfunction"
                    || word.Text == "endtask" || word.Text == "endgenerate"
                    || word.Text == "endcase" || word.Text == "endchecker") break;
                if (word.CreateIndexReference().IsSameAs(iref))
                {
                    word.MoveNext();
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

        /// <summary>
        /// Members of packed structures shall not be assigned individual default
        /// member values. The same restriction applies to every member of an
        /// unpacked structure that contains a union (IEEE 1800-2017 7.2.2).
        /// </summary>
        private static bool isMemberDefaultValueIllegal(StructType struct_)
        {
            if (struct_.Packed) return true;
            if (struct_.IsUnion) return false;
            foreach (Member member in struct_.Members.Values)
            {
                IDataType memberType = member.DatType;
                if (memberType is UnionType) return true;
                if (memberType is UserDefinedType userDefinedType
                    && userDefinedType.OriginalDataType is UnionType) return true;
            }
            return false;
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
            bool isVoid = false;
            if (word.Text == "void")
            {
                // data_type_or_void ::= "void" (valid only in tagged union members)
                isVoid = true;
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
                WordReference memberReference = word.GetReference();
                if (dataType != null) word.Color(dataType.ColorType);
                word.MoveNext();

                List<PackedArray> dimensions = new List<PackedArray>();
                if (word.Text == "[")
                {
                    // struct_union_member ::= data_type_or_void
                    //   list_of_variable_decl_assignments ;
                    //   variable_decl_assignment ::=
                    //     variable_identifier { variable_dimension }
                    // The dimension after a member identifier is a
                    // variable_dimension (unpacked / associative / queue), not a
                    // packed range. A simple "[N]" (e.g. "int b[4];") is an
                    // unpacked dimension; only "[m:n]" is a packed range. Accept
                    // both so "int a, b[4];" parses (previously PackedArray
                    // required ":" and reported ": expected").
                    WordScanner probe = word.Clone(false);
                    probe.MoveNext(); // [
                    bool hasColon = false;
                    int depth = 0;
                    while (!probe.Eof)
                    {
                        if (probe.Text == "[") depth++;
                        else if (probe.Text == "]")
                        {
                            if (depth == 0) break;
                            depth--;
                        }
                        else if (probe.Text == ":" && depth == 0) { hasColon = true; break; }
                        probe.MoveNext();
                    }
                    probe.Dispose();

                    if (hasColon)
                    {
                        var packedArray = PackedArray.ParseCreate(word, nameSpace);
                        if (packedArray != null) dimensions.Add(packedArray);
                    }
                    else if (word.NextText == "*")
                    {
                        // unsized_dimension "[*]" (associative array declared
                        // inside a struct member, IEEE 1800-2017 A.2.5:
                        // variable_dimension ::= unsized_dimension | ...).
                        // "int I[*];" previously fell into the expression
                        // branch and reported "] expected" because "*" is not
                        // an expression. Consume "[", "*", "]" as a dimension.
                        word.MoveNext(); // [
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext(); // *
                        if (word.Text == "]") word.MoveNext();
                        else word.AddError("] expected");
                    }
                    else
                    {
                        // unpacked dimension "[expr]" (accept and consume)
                        word.MoveNext(); // [
                        Expressions.Expression? dimExpr = Expressions.Expression.ParseCreate(word, nameSpace);
                        if (word.Text == "]") word.MoveNext();
                        else word.AddError("] expected");
                    }
                }

                Expressions.Expression? exp = null;
                if (word.Text == "=")
                {
                    word.MoveNext();    // =
                    exp = Expressions.Expression.ParseCreate(word, nameSpace);
                }

                // Members of packed structures shall not be assigned individual
                // default member values (IEEE 1800-2017 7.2.2). The same
                // restriction applies to all members of an unpacked structure
                // that contains a union.
                if (exp != null && !word.Prototype && isMemberDefaultValueIllegal(struct_))
                {
                    memberReference.AddError(
                        "members of packed structures shall not be assigned individual default member values");
                }

                if (isVoid)
                {
                    // void member (tagged union): consume identifier, register nothing
                    if (word.Text != ",") return true;
                    word.MoveNext();
                    continue;
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
                    struct_.Members.Add(identifier, member);
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

        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            foreach (var member in Members.Values)
            {
                var dataObject = DataObject.Create(member.Identifier, member.DatType);
                dataObject.Defined = true;
                namedElements.Add(dataObject.Name, dataObject);
            }
        }

        public class Member
        {
            public Member() { }
            public required string Identifier { get; init; }
            public List<PackedArray> Dimentions = new List<PackedArray>();
            public required IDataType DatType { get; init; }
            public Expressions.Expression? Value;

            /// <summary>
            /// Get the bit offset and width of this member within the packed struct.
            /// </summary>
            /// <param name="structType">The parent StructType</param>
            /// <returns>Tuple of (offset, width) or null if struct is not packed or member not found</returns>
            public (long offset, int width)? GetMemberBitInfo(StructType structType)
            {
                if (!structType.Packed) return null;

                long offset = 0;
                foreach (var member in structType.Members.Values)
                {
                    if (member.Identifier == Identifier)
                    {
                        int width = member.DatType.BitWidth ?? 1;
                        return (offset, width);
                    }
                    // Add member width to offset
                    int memberWidth = member.DatType.BitWidth ?? 1;
                    offset += memberWidth;
                }
                return null;
            }

            public Member Clone()
            {
                Member member = new Member() { Identifier = Identifier, DatType = DatType, Value = Value };
                foreach (var packedArray in Dimentions)
                {
                    member.Dimentions.Add(packedArray.Clone());
                }
                return member;
            }
            public void AppendTypeLabel(ColorLabel label)
            {
                DatType.AppendTypeLabel(label);
                foreach (var packedArray in Dimentions)
                {
                    packedArray.AppendLabel(label);
                    label.AppendText(" ");
                }
                label.AppendText(Identifier, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Identifier));
            }
        }

    }
}

