using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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
