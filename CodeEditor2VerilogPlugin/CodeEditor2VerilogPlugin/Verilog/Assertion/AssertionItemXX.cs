namespace pluginVerilog.Verilog.Assertion
{
    public class AssertionItemXX
    {
        /*
        assertion_item ::=
              concurrent_assertion_item
            | deferred_immediate_assertion_item

        deferred_immediate_assertion_item ::=
            [ block_identifier : ] deferred_immediate_assertion_statement

        procedural_assertion_statement ::=
              concurrent_assertion_statement
            | immediate_assertion_statement
            | checker_instantiation

        immediate_assertion_statement ::=
              simple_immediate_assertion_statement
            | deferred_immediate_assertion_statement

        simple_immediate_assertion_statement ::=
              simple_immediate_assert_statement
            | simple_immediate_assume_statement
            | simple_immediate_cover_statement

        simple_immediate_assert_statement ::=
            assert ( expression ) action_block

        simple_immediate_assume_statement ::=
            assume ( expression ) action_block

        simple_immediate_cover_statement ::=
            cover ( expression ) statement_or_null

        deferred_immediate_assertion_statement ::=
              deferred_immediate_assert_statement
            | deferred_immediate_assume_statement
            | deferred_immediate_cover_statement

        deferred_immediate_assert_statement ::=
              assert #0 ( expression ) action_block
            | assert final ( expression ) action_block

        deferred_immediate_assume_statement ::=
              assume #0 ( expression ) action_block
            | assume final ( expression ) action_block

        deferred_immediate_cover_statement ::=
              cover #0 ( expression ) statement_or_null
            | cover final ( expression ) statement_or_null

        action_block ::=
            [ statement ] [ else statement ]
         */

        // 実装ステータス:
        // - ImmidiateAssertionStatement.ParseCreate() で対応:
        //   ✅ simple_immediate_assert_statement (assert (expr) action_block)
        //   ✅ simple_immediate_assume_statement (assume (expr) action_block)
        //   ✅ simple_immediate_cover_statement (cover (expr) statement_or_null)
        //   ✅ deferred_immediate_assert_statement (assert #0 / final)
        //   ✅ deferred_immediate_assume_statement (assume #0 / final)
        //   ✅ deferred_immediate_cover_statement (cover #0 / final)
        //   ✅ action_block の else 句対応
    }
}
