namespace pluginVerilog
{
    public class Rule
    {
        public enum SeverityEnum
        {
            Error,
            Warning,
            Notice
        }
        public required string Name { get; init; }
        public required SeverityEnum Severity { get; init; }
        public required string Message { get; init; }
        public required string Description { get; init; }

    }
}
