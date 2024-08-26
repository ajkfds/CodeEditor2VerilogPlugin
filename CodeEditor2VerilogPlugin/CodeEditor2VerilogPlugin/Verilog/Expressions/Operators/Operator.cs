using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Expressions.Operators
{
    public class Operator : Primary
    {
        protected Operator(string text, byte precedence)
        {
            Text = text;
            Precedence = precedence;
        }

        public readonly string Text = "";
        public readonly byte Precedence;

        public override AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Contorls.ColorLabel label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            AppendLabel(label);
            return label;
        }

        public override string CreateString()
        {
            return Text;
        }
    }
    /*
    Table 12—Precedence rules for operators
    1   + - ! ~ (unary)             Highest precedence
    2   **
    3   *  /  %
    4   +  - (binary)
    5   <<  >>  <<<  >>>
    6   <  <=  >  >=
    7   ==  !=  ===  !==
    8   &  ~&
    9   ^  ^~  ~^
    10  |  ~|
    11  &&
    12  ||
    13  ?: (conditional operator)   Lowest precedence
    */

    /* SystemVerilog2017
     * 
    Precedence rules for operators
    1   () [] :: .                                              Left    Highest
    2                                  (' cast)
    3   + - ! ~ & ~& | ~| ^ ~^ ^~ ++ -- (unary)
    4   **                                                      Left
    5   * / %                                                   Left
    6   + - (binary)                                            Left
    7   << >> <<< >>>                                           Left
    8   < <= > >= inside dist                                   Left
    9   == != === !== ==? !=?                                   Left
    10   & (binary)                                              Left
    11  ^ ~^ ^~ (binary)                                        Left
    12  | (binary)                                              Left
    13  &&                                                      Left
    14  ||                                                      Left
    15  ?: (conditional operator)                               Right
    16  –> <–>                                                  Right
    17  = += -= *= /= %= &= ^= |= <<= >>= <<<= >>>= := :/ <=    None
    18  {} {{}}                                                 Concatenation   Lowest

    assignment_operator ::= = | += | -= | *= | /= | %= | &= | |= | ^= | <<= | >>= | <<<= | >>>=
    conditional_expression ::= cond_predicate ? { attribute_instance } expression : expression 
    binary_operator ::=  + | - | * | / | % | == | != | === | !== | ==? | !=? | && | || | **
    | < | <= | > | >= | & | | | ^ | ^~ | ~^ | >> | << | >>> | <<<
    | -> | <-> 
    inc_or_dec_operator ::= ++ | --
    stream_operator ::= >> | <<
    */


    /* SystemVerilog2017 not implemented list
    Precedence rules for operators
    1   () [] :: .                                              Left    Highest
    2                                  (' cast)
    8  inside dist                                   Left
    17  = += -= *= /= %= &= ^= |= <<= >>= <<<= >>>= := :/ <=    None
    18  {} {{}}                                                 Concatenation   Lowest

    assignment_operator ::= = | += | -= | *= | /= | %= | &= | |= | ^= | <<= | >>= | <<<= | >>>=
    conditional_expression ::= cond_predicate ? { attribute_instance } expression : expression 
    inc_or_dec_operator ::= ++ | --
    stream_operator ::= >> | <<
    */
    /*
    assignment_operator ::= 
        = | += | -= | *= | /= | %= | &= | |= | ^= | <<= | >>= | <<<= | >>>=
    conditional_expression ::= 
        cond_predicate ? { attribute_instance } expression : expression
    unary_operator ::=
        + | - | ! | ~ | & | ~& | | | ~| | ^ | ~^ | ^~
    binary_operator ::=
        + | - | * | / | % | == | != | === | !== | ==? | !=? | && | || | **
        | < | <= | > | >= | & | | | ^ | ^~ | ~^ | >> | << | >>> | <<<
        | -> | <->
    inc_or_dec_operator ::= ++ | --
    
    stream_operator ::= >> | <<     
     


    Operator                token Name                                      Operand data types
    =                       Binary assignment operator                      Any
    += -= /= *=             Binary arithmetic assignment operators          Integral, real, shortreal
    %=                      Binary arithmetic modulus assignment operator   Integral
    &= |= ^=                Binary bitwise assignment operators             Integral
    >>= <<=                 Binary logical shift assignment operators       Integral
    >>>= <<<=               Binary arithmetic shift assignment operators    Integral
    ?:                      Conditional operator                            Any
    + -                     Unary arithmetic operators                      Integral, real, shortreal
    !                       Unary logical negation operator                 Integral, real, shortreal
    ~ & ~& | ~| ^ ~^ ^~     Unary logical reduction operators               Integral
    + - * / **              Binary arithmetic operators                     Integral, real, shortreal
    %                       Binary arithmetic modulus operator              Integral
    & | ^ ^~ ~^             Binary bitwise operators                        Integral
    >> <<                   Binary logical shift operators                  Integral
    >>> <<<                 Binary arithmetic shift operators               Integral
    && || –> <–>            Binary logical operators                        Integral, real, shortreal
    < <= > >=               Binary relational operators                     Integral, real, shortreal
    === !==                 Binary case equality operators                  Any except real and shortreal
    == !=                   Binary logical equality operators               Any
    ==? !=?                 Binary wildcard equality operators              Integral
    ++ --                   Unary increment, decrement operators            Integral, real, shortreal
    inside                  Binary set membership operator                  Singular for the left operand
    dist                    Binary distribution operator                    Integral
    {} {{}}                 Concatenation, replication operators            Integral
    {<<{}} {>>{}}           Stream operators                                Integral


    Operator                                    Associativity   Precedence
    () [] :: .                                  Left            Highest
    + - ! ~ & ~& | ~| ^ ~^ ^~ ++ -- (unary)
    **                                          Left
    * / %                                       Left
    + - (binary)                                Left
    << >> <<< >>>                               Left
    < <= > >= inside dist                       Left
    == != === !== ==? !=?                       Left
    & (binary)                                  Left
    ^ ~^ ^~ (binary)                            Left
    | (binary)                                  Left
    &&                                          Left
    ||                                          Left
    ?: (conditional operator)                   Right
    –> <–>                                      Right
    = += -= *= /= %= &= ^= |=
    <<= >>= <<<= >>>= := :/ <=
    None
    {} {{}}                                     Concatenation   Lowest
    */





}
