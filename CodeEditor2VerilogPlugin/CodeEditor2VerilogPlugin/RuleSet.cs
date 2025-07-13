using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class RuleSet
    {
        public Rule ImplicitNetDeclaretion = new Rule
        {
            Name = "ImplicitNetDeclaration",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit net declaration",
            Description = "Implicit net declaration. It is recommended to declare the net type explicitly."
        };

        public Rule AssignmentBitwidthMismatch = new Rule
        {
            Name = "AssignmentBitwidthMismatch",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Assignment bitwidth mismatch",
            Description = "The bitwidth of the left-hand side and right-hand side of an assignment do not match."
        };

    }
}
