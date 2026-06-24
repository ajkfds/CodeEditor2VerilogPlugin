`timescale 1ns / 1ps

// Test module for instance array port bit width checking
// 
// Test cases:
// 1. Valid broadcast: expression.BitWidth == instanceCount * port.BitWidth
// 2. Valid single signal: expression.BitWidth == port.BitWidth
// 3. Insufficient bits: expression.BitWidth < instanceCount * port.BitWidth (error)
// 4. Excess bits: expression.BitWidth > instanceCount * port.BitWidth (warning)

// ============================================
// Sub-module with 4-bit port
// ============================================
module BUF4 (
    input wire [3:0]  I,
    output wire [3:0] O,
    input wire        EN
);
    assign O = EN ? I : 4'b0;
endmodule

// ============================================
// Sub-module with 8-bit port
// ============================================
module BUF8 (
    input wire [7:0]  I,
    output wire [7:0] O,
    input wire        EN
);
    assign O = EN ? I : 8'b0;
endmodule

// ============================================
// Test bench
// ============================================
module TEST_RTL_INSTANCE_ARRAY;

wire [3:0]  in_data;
wire        en;
wire [15:0] out16;   // 4-bit x 4 instances = 16 bits
wire [3:0]  out4;    // Single 4-bit signal
wire [31:0] out32;   // 4-bit x 4 instances = 16 bits (excess)
wire [7:0]  out8;    // 8-bit single signal

// Test 1: Valid broadcast - 16 bits for 4 instances (4-bit each)
BUF4 u_buf4_valid[3:0] (
    .I  ( in_data ),        // 4-bit signal -> broadcast to 4 instances
    .O  ( out16 ),          // 16-bit output (4 x 4-bit)
    .EN ( en )
);
// Expected: Valid - broadcast (4-bit connected to 4 instances)

// Test 2: Valid - exact bit width (16 bits for 4 instances)
BUF4 u_buf4_exact[3:0] (
    .I  ( {in_data, in_data, in_data, in_data} ),  // 16-bit concatenation
    .O  ( out16 ),
    .EN ( en )
);
// Expected: Valid - exact match (16 bits = 4 x 4-bit)

// Test 3: Insufficient bits - should produce ERROR
BUF4 u_buf4_insufficient[3:0] (
    .I  ( in_data[1:0] ),   // 2-bit signal only!
    .O  ( out16 ),
    .EN ( en )
);
// Expected: ERROR - insufficient bits (need 16 bits, got 2)

// Test 4: Excess bits - should produce WARNING
BUF4 u_buf4_excess[3:0] (
    .I  ( out32[15:0] ),    // 32-bit signal, only lower 16 used
    .O  ( out16 ),
    .EN ( en )
);
// Expected: WARNING - excess bits (32 bits for 4 instances = 16 bits needed)

// Test 5: 8-bit port with instance array
BUF8 u_buf8[1:0] (
    .I  ( out32[15:8] ),    // 8-bit signal
    .O  ( out8 ),
    .EN ( en )
);
// Expected: Valid - 8-bit for 2 instances (8-bit each)

// Test 6: 8-bit port - insufficient bits
BUF8 u_buf8_insufficient[1:0] (
    .I  ( in_data[1:0] ),   // 2-bit signal!
    .O  ( out8 ),
    .EN ( en )
);
// Expected: ERROR - insufficient bits (need 16 bits, got 2)

// Test 7: 8-bit port - excess bits
BUF8 u_buf8_excess[1:0] (
    .I  ( out32 ),          // 32-bit signal, only lower 8 used per instance
    .O  ( out8 ),
    .EN ( en )
);
// Expected: WARNING - excess bits (32 bits for 2 instances = 16 bits needed)

endmodule
