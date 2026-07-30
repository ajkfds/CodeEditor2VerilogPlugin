using pluginVerilog.Data;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    /// <summary>
    /// Represents a scope reference declared via @scope comment annotation.
    /// Allows accessing elements from another building block (module, interface, etc.)
    /// without explicit instance connection.
    /// 
    /// Format: // @scope buildingBlockName #(.paramName(paramValue),...) [instanceName]
    /// 
    /// Examples:
    ///   // @scope my_module                         - Direct access to my_module's internal signals
    ///   // @scope my_module u_inst                  - Access via instance (u_inst.signal)
    ///   // @scope my_module #(.WIDTH(8)) u_parent   - With parameter override
    /// </summary>
    public class CommentScopeReference
    {
        /// <summary>
        /// Name of the target building block (module, interface, etc.)
        /// </summary>
        public string BuildingBlockName { get; set; } = "";

        /// <parameter name="InstanceName">
        /// Optional instance name. If specified, access via instance (e.g., u_inst.signal).
        /// If null or empty, direct access to internal signals.
        /// </parameter>
        public string? InstanceName { get; set; }

        /// <summary>
        /// Parameter overrides for the building block instantiation.
        /// </summary>
        public Dictionary<string, Expression>? ParameterOverrides { get; set; }

        /// <summary>
        /// Resolved building block (set during code analysis)
        /// </summary>
        public BuildingBlocks.BuildingBlock? ResolvedBuildingBlock { get; set; }

        /// <summary>
        /// Resolved instance (set when InstanceName is specified)
        /// </summary>
        public VerilogModuleInstance? ResolvedInstance { get; set; }

        /// <summary>
        /// Whether this scope reference has been resolved
        /// </summary>
        public bool IsResolved => ResolvedBuildingBlock != null;

        /// <summary>
        /// The name under which this scope reference's VirtualScopeNameSpace is
        /// (or will be) registered in the containing NameSpace. By default this
        /// is the InstanceName if present, otherwise the BuildingBlockName. Used
        /// to deduplicate repeated @scope annotations targeting the same entry.
        /// </summary>
        public string VirtualScopeEntryName { get; set; } = "";

        public override string ToString()
        {
            string result = BuildingBlockName;
            if (ParameterOverrides != null && ParameterOverrides.Count > 0)
            {
                result += " #(";
                bool first = true;
                foreach (var param in ParameterOverrides)
                {
                    if (!first) result += ", ";
                    first = false;
                    result += $".{param.Key}({param.Value.ConstantValueString()})";
                }
                result += ")";
            }
            if (!string.IsNullOrEmpty(InstanceName))
            {
                result += " " + InstanceName;
            }
            return result;
        }
    }
}
