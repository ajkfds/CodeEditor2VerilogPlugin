using AjkAvaloniaLibs.Controls;
using pluginVerilog.Verilog.DataObjects.Arrays;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.DataTypes
{
    /// <summary>
    /// SystemVerilog type_reference
    /// IEEE 1800-2017
    /// 
    /// type_reference ::= 
    ///     "type" "(" expression ")"
    ///   | "type" "(" data_type ")"
    /// 
    /// The type() construct allows referring to a type by value.
    /// It can be used in:
    /// - Variable declarations: type(var_name)
    /// - Function return types
    /// - Cast expressions
    /// </summary>
    public class TypeReference : IDataType
    {
        public DataTypeEnum Type { get { return DataTypeEnum.TypeReference; } }

        /// <summary>
        /// The referenced data type (when type(data_type) is used)
        /// </summary>
        public IDataType? ReferencedDataType { get; }

        /// <summary>
        /// The referenced expression (when type(expression) is used)
        /// </summary>
        public Expressions.Expression? ReferencedExpression { get; }

        /// <summary>
        /// The resolved type (for expression-based type references)
        /// This is set during analysis when we can determine the type from the expression
        /// </summary>
        public IDataType? ResolvedType { get; set; }

        public bool Packable
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.Packable ?? false;
            }
        }

        public bool PartSelectable
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.PartSelectable ?? false;
            }
        }

        public bool IsValidForNet
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.IsValidForNet ?? false;
            }
        }

        public int? BitWidth
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.BitWidth;
            }
        }

        public CodeDrawStyle.ColorType ColorType
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.ColorType ?? CodeDrawStyle.ColorType.Variable;
            }
        }

        public bool IsVector
        {
            get
            {
                IDataType? type = GetReferencedType();
                return type?.IsVector ?? false;
            }
        }

        public List<PackedArray> PackedDimensions { get; } = new List<PackedArray>();

        public TypeReference(IDataType? referencedDataType, Expressions.Expression? referencedExpression)
        {
            ReferencedDataType = referencedDataType;
            ReferencedExpression = referencedExpression;
        }

        /// <summary>
        /// Get the actual type being referenced
        /// </summary>
        /// <returns>The underlying data type, or null if not available</returns>
        public IDataType? GetReferencedType()
        {
            if (ReferencedDataType != null)
            {
                return ReferencedDataType;
            }

            // For type(expression), return the resolved type if available
            return ResolvedType;
        }

        public void AppendTypeLabel(ColorLabel label)
        {
            label.AppendText("type", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            label.AppendText("(");
            if (ReferencedDataType != null)
            {
                ReferencedDataType.AppendTypeLabel(label);
            }
            else if (ResolvedType != null)
            {
                ResolvedType.AppendTypeLabel(label);
            }
            label.AppendText(")");
        }

        public void AppendChiledNamedElements(NamedElements namedElements)
        {
            IDataType? type = GetReferencedType();
            type?.AppendChiledNamedElements(namedElements);
        }

        public IDataType Clone()
        {
            TypeReference clone = new TypeReference(
                ReferencedDataType?.Clone(),
                null // Expressions are not cloned here
            );
            clone.ResolvedType = ResolvedType?.Clone();
            foreach (var packedArray in PackedDimensions)
            {
                clone.PackedDimensions.Add(packedArray.Clone());
            }
            return clone;
        }

        public string CreateString()
        {
            ColorLabel label = new ColorLabel();
            AppendTypeLabel(label);
            return label.CreateString();
        }

        /// <summary>
        /// Create a type reference for a data type
        /// </summary>
        public static TypeReference CreateForDataType(IDataType dataType)
        {
            return new TypeReference(dataType, null);
        }

        /// <summary>
        /// Create a type reference for an expression
        /// </summary>
        public static TypeReference CreateForExpression(Expressions.Expression expression)
        {
            return new TypeReference(null, expression);
        }
    }
}
