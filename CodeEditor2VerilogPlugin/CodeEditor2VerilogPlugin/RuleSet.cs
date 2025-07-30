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

        public Rule ImplicitModportDirection = new Rule
        {
            Name = "ImplicitModportDirection",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit Modport direction",
            Description = "The direction of modport declaration is missing. inout direction is used for this port."
        };

        public Rule ImplicitModportInterfaceConnectionToInstance = new Rule
        {
            Name = "ImplicitModportInterfaceConnectionToInstance",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Implicit Modport connection",
            Description = "The implicit modport is connected to modport.use modport to connect to instances."
        };

        public Rule NotAllPortConnectedWithWildcardNamedPortConnections = new Rule
        {
            Name = "NotAllPortConnectedWithWildcardNamedPortConnections",
            Severity = Rule.SeverityEnum.Warning,
            Message = "Not all ports connected with wildcard named port connections",
            Description = "Not all ports are connected with wildcard named port connections. It is recommended to connect all ports."
        };
    }
}
