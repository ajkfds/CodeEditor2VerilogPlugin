`timescale 1ns / 1ps

module TEST_RTL_MODULE;
wire [31:0] data;

assign data =	32'(2'b00);

`define SCR1_XLEN               32
wire [31:0]	data0 =	`SCR1_XLEN'(2'b00);














endmodule
