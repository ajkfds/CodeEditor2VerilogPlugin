using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    /// <summary>
    /// Virtual Interface Type
    /// IEEE 1800-2017 SystemVerilog
    /// 
    /// virtual_interface_type ::=
    ///     "virtual" [ "interface" ] interface_identifier [ parameter_value_assignment ] [ . modport_identifier ]
    /// 
    /// data_type ::=
    ///     ...
    ///     | "virtual" [ "interface" ] interface_identifier [ parameter_value_assignment ] [ . modport_identifier ]
    ///     ...
    /// </summary>
    public class VirtualInterfaceType : IDataType
    {
        public DataTypeEnum Type => DataTypeEnum.Virtual;

        /// <summary>
        /// Interface identifier
        /// </summary>
        public string InterfaceIdentifier { get; set; } = "";

        /// <summary>
        /// Optional parameter overrides
        /// </summary>
        public Dictionary<string, Expression> ParameterOverrides { get; set; } = new Dictionary<string, Expression>();

        /// <summary>
        /// Optional modport identifier
        /// </summary>
        public string? ModportIdentifier { get; set; }

        /// <summary>
        /// Packed dimensions (not typically used with virtual interface)
        /// </summary>
        public List<DataObjects.Arrays.PackedArray> PackedDimensions { get; set; } = new List<DataObjects.Arrays.PackedArray>();

        /// <summary>
        /// Bit width (typically null for virtual interface as it's a handle)
        /// </summary>
        public int? BitWidth => null;

        /// <summary>
        /// Whether this is 4-state (typically false for virtual interface)
        /// </summary>
        public bool State4 => false;

        DataTypeEnum IDataType.Type => DataObjects.DataTypes.DataTypeEnum.Virtual;

        bool IDataType.Packable => false;

        bool IDataType.PartSelectable => false;

        bool IDataType.IsVector => false;

        int? IDataType.BitWidth => null;

        CodeDrawStyle.ColorType IDataType.ColorType => CodeDrawStyle.ColorType.Variable;

        List<PackedArray> packedArrays = new List<PackedArray>();
        List<PackedArray> IDataType.PackedDimensions => packedArrays;

        bool IDataType.IsValidForNet => false;
        public static VirtualInterfaceType? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "virtual")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // virtual

            VirtualInterfaceType vit = new VirtualInterfaceType();

            // Optional "interface" keyword
            if (word.Text == "interface")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            // interface_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("interface identifier expected");
                return vit;
            }

            vit.InterfaceIdentifier = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Optional parameter_value_assignment
            if (word.Text == "#")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext(); // (

                    while (!word.Eof && word.Text != ")")
                    {
                        if (word.Text == ")")
                        {
                            break;
                        }

                        if (General.IsIdentifier(word.Text) && word.NextText == "=")
                        {
                            // Named parameter
                            string paramName = word.Text;
                            word.MoveNext(); // param name
                            word.MoveNext(); // =
                            Expression? expr = Expression.ParseCreate(word, nameSpace);
                            if (expr != null)
                            {
                                vit.ParameterOverrides[paramName] = expr;
                            }
                        }
                        else
                        {
                            // Ordered parameter
                            Expression? expr = Expression.ParseCreate(word, nameSpace);
                        }

                        if (word.Text == ",")
                        {
                            word.MoveNext();
                        }
                    }

                    if (word.Text == ")")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(") expected");
                    }
                }
            }

            // Optional . modport_identifier
            if (word.Text == ".")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (General.IsIdentifier(word.Text))
                {
                    vit.ModportIdentifier = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    word.AddError("modport identifier expected");
                }
            }

            return vit;
        }

        void IDataType.AppendTypeLabel(ColorLabel label)
        {
            throw new System.NotImplementedException();
        }

        IDataType IDataType.Clone()
        {
            throw new System.NotImplementedException();
        }

        string IDataType.CreateString()
        {
            throw new System.NotImplementedException();
        }
    }
}
