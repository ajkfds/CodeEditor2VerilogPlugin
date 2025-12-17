using pluginVerilog.Verilog.DataObjects.Nets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Items
{
    public class ConcurrentAssertionItemExceptCheckerInstantiation
    {
        /*
        concurrent_assertion_item ::=
                [ block_identifier : ] concurrent_assertion_statement
            | checker_instantiation
        
            concurrent_assertion_statement ::=
                  assert_property_statement
                | assume_property_statement
                | cover_property_statement
                | cover_sequence_statement
                | restrict_property_statement
            checker_instantiation ::=
                    ps_checker_identifier name_of_instance ( [list_of_checker_port_connections] ) ;
        */
        public static async Task<bool> Parse(WordScanner word, NameSpace nameSpace)
        {
            string? blockIdentifier = null;
            if (General.IsSimpleIdentifier(word.Text) && word.NextText == ":")
            {
                blockIdentifier = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext();
            }
            if (word.NextText == "property")
            {
                switch (word.Text)
                {
                    case "assert":
                        return await ConcurrentAssertionStatementItem.Parse(word, nameSpace, blockIdentifier);
                }
            }

            if (blockIdentifier == null) return false;
            return true;
        }
    }
}
