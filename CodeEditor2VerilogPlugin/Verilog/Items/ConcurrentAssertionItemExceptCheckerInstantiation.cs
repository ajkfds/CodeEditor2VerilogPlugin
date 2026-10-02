using pluginVerilog.Verilog.Assertion;
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
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            string? blockIdentifier = null;
            if (General.IsSimpleIdentifier(word.Text) && word.NextText == ":")
            {
                blockIdentifier = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
                word.MoveNext();
            }

            // Handle assert property
            if (word.Text == "assert" && word.NextText == "property")
            {
                return ConcurrentAssertionStatementItem.ParseAssertProperty(word, nameSpace, blockIdentifier);
            }

            // Handle assume property
            if (word.Text == "assume" && word.NextText == "property")
            {
                return ConcurrentAssertionStatementItem.ParseAssumeProperty(word, nameSpace, blockIdentifier);
            }

            // Handle cover property
            if (word.Text == "cover" && word.NextText == "property")
            {
                return ConcurrentAssertionStatementItem.ParseCoverProperty(word, nameSpace, blockIdentifier);
            }

            // Handle restrict property
            if (word.Text == "restrict" && word.NextText == "property")
            {
                return ConcurrentAssertionStatementItem.ParseRestrictProperty(word, nameSpace, blockIdentifier);
            }

            // Handle cover sequence
            if (word.Text == "cover" && word.NextText == "sequence")
            {
                return ConcurrentAssertionStatementItem.ParseCoverSequence(word, nameSpace, blockIdentifier);
            }

            // checker_instantiation ::= ps_checker_identifier name_of_instance ( [list_of_checker_port_connections] ) ;
            // the checker identifier must resolve to a Checker building block
            if (blockIdentifier == null && General.IsSimpleIdentifier(word.Text) && word.NextText == "(")
            {
                BuildingBlocks.Checker? checker = word.ProjectProperty.DefinitionNameSpace.Get(word.Text) as BuildingBlocks.Checker;
                if (checker == null)
                {
                    BuildingBlocks.BuildingBlock? upperBlock = nameSpace.BuildingBlock.SearchBuildingBlockUpward(word.Text);
                    checker = upperBlock as BuildingBlocks.Checker;
                }
                if (checker != null)
                {
                    return CheckerInstantiation.Parse(word, nameSpace, checker);
                }
            }

            if (blockIdentifier == null) return false;
            return true;
        }
    }
}
