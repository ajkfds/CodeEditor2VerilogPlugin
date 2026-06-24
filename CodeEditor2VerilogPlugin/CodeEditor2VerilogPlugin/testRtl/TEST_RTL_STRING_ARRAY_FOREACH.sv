// TEST_RTL_STRING_ARRAY_FOREACH.sv
// Bug 12.7.3 - string arrayのforeachインデックスparseエラー

module test_rtl_string_array_foreach();
    
    // string arrayの宣言
    string test [4] = '{"111", "222", "333", "444"};
    
    int i;
    
    initial begin
        // foreachでstring arrayを走査
        foreach(test[i]) begin
            $display("index=%d, value=%s", i, test[i]);
        end
    end
    
    // 多次元string array
    string matrix [2][3] = '{"{"a00", "a01", "a02"}", "{"b00", "b01", "b02"}"};
    
    int row, col;
    
    initial begin
        foreach(matrix[row, col]) begin
            $display("matrix[%0d][%0d]=%s", row, col, matrix[row][col]);
        end
    end
    
    // dynamic string array
    string dyn_str[];
    
    initial begin
        dyn_str = new[3];
        dyn_str[0] = "hello";
        dyn_str[1] = "world";
        dyn_str[2] = "test";
        
        foreach(dyn_str[j]) begin
            $display("dyn_str[%0d]=%s", j, dyn_str[j]);
        end
    end
    
endmodule
