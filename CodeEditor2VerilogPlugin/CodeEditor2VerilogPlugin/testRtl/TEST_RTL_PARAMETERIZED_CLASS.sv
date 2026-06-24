// TEST_RTL_PARAMETERIZED_CLASS.sv
// パラメタライズドクラス未対応

module test_rtl_parameterized_class();
    
    // パラメタライズドクラスの定義
    class par_cls #(int a = 25);
        parameter int b = 23;
        
        logic [a-1:0] data_a;
        logic [b-1:0] data_b;
        
        function new();
            data_a = 0;
            data_b = 0;
        endfunction
        
        function void set_a(logic [a-1:0] val);
            data_a = val;
        endfunction
        
    endclass
    
    // パラメタライズドクラスのインスタンス生成
    par_cls #(.a(16)) obj1;
    par_cls #(32) obj2;
    par_cls obj3;  // デフォルトパラメータ
    
    initial begin
        obj1 = new();
        obj2 = new();
        obj3 = new();
        
        obj1.set_a(16'hABCD);
        $display("obj1.data_a=%h", obj1.data_a);
    end
    
    // 複数パラメータのクラス
    class multi_param_class #(type T = logic, int W = 8);
        parameter int DEPTH = 16;
        
        T queue [$:DEPTH];
        
        function void push(T val);
            if (queue.size() < DEPTH)
                queue.push_back(val);
        endfunction
        
        function T pop();
            if (queue.size() > 0)
                return queue.pop_front();
            else
                return 0;
        endfunction
        
    endclass
    
    // インスタンス化
    multi_param_class #(logic, 16) #(.DEPTH(32)) queue_obj;
    
    initial begin
        queue_obj = new();
        queue_obj.push(8'hAA);
        queue_obj.push(8'hBB);
        $display("pop=%h", queue_obj.pop());
    end
    
endmodule
