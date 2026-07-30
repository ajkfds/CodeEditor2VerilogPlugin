// TEST_RTL_MODULE_INSTANCE_DOTSTAR.sv
// Bug 10.6.2 - module instance .* parse error

module flop (
    input clk,		// @sync:clock
    input rst_n,	// @sync:reset
    input [7:0] data_in,
    output reg [7:0] data_out
);
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n)
            data_out <= 8'h00;
        else
            data_out <= data_in;
    end
endmodule

module test_rtl_module_instance_dotstar();
    
    logic clk = 0;
    logic rst_n = 0;
    logic [7:0] data_in = 8'hAA;
    wire [7:0] data_out;
    
    // .* を使った暗黙のport接続
    // flop u_flop (.*);  <- この形式でエラー
    flop u_flop (.*);
    
    // 通常の明示的接続（比較用）
    flop u_flop_explicit (
        .clk(clk),
        .rst_n(rst_n),
        .data_in(data_in),
        .data_out(data_out)
    );
    
    // .* と明示的接続の組み合わせ
    // 以下の形式でエラー
    flop u_flop_mixed (.*);
    
    //  clock
    always #5 clk = ~clk;
    
    initial begin
        #10 rst_n = 1;
        #100 $finish;
    end
    
endmodule
