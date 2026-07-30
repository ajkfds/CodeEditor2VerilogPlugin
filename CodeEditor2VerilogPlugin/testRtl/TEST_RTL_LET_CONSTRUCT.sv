// TEST_RTL_LET_CONSTRUCT.sv
// Bug 11.12 - let construct parse error

module test_rtl_let();
    // let宣言の定義
    let op_and(x, y) = x & y;
    let op_or(x, y) = x | y;
    let op_xor(x, y) = x ^ y;
    
    // let呼び出し（function_callとしてパース）
    logic [7:0] a = 8'hAA;
    logic [7:0] b = 8'h55;
    logic [7:0] result_and;
    logic [7:0] result_or;
    logic [7:0] result_xor;
    
    initial begin
        result_and = op_and(a, b);  // 0xAA & 0x55 = 0x00
        result_or = op_or(a, b);    // 0xAA | 0x55 = 0xFF
        result_xor = op_xor(a, b);  // 0xAA ^ 0x55 = 0xFF
        $display("AND=%h, OR=%h, XOR=%h", result_and, result_or, result_xor);
    end
    
    // 複雑なlet式
    let op(x, y, z) = |((x | y) & z);
    
    logic [7:0] c = 8'hFF;
    logic result;
    
    initial begin
        result = op(a, b, c);
        $display("result=%b", result);
    end
    
endmodule
