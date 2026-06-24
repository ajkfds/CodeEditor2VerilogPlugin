// TEST_RTL_DYNAMIC_ARRAY_METHOD.sv
// dynamic arrayのシステムメソッド未対応

module test_rtl_dynamic_array_method();
    
    // dynamic arrayの宣言
    bit [7:0] arr[];
    
    initial begin
        // new[] でメモリ確保
        arr = new [ 16 ];
        $display(":assert: (%d == 16)", arr.size());
        
        // 要素へのアクセス
        arr[0] = 8'hAA;
        arr[1] = 8'hBB;
        arr[15] = 8'hFF;
        
        $display("arr[0]=%h", arr[0]);
        $display("arr[1]=%h", arr[1]);
        $display("arr[15]=%h", arr[15]);
        
        // delete で全要素削除
        arr.delete;
        $display(":assert: (%d == 0)", arr.size());
    end
    
    // new[] でコピー
    bit [7:0] src[];
    bit [7:0] dst[];
    
    initial begin
        src = new[4];
        src[0] = 8'h01;
        src[1] = 8'h02;
        src[2] = 8'h03;
        src[3] = 8'h04;
        
        // new[] with size - 新しい配列を作成
        dst = new[4](src);  // srcの内容をコピー
        $display("dst[0]=%h", dst[0]);
        $display("dst[3]=%h", dst[3]);
        
        // new[] でresize
        dst = new[8](dst);  //  расширение
        $display(":assert: (%d == 8)", dst.size());
        $display("dst[4]=%h", dst[4]);  // 拡張部分は未初期化
    end
    
    // 多次元dynamic array
    int matrix [][];
    
    initial begin
        matrix = new[3];
        for (int i = 0; i < 3; i++) begin
            matrix[i] = new[4];
        end
        
        matrix[0][0] = 0;
        matrix[0][1] = 1;
        matrix[1][0] = 10;
        
        $display("matrix[0][0]=%d", matrix[0][0]);
        $display("matrix[1][0]=%d", matrix[1][0]);
    end
    
    // 組み込みメソッドの連鎖
    int int_arr[];
    
    initial begin
        int_arr = new[10];
        for (int i = 0; i < 10; i++) begin
            int_arr[i] = i;
        end
        
        // sort
        int_arr.sort();
        $display("sort[0]=%d", int_arr[0]);
        
        // reverse
        int_arr.reverse();
        $display("reverse[0]=%d", int_arr[0]);
        
        // sum
        int sum_val = int_arr.sum();
        $display("sum=%d", sum_val);
        
        // product
        int prod_val = int_arr.product();
        $display("product=%d", prod_val);
        
    end
    
endmodule
