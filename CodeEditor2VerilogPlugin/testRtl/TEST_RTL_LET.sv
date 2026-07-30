`timescale 1ns / 1ps

// Test module for let construct
// 
// Test cases for let declaration and function call:
// 1. Basic let declaration with simple expression
// 2. let with multiple arguments
// 3. let usage in expressions
// 4. let in module body

module TEST_RTL_LET;

logic [7:0] a;
logic [7:0] b;
logic [7:0] c;
logic [7:0] result;
logic       flag;

// ============================================
// Test 1: Basic let declaration
// ============================================
// let add(a, b) = a + b;
let ADD(a, b) = a + b;

// Usage
always_comb begin
    result = ADD(a, b);  // function call style
end

// ============================================
// Test 2: let with bitwise operations
// ============================================
let AND_OP(x, y) = x & y;
let OR_OP(x, y) = x | y;
let XOR_OP(x, y) = x ^ y;

always_comb begin
    result = AND_OP(a, b);
    result = OR_OP(a, b);
    result = XOR_OP(a, b);
end

// ============================================
// Test 3: let with reduction operators
// ============================================
let REDUCE_OR(x) = |x;
let REDUCE_AND(x) = &x;
let REDUCE_XOR(x) = ^x;

always_comb begin
    flag = REDUCE_OR(a);   // OR reduction
    flag = REDUCE_AND(b);  // AND reduction  
    flag = REDUCE_XOR(c);  // XOR reduction
end

// ============================================
// Test 4: let with conditional expression
// ============================================
let MUX(sel, d0, d1) = sel ? d1 : d0;

always_comb begin
    result = MUX(flag, a, b);
end

// ============================================
// Test 5: let with concatenation
// ============================================
let CONCAT_HI_LO(hi, lo) = {hi, lo};

always_comb begin
    result = CONCAT_HI_LO(a, b);  // {a, b}
end

// ============================================
// Test 6: Nested let calls
// ============================================
let ADD3(x, y, z) = x + y + z;

always_comb begin
    result = ADD3(a, b, c);  // a + b + c
end

// ============================================
// Test 7: let with shift operations
// ============================================
let SHL(x, n) = x << n;
let SHR(x, n) = x >> n;

always_comb begin
    result = SHL(a, 2);
    result = SHR(b, 3);
end

// ============================================
// Test 8: let with comparison result
// ============================================
let GT(a, b) = a > b;
let EQ(a, b) = a == b;

always_comb begin
    flag = GT(a, b);
    flag = EQ(a, b);
end

// ============================================
// Test 9: Simple let without arguments (constant)
// ============================================
let CONST_ONE = 8'h01;
let CONST_ZERO = 8'h00;

always_comb begin
    result = flag ? CONST_ONE : CONST_ZERO;
end

// ============================================
// Test 10: let in continuous assignment
// ============================================
let MAX(a, b) = (a > b) ? a : b;

assign result = MAX(a, b);

// ============================================
// Test 11: let with complex expression
// ============================================
let CLAMP(val, min_val, max_val) = (val < min_val) ? min_val : ((val > max_val) ? max_val : val);

always_comb begin
    result = CLAMP(a, 8'h10, 8'hF0);
end

endmodule
