using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Property
{
    /// <summary>
    /// Property primary expressions (base case for property expressions)
    /// 
    /// property_primary ::=
    ///     sequence_instance
    ///     | property_instance
    ///     | ( expression )
    /// 
    /// A property primary is the simplest form of property expression that can be used
    /// in more complex property expressions.
    /// </summary>
    public class PropertyPrimary : Expressions.Expression
    {
        /// <summary>
        /// Reference to sequence instance (if this primary is a sequence)
        /// </summary>
        public Sequence.SequenceInstance? SequenceInstance { get; set; }

        /// <summary>
        /// Reference to property instance (if this primary is a property call)
        /// </summary>
        public PropertyInstance? PropertyInstance { get; set; }

        /// <summary>
        /// Expression wrapped in parentheses (if any)
        /// </summary>
        public Expression? ParenthesizedExpression { get; set; }

        /// <summary>
        /// Parse a property primary
        /// </summary>
        public static PropertyPrimary? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            PropertyPrimary primary = new PropertyPrimary();

            // Check for parenthesized expression: ( expression )
            if (word.Text == "(")
            {
                word.MoveNext();
                Expression? expr = Expression.ParseCreate(word, nameSpace);
                if (expr != null && word.Text == ")")
                {
                    word.MoveNext();
                    primary.ParenthesizedExpression = expr;
                    return primary;
                }
                // If parsing failed, restore position would be needed but for simplicity we continue
            }

            // Check for sequence_instance (identifier followed by optional arguments)
            var sequenceInstance = Sequence.SequenceInstance.ParseCreate(word, nameSpace);
            if (sequenceInstance != null)
            {
                primary.SequenceInstance = sequenceInstance;
                return primary;
            }

            // Check for property_instance (property identifier followed by optional arguments)
            var propertyInstance = PropertyInstance.ParseCreate(word, nameSpace);
            if (propertyInstance != null)
            {
                primary.PropertyInstance = propertyInstance;
                return primary;
            }

            return null;
        }
    }
}
