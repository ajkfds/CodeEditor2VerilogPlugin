using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Constants;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    public class Struct : IDataType
    { 
        public virtual DataTypeEnum Type
        {
            get
            {
                return DataTypeEnum.Struct;
            }
        }

        public bool Tagged = false; 
        public bool Packed = false;
        public bool Signed = false;

        public List<Member> Members = new List<Member>();

        /*
        data_type ::= // from A.2.2.1
        ... 
        | struct_union [ packed [ signing ] ] "{" struct_union_member { struct_union_member } "}" { packed_dimension }

        struct_union_member ::=   { attribute_instance } [random_qualifier] data_type_or_void list_of_variable_decl_assignments ;
        data_type_or_void ::= data_type | "void"
        struct_union ::= "struct" | "union" [ "tagged" ]          
         */
        public virtual string CreateString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("struct ");

            if (Packed) sb.Append("packed ");
            if (Signed) sb.Append("signed ");

            return sb.ToString();
        }

        public static Struct? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "struct" ) System.Diagnostics.Debugger.Break();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.AddSystemVerilogError();
            word.MoveNext();

            Struct type = new Struct();

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

            int index = 0;
            while (!word.Eof | word.Text != "}")
            {
                if (!parseMembers(type, word, nameSpace)) break;

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

        private static bool parseMembers(Struct struct_, WordScanner word, NameSpace nameSpace)
        {
            /*
            struct_union_member ::=   { attribute_instance } [random_qualifier] data_type_or_void list_of_variable_decl_assignments;
            random_qualifier ::= "rand" | "randc"
            */

            if (word.Text == "}" | word.Text == ",") return false;
            if (!General.IsIdentifier(word.Text)) return false;

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
                dataType = DataType.ParseCreate(word, nameSpace, null);
            }

            while (!word.Eof)
            {
                if (!General.IsSimpleIdentifier(word.Text)) return false;
                string identifier = word.Text;
                word.Color(CodeDrawStyle.ColorType.Parameter);
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

                Member member = new Member()
                {
                    DatType = dataType,
                    Identifier = identifier,
                    PackedArray = range,
                    Value = exp
                };

                struct_.Members.Add(member);
            }
            return true;
        }

        public class Member
        {


            public string Identifier;
            public PackedArray? PackedArray;
            public IDataType? DatType;
            public Expressions.Expression? Value;
        }

    }
}

