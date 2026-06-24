`timescale 1ns / 1ps

module TEST_RTL_MODULE_INSTANCE;

wire clk;
wire rst_x;
wire [1:0]	data1;
wire [1:0]	data0;

TEST_RTL_MODULE TEST_RTL_MODULE_0 [1:0] (
	.DATA_O	( { data1, data0 } ),
	.CLK_I	( clk ),
	.RST_X_I	( rst_x )
);




endmodule
