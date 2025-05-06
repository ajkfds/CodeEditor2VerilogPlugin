# Verilog Annotation Specification for Static Analysis

This document defines a specification for annotations embedded in Verilog HDL code via comments. These annotations allow tools to perform advanced static analysis without affecting synthesis or simulation.

---

## Basic Syntax

Annotations are written inside single-line comments `//`, and follow this format:

```
@command
@command: value
@command: value1, value2, ...
```

- Annotations must begin with `@`
- Arguments may be comma-separated
- Multiple annotations can be written in a single comment line

---

## Supported Commands

| Command       | Meaning                                                         | Examples                                |
|---------------|------------------------------------------------------------------|-----------------------------------------|
| `@clock`      | Declares the signal as a clock                                  | (optional) `posedge`, `negedge`         |
| `@reset`      | Declares the signal as a reset                                  | `active-high`, `active-low`             |
| `@sync`       | Declares that the signal is synchronized to a specific clock    | `clk_name`, `posedge`, `reset_name`     |
| `@async`      | Declares that the reset is asynchronous                         | (no value)                              |
| `@portgroup`  | Groups a set of port declarations for readability               | `Group Name`                            |
| `@discard`    | Marks variables as no longer valid after this point             | variable names                          |

---

## Examples

```verilog
// @portgroup: Clock & Reset
input logic clk_sys;         // @clock
input logic reset_n;         // @reset: active-low, @sync: clk_sys
input logic rst_async;       // @reset: active-high, @async

// @portgroup: AXI Interface
input  logic        axi_valid;   // @sync: clk_sys
output logic        axi_ready;   // @sync: clk_sys
input  logic [31:0] axi_data;    // @sync: clk_sys

// @portgroup: Control
reg [7:0] counter;           // @sync: clk_sys
reg ready;                   // @sync: clk_sys, posedge, reset_n

// Discard temporary variables
logic [7:0] temp_data;
temp_data = some_function();
// @discard: temp_data

if (temp_data == 8'h00) begin  // Static error: temp_data discarded
    ready = 1;
end
```

---

## Static Checker Rules

- `@sync:` must refer to a clock declared with `@clock`
- `@reset:` must include either `@sync:` or `@async`
- Discarded variables (`@discard`) must not be used afterwards
- Variables without `@sync` can optionally be flagged
- Port grouping (`@portgroup`) is used for documentation and visualization

---

## Port Grouping

To improve readability of modules with many ports, use the `@portgroup:` annotation to group related ports:

```verilog
// @portgroup: Memory Interface
input  logic mem_valid;
output logic mem_ready;
input  logic [31:0] mem_addr;
```

These groups can be used to generate structured documentation.

---

## Discarding Variables

Use `@discard:` to indicate that one or more variables are no longer valid after a given point in code. Useful for scoping temporary variables.

```verilog
// @discard: temp1, temp2
```

The static analyzer should flag any usage of those variables after this point.

---

## Notes

- These annotations are comment-based and do not affect Verilog synthesis or simulation
- Ideal for use in linters, documentation generators, and intelligent code editors
- Designed to scale with large codebases and encourage design clarity

---

## Potential Future Extensions

- `@domain:` for clock domain labeling
- `@latency:`, `@role:`, `@bus:` for performance and role annotations
- `@readonly`, `@writeonly` for access control

