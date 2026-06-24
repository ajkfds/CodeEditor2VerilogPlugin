// TEST_RTL_NAMED_BLOCKS.sv
// Bug 9.3.5 - named blocks parse error

module test_rtl_named_blocks();
    
    logic a = 0;
    logic b = 0;
    logic c = 0;
    
    // 基本的なnamed block
    initial begin : init_block
        a = 1;
        b = a;
        c = b;
    end : init_block
    
    // begin-end with label (両方にラベル)
    logic x = 0;
    logic y = 0;
    logic z = 0;
    
    initial begin : assign_block
        x = 1;
        y = x;
        z = y;
    end : assign_block
    
    // ネストしたnamed block
    logic p = 0;
    logic q = 0;
    logic r = 0;
    logic s = 0;
    
    initial begin : outer_block
        p = 1;
        begin : inner_block
            q = p;
            r = q;
        end : inner_block
        s = r;
    end : outer_block
    
    // alwaysブロックでのnamed block
    logic [7:0] counter = 0;
    logic clk = 0;
    
    always @(posedge clk) begin : count_block
        if (counter < 255)
            counter <= counter + 1;
        else
            counter <= 0;
    end : count_block
    
    // for loop内のnamed block
    logic [7:0] sum = 0;
    
    initial begin : sum_loop
        for (int i = 0; i < 10; i++) begin : for_block
            sum = sum + i;
        end : for_block
        $display("sum=%d", sum);
    end : sum_loop
    
endmodule
