// The parser namespace moved to the UI-free assembly (SystemVerilogCore.Verilog).
// Much of this project still writes `Verilog.ParsedDocument` and similar, which
// used to resolve to the child namespace `pluginVerilog.Verilog`. This alias
// keeps those references resolving to the new location, so the move did not have
// to rewrite every type name by hand.
global using Verilog = SystemVerilogCore.Verilog;
