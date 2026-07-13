`timescale 1ns / 1ps

module TEST_RTL_MODULE_INSTANCE
import TEST_PKG::*;
;

TEST_INTERFACE INF(

);
wire data;
wire clk;
/*
wire [1:0] data;
TEST_RTL_MODULE TEST_RTL_MODULE_0 (
	.DATA_I	(  ),
	.CLK_I	(  ),
	.RST_X_I	( aa.VALD )
);;
*/
// @scope TEST_RTL_MODULE TEST_RTL_MODULE_0
wire [7:0]	aa = TEST_RTL_MODULE_0.aaaa;





endmodule
