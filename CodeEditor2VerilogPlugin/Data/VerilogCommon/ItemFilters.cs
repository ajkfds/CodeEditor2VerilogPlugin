using System;
using System.Collections.Generic;
using System.Text;

namespace pluginVerilog.Data.VerilogCommon
{
    public static class ItemFilters
    {
        public static Func<Data.VerilogCommon.AutoCompleteItem, bool> NoFilter()
        {
            return (item) => true;
        }

        public static Func<Data.VerilogCommon.AutoCompleteItem, bool> ExpressionOnly()
        {
            return (item) =>
            {
                if (item.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.Keyword) return false;
                if (item.Type == Data.VerilogCommon.AutoCompleteItem.CompleteType.Task) return false;
                return true;
            };
        }


    }
}
