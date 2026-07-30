// TEST_RTL_CONST_FUNCTION.sv
// Bug 13.4.3 - const function (localparamでのfunction call)

module test_rtl_const_function();
    
    // 単純なfunction
    function logic [7:0] double_val(logic [7:0] val);
        return val * 2;
    endfunction
    
    function logic [7:0] add_val(logic [7:0] a, logic [7:0] b);
        return a + b;
    endfunction
    
    // localparamでのfunction call（定数評価）
    localparam [7:0] a = fun(3);
    localparam [7:0] b = double_val(8'h10);
    localparam [7:0] c = add_val(8'hAA, 8'h55);
    
    function [7:0] fun(int x);
        return x * 3;
    endfunction
    
    initial begin
        $display("a=%h", a);
        $display("b=%h", b);
        $display("c=%h", c);
    end
    
    // 複雑な定数式
    localparam [7:0] d = double_val(add_val(8'h01, 8'h02));
    
    initial begin
        $display("d=%h", d);
    end
    
    // 再帰function（定数ではない可能性）
    function [31:0] factorial(int n);
        if (n <= 1)
            return 1;
        else
            return n * factorial(n - 1);
    endfunction
    
    localparam [31:0] fact5 = factorial(5);
    
    initial begin
        $display("factorial(5)=%d", fact5);
    end
    
endmodule
