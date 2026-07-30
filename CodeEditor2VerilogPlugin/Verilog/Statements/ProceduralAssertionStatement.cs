namespace pluginVerilog.Verilog.Statements
{
    public static class ProceduralAssertionStatement
    {
        /*
        procedural_assertion_statement ::=
              concurrent_assertion_statement
            | immediate_assertion_statement
            | checker_instantiation

        concurrent_assertion_item ::=
            [ block_identifier : ] concurrent_assertion_statement
            | checker_instantiation

        concurrent_assertion_statement ::=
              assert_property_statement
            | assume_property_statement
            | cover_property_statement
            | cover_sequence_statement
            | restrict_property_statement
        assert_property_statement::=
            "assert" "property" ( property_spec ) action_block
        
        assume_property_statement::=
            "assume" "property" ( property_spec ) action_block
        cover_property_statement::=
            "cover property" ( property_spec ) statement_or_null
        
        expect_property_statement ::=
            "expect" ( property_spec ) action_block
        
        cover_sequence_statement::=
            "cover" sequence ( [clocking_event ] [ disable iff ( expression_or_dist ) ]
            sequence_expr ) statement_or_null
        
        restrict_property_statement::=
            "restrict" property ( property_spec ) ;

        property_spec ::=
            [clocking_event ] [ disable iff ( expression_or_dist ) ] property_expr



        immediate_assertion_statement (assert,assume,cover)

        */

        public static IStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            switch (word.Text)
            {
                case "assert":
                case "assume":
                case "cover":
                case "restrict":
                    break;
                default:
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                    return null;
            }

            // Handle concurrent assertions (property keyword)
            if (word.NextText == "property")
            {
                switch (word.Text)
                {
                    case "assert":
                        return Assertion.AssertPropertyStatement.ParseCreate(word, nameSpace, statement_label);
                    case "assume":
                        return Assertion.AssumePropertyStatement.ParseCreate(word, nameSpace, statement_label);
                    case "cover":
                        return Assertion.CoverPropertyStatement.ParseCreate(word, nameSpace, statement_label);
                    case "restrict":
                        return Assertion.RestrictPropertyStatement.ParseCreate(word, nameSpace, statement_label);
                }
            }

            // Handle cover sequence
            if (word.Text == "cover" && word.NextText == "sequence")
            {
                return Assertion.CoverSequenceStatement.ParseCreate(word, nameSpace, statement_label);
            }

            return null;
        }
    }
}
