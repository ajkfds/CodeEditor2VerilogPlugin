using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Verilog.Expressions
{
    /// <summary>
    /// Reference to a sequence / property declaration identifier.
    /// Used by event_control of "@ sequence_identifier" / "@ property_identifier"
    /// (event_control ::= @ ps_or_hierarchical_sequence_identifier)
    /// </summary>
    public class SequenceReference : Primary
    {
    }
}
