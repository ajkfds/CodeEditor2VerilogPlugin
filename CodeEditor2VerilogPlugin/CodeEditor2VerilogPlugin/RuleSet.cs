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

    }
}
