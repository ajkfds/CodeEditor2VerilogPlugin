`timescale 1ns / 1ps

module  AA;
	
endmodule

module TEST_RTL_FUNCTION;

typedef struct packed{
logic [3:0]	aa;
logic bb;
} aaa;

assign  aaa = 1'b0;

MODULE1 aa();

// MODULE1 aaa();


endmodule


