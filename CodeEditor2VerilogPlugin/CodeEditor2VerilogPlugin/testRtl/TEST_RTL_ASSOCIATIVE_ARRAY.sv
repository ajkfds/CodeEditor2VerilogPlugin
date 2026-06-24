// TEST_RTL_ASSOCIATIVE_ARRAY.sv
// associative arrayの参照エラー

module test_rtl_associative_array();
    
    // associative arrayの宣言
    int arr [ int ];
    int str_arr [ string ];
    logic [7:0] data_arr [ bit [1:0] ];
    
    initial begin
        // 初期状態の確認
        $display(":assert: (%d == 0)", arr.size());
        
        // 要素の代入
        arr[10] = 10;
        arr[20] = 20;
        arr[30] = 30;
        
        // size() メソッド
        $display(":assert: (%d == 3)", arr.size());
        
        // exists() メソッド
        $display(":assert: (%d == 1)", arr.exists(10));
        $display(":assert: (%d == 0)", arr.exists(99));
        
        // first() / last() メソッド
        int idx;
        $display(":assert: (%d == 1)", arr.first(idx));
        $display("first key=%d", idx);
        
        $display(":assert: (%d == 1)", arr.last(idx));
        $display("last key=%d", idx);
        
        // next() / prev() メソッド
        idx = 10;
        $display(":assert: (%d == 1)", arr.next(idx));
        $display("next key=%d", idx);
        
        // delete() メソッド
        arr.delete(20);
        $display(":assert: (%d == 2)", arr.size());
        
        arr.delete();
        $display(":assert: (%d == 0)", arr.size());
    end
    
    // string key associative array
    initial begin
        str_arr["key1"] = 100;
        str_arr["key2"] = 200;
        
        string str_idx;
        $display(":assert: (%d == 1)", str_arr.first(str_idx));
        $display("first string key=%s", str_idx);
        
        $display("value=%d", str_arr["key1"]);
    end
    
    // 多次元associative array
    int matrix [string][int];
    
    initial begin
        matrix["row1"][0] = 1;
        matrix["row1"][1] = 2;
        matrix["row2"][0] = 3;
        
        $display(":assert: (%d == 2)", matrix["row1"].size());
    end
    
endmodule
