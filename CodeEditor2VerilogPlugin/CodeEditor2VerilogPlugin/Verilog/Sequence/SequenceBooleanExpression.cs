namespace pluginVerilog.Verilog.Sequence
{
    public class SequenceBooleanExpression : SequencePrimary
    {
        public required Expressions.Expression Expression { get; set; }
        public static new SequenceBooleanExpression? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace);
            if (expression == null) return null;
            return new SequenceBooleanExpression() { Expression = expression };
        }
    }
}
