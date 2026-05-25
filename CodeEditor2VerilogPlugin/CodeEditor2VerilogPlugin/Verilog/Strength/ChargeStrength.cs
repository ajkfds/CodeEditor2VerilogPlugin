using pluginVerilog.Verilog.DataObjects;

namespace pluginVerilog.Verilog.DataObjects.Nets
{
    /// <summary>
    /// SystemVerilog Charge Strength
    /// IEEE 1800-2017
    /// 
    /// charge_strength ::= ( small ) | ( medium ) | ( large )
    /// 
    /// This is used with trireg net type to specify the strength of the charge storage.
    /// </summary>
    public class ChargeStrength
    {
        /// <summary>
        /// Charge strength value
        /// </summary>
        public StrengthValueEnum Strength { get; }

        /// <summary>
        /// Charge strength value enumeration
        /// </summary>
        public enum StrengthValueEnum
        {
            Small,
            Medium,
            Large
        }

        private ChargeStrength(StrengthValueEnum strength)
        {
            Strength = strength;
        }

        /// <summary>
        /// Parse charge strength specification
        /// </summary>
        public static ChargeStrength? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            /*
            charge_strength ::= ( small ) | ( medium ) | ( large )
            */

            if (word.Text != "(") return null;
            word.MoveNext();

            StrengthValueEnum? strength = null;
            switch (word.Text)
            {
                case "small":
                    strength = StrengthValueEnum.Small;
                    break;
                case "medium":
                    strength = StrengthValueEnum.Medium;
                    break;
                case "large":
                    strength = StrengthValueEnum.Large;
                    break;
                default:
                    word.AddError("small, medium, or large expected");
                    if (word.Text == ")") word.MoveNext();
                    return null;
            }

            if (word.Text != ")")
            {
                word.AddError(") expected");
                return null;
            }
            word.MoveNext();

            if (strength == null) return null;

            return new ChargeStrength((StrengthValueEnum)strength);
        }

        public string CreateString()
        {
            return "(" + Strength.ToString().ToLower() + ")";
        }
    }
}
