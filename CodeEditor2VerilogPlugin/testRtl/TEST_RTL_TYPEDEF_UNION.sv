// TEST_RTL_TYPEDEF_UNION.sv
// Bug 11.14 - typedef union未対応

module test_rtl_typedef_union();
    
    // typedef unionの例
    typedef union {
        logic [7:0] byte_val;
        logic [15:0] word_val;
        logic [31:0] dword_val;
    } data_union_t;
    
    // 変数宣言
    data_union_t union_data;
    
    initial begin
        union_data.byte_val = 8'hAA;
        $display("byte_val=%h", union_data.byte_val);
        
        union_data.word_val = 16'h1234;
        $display("word_val=%h", union_data.word_val);
        
        union_data.dword_val = 32'hDEADBEEF;
        $display("dword_val=%h", union_data.dword_val);
    end
    
    // typedef struct packedとの組み合わせ
    typedef struct packed {
        logic [7:0] header;
        logic [7:0] id;
    } header_t;
    
    typedef union packed {
        header_t header;
        logic [15:0] raw;
    } header_union_t;
    
    header_union_t hdr;
    
    initial begin
        hdr.header.id = 8'h42;
        $display("header.id=%h", hdr.header.id);
        $display("raw=%h", hdr.raw);
    end
    
endmodule
