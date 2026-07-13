`timescale 1ns / 1ps

module TEST_RTL_MODULE #(
parameter	BUS_WIDTH = 8
)(
input [BUS_WIDTH-1:0]	DATA_I,	// @sync:CLK_I
input	CLK_I,		// @sync:clock
input	RST_X_I		// @sync:reset
);

TEST_INTERFACE TI();



wire [7:0] aaaa;


if(1) begin : aa
	wire	a;
	TEST_RTL_MODULE3 TTT();
end

if(1) begin : bb
	wire	a;
end

wire m2 = aa.TTT.m3_wire;


//wire m22 = TTT.m2;

endmodule


