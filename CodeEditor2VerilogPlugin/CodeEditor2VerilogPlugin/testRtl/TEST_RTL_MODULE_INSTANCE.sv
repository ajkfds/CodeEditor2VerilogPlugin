`timescale 1ns / 1ps

module TEST_RTL_MODULE_INSTANCE
import TEST_PKG::*;
;

TEST_INTERFACE INF(

);

wire [1:0] data;
TEST_RTL_MODULE #(
.BITWIDTH(3)
)TEST_RTL_MODULE_0 (
	.DATA_O	( data ),
	.DATA2_O	( data ),
	.CLK_I	(  ),
	.RST_X_I	(  )
);




endmodule
