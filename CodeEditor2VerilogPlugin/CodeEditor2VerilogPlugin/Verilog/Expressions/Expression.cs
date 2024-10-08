using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor;
using pluginVerilog.Verilog.Expressions.Operators;

namespace pluginVerilog.Verilog.Expressions
{
    public class Expression
    {
        protected Expression()
        {
            Constant = false;
        }

        //        public List<Primary> RpnPrimaries = new List<Primary>();

        public Primary Primary;
        public virtual bool Constant { get; protected set; }
        public virtual double? Value { get; protected set; }
        public virtual int? BitWidth { get; protected set; }
        public WordReference Reference { get; protected set; }
        public bool IncrementDecrement = false;

        /// <summary>
        /// dispose reference
        /// </summary>
        public virtual void DisposeReference()
        {
            Reference.Dispose();
            Reference = null;
        }

        /// <summary>
        ///  dispose reference hierarchy, keep top level reference only.
        /// </summary>
        public virtual void DisposeSubReference(bool keepThisReference)
        {
            if (!keepThisReference) DisposeReference();
        }

        /// <summary>
        /// get label object of this expression
        /// </summary>
        /// <returns></returns>
        public virtual AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            var label = new AjkAvaloniaLibs.Contorls.ColorLabel();
            AppendLabel(label);
            return label;
        }
        public virtual string CreateString()
        {
            return "";
        }
        public virtual void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {

        }

        public virtual void AppendString(StringBuilder stringBuilder)
        {

        }

        public string ConstantValueString()
        {
            if (Constant)
            {
                if (Value == null)
                {
                    return "";
                }
                else
                {
                    return ((double)Value).ToString();
                }
            }
            else
            {
                return "";
            }
        }

        /*
        A.8.3 Expressions
        base_expression                 ::= expression
        expression1                     ::= expression 
        expression2                     ::= expression 
        expression3                     ::= expression 
        conditional_expression          ::= expression1 ? { attribute_instance } expression2 : expression3

        expression                      ::= primary 
                                            | unary_operator { attribute_instance } primary 
                                            | expression binary_operator { attribute_instance } expression 
                                            | conditional_expression 
                                            | string  

        constant_base_expression        ::= constant_expression
        constant_expression             ::= constant_primary          | unary_operator { attribute_instance } constant_primary          | constant_expression binary_operator { attribute_instance } constant_expression          | constant_expression ? { attribute_instance } constant_expression : constant_expression          | string
        constant_mintypmax_expression   ::= constant_expression          | constant_expression : constant_expression : constant_expression
        constant_range_expression       ::= constant_expression        | msb_constant_expression : lsb_constant_expression          | constant_base_expression +: width_constant_expression          | constant_base_expression -: width_constant_expression
        dimension_constant_expression   ::= constant_expression  
        lsb_constant_expression         ::= constant_expression  
        mintypmax_expression            ::= expression | expression : expression : expression  
        module_path_conditional_expression  ::= module_path_expression ? { attribute_instance } module_path_expression : module_path_expression 
        module_path_expression              ::= module_path_primary | unary_module_path_operator { attribute_instance } module_path_primary  | module_path_expression binary_module_path_operator { attribute_instance }                  module_path_expression          | module_path_conditional_expression 
        module_path_mintypmax_expression    ::= module_path_expression | module_path_expression : module_path_expression : module_path_expression
        msb_constant_expression         ::= constant_expression  
        range_expression                ::= expression  msb_constant_expression : lsb_constant_expression | base_expression +: width_constant_expression | base_expression -: width_constant_expression
        width_constant_expression ::= constant_expression
        */

        /*
        expression ::= 
              primary
            | unary_operator { attribute_instance } primary
            | inc_or_dec_expression
            | "(" operator_assignment ")"
            | expression binary_operator { attribute_instance } expression
            | conditional_expression
            | inside_expression
            | tagged_union_expression

        inside_expression ::= expression "inside" "{" open_range_list "}"
        open_range_list ::= open_value_range { , open_value_range }
        open_value_range ::= value_range
        operator_assignment ::= 
            variable_lvalue assignment_operator expression
        assignment_operator ::=
            = | += | -= | *= | /= | %= | &= | |= | ^= | <<= | >>= | <<<= | >>>=

        inc_or_dec_expression ::=
              inc_or_dec_operator { attribute_instance } variable_lvalue
            | variable_lvalue { attribute_instance } inc_or_dec_operator
        
        conditional_expression ::= 
            cond_predicate ? { attribute_instance } expression : expression
        
        constant_expression ::=
              constant_primary
            | unary_operator { attribute_instance } constant_primary
            | constant_expression binary_operator { attribute_instance } constant_expression
            | constant_expression ? { attribute_instance } constant_expression : constant_expression
        constant_mintypmax_expression ::=
              constant_expression
            | constant_expression : constant_expression : constant_expression
        constant_param_expression ::=
            constant_mintypmax_expression | data_type | "$"
        param_expression ::= 
            mintypmax_expression | data_type | "$"

        */
        public static Expression? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            Expression? exp = parseCreate(word, nameSpace,false);
            if (exp == null) return null;

            if(exp is AssignmentOperator)
            {
                exp.Reference.AddError("assignment operator needs ()");
            }

            return exp;
        }

        public static Expression? Create(string text, ParsedDocument parsedDocument, bool systemVerilog,NameSpace nameSpace)
        {
            WordScanner wordScanner = new WordScanner(parsedDocument.CodeDocument, parsedDocument, systemVerilog);
            return ParseCreate(wordScanner, nameSpace);
        }

        public static Expression CreateTempExpression(string text)
        {
            Expression expression = new Expression();
            expression.Primary = new TempPrimary(text);
            return expression;
        }

        public static Expression? ParseCreateInBracket(WordScanner word, NameSpace nameSpace)
        {
            Expression? exp = parseCreate(word, nameSpace,true);
            if (exp == null) return null;

            return exp;
        }

        public static Expression? parseCreate(WordScanner word, NameSpace nameSpace,bool acceptAssignment)
        {
            Expression expression = new Expression();
            List<Operator> operatorsStock = new List<Operator>();
            WordReference reference = word.GetReference();

            List<Primary> rpnPrimaries = new List<Primary>();

            parseExpressionPrimaries(word, nameSpace, rpnPrimaries, operatorsStock, ref reference,acceptAssignment);
            expression.Reference = reference;
            while (operatorsStock.Count != 0)
            {
                rpnPrimaries.Add(operatorsStock.Last());
                operatorsStock.RemoveAt(operatorsStock.Count - 1);
            }
            if (rpnPrimaries.Count == 0)
            {
                return null;
            }

            if (rpnPrimaries.Count == 1 && rpnPrimaries[0] is Primary)
            {
                Primary primary = rpnPrimaries[0] as Primary;
                //expression.Constant = primary.Constant;
                //expression.Value = primary.Value;
                //expression.BitWidth = primary.BitWidth;
                //expression.Primary = primary;
                return primary;
            }

            if (!word.Active) return expression;

            bool incDec = false;
            if (rpnPrimaries.Count == 2)
            {
                if (rpnPrimaries[0] is IncDecOperator || rpnPrimaries[1] is IncDecOperator)
                {
                    incDec = true;
                }
            }


            // parse rpn
            List<Primary> primaries = new List<Primary>();
            for (int i = 0; i < rpnPrimaries.Count; i++)
            {
                Primary item = rpnPrimaries[i];
                if (item is BinaryOperator)
                {
                    if (primaries.Count < 2) return null;
                    BinaryOperator? op = item as BinaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[primaries.Count - 2], primaries[primaries.Count - 1]);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.Add(primary);
                }
                else if (item is AssignmentOperator)
                {
                    if (primaries.Count < 2) return null;
                    AssignmentOperator? op = item as AssignmentOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[primaries.Count - 2], primaries[primaries.Count - 1]);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.Add(primary);
                    if (primaries.Count != 1)
                    {
                        primary.Reference.AddError("assignment operator needs ()");
                    }
                }
                else if (item is UnaryOperator)
                {
                    if (primaries.Count < 1) return null;
                    UnaryOperator? op = item as UnaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[0]);
                    primaries.RemoveAt(0);
                    primaries.Add(primary);
                }
                else if (item is InsideOperator)
                {
                    if (primaries.Count < 1) return null;
                    InsideOperator? op = item as InsideOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[0]);
                    primaries.RemoveAt(0);
                    primaries.Add(primary);
                }
                else if (item is IncDecOperator)
                {
                    if (primaries.Count < 1) return null;
                    IncDecOperator? op = item as IncDecOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[0]);
                    primaries.RemoveAt(0);
                    primaries.Add(primary);
                }
                else if (item is TenaryOperator)
                {
                    if (primaries.Count < 3) return null;
                    TenaryOperator? op = item as TenaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(primaries[primaries.Count - 3], primaries[primaries.Count - 2], primaries[primaries.Count - 1]);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.RemoveAt(primaries.Count - 1);
                    primaries.Add(primary);
                }
                else
                {
                    primaries.Add(item as Primary);
                }
            }
            if (primaries.Count == 1)
            {
                //expression.Constant = Primaries[0].Constant;
                //expression.BitWidth = Primaries[0].BitWidth;
                //expression.Value = Primaries[0].Value;
                //expression.Primary = Primaries[0];
            }
            else
            {
                return null;
            }

            primaries[0].IncrementDecrement = incDec;
            return primaries[0];
            //            return expression;
        }



        // parse lvalue expression or task reference
        public static Expression? ParseCreateVariableLValue(WordScanner word, NameSpace nameSpace)
        {
            Expression expression = new Expression();
            List<Operator> operatorsStock = new List<Operator>();
            List<Primary> rpnPrimaries = new List<Primary>();

            WordReference reference = word.GetReference();

            parseVariableLValue(word, nameSpace, rpnPrimaries, operatorsStock);
            expression.Reference = reference;
            while (operatorsStock.Count != 0)
            {
                rpnPrimaries.Add(operatorsStock.Last());
                operatorsStock.RemoveAt(operatorsStock.Count - 1);
            }
            if (rpnPrimaries.Count == 0)
            {
                return null;
            }

            if (rpnPrimaries.Count == 1 && rpnPrimaries[0] is Primary)
            {
                Primary primary = rpnPrimaries[0] as Primary;
                //expression.Constant = primary.Constant;
                //expression.Value = primary.Value;
                //expression.BitWidth = primary.BitWidth;
                //expression.Primary = primary;
                //return expression;
                return primary;
            }
            // parse rpn
            List<Primary> Primaries = new List<Primary>();
            for (int i = 0; i < rpnPrimaries.Count; i++)
            {
                Primary item = rpnPrimaries[i];
                if (item is Primary)
                {
                    Primaries.Add(item as Primary);
                }
                else if (item is BinaryOperator)
                {
                    if (Primaries.Count < 2) return null;
                    BinaryOperator? op = item as BinaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(Primaries[0], Primaries[1]);
                    Primaries.RemoveAt(0);
                    Primaries.RemoveAt(0);
                    Primaries.Add(primary);
                }
                else if (item is AssignmentOperator)
                {
                    if (Primaries.Count < 2) return null;
                    AssignmentOperator? op = item as AssignmentOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(Primaries[0], Primaries[1]);
                    Primaries.RemoveAt(0);
                    Primaries.RemoveAt(0);
                    Primaries.Add(primary);
                }
                else if (item is UnaryOperator)
                {
                    if (Primaries.Count < 1) return null;
                    UnaryOperator? op = item as UnaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(Primaries[0]);
                    Primaries.RemoveAt(0);
                    Primaries.Add(primary);
                }
                else if (item is TenaryOperator)
                {
                    if (Primaries.Count < 3) return null;
                    TenaryOperator? op = item as TenaryOperator;
                    if (op == null) throw new Exception();
                    Primary primary = op.Operate(Primaries[0], Primaries[1], Primaries[2]);
                    Primaries.RemoveAt(0);
                    Primaries.RemoveAt(0);
                    Primaries.RemoveAt(0);
                    Primaries.Add(primary);
                }
                else
                {
                    return null;
                }
            }
            if (Primaries.Count == 1)
            {
                return Primaries[0];
                //                expression.Constant = Primaries[0].Constant;
                //                expression.BitWidth = Primaries[0].BitWidth;
                //                expression.Value = Primaries[0].Value;
            }
            else
            {
                return null;
            }

        }

        private static bool parseExpressionPrimaries(WordScanner word, NameSpace nameSpace, List<Primary> Primaries, List<Operator> operatorStock, ref WordReference reference,bool acceptAssignment)
        {
            // ++(primary),--(primary)
            Primary? primary = Primary.ParseCreate(word, nameSpace);
            if (primary != null)
            {
                Primaries.Add(primary);
                IncDecOperator incDecOperator = IncDecOperator.ParseCreate(word);
                if (incDecOperator != null)
                {
                    Primaries.Add(incDecOperator);
                }
            }
            else
            {
                // ++(primary),-(primary)
                IncDecOperator incDecOperator = IncDecOperator.ParseCreate(word);
                if (incDecOperator != null)
                {
                    if (word.Eof)
                    {
                        word.AddError("illegal unary Operator");
                        return false;
                    }
                    primary = Primary.ParseCreate(word, nameSpace);
                    if (primary == null)
                    {
                        word.AddError("illegal unary Operator");
                        return false;
                    }
                    Primaries.Add(primary);
                    Primaries.Add(incDecOperator);
                }
                else
                {
                    UnaryOperator unaryOperator = UnaryOperator.ParseCreate(word);
                    if (unaryOperator != null)
                    {
                        if (word.Eof)
                        {
                            word.AddError("illegal unary Operator");
                            return false;
                        }
                        primary = Primary.ParseCreate(word, nameSpace);
                        if (primary == null)
                        {
                            word.AddError("illegal unary Operator");
                            return false;
                        }
                        Primaries.Add(primary);
                        Primaries.Add(unaryOperator);
                        //                        addOperator(unaryOperator, Primaries, operatorStock);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (word.Eof) return true;


            if (word.GetCharAt(0) == '?')
            {
                word.MoveNext();
                do
                {
                    if (!parseExpressionPrimaries(word, nameSpace, Primaries, operatorStock, ref reference, false))
                    {
                        word.AddError("illegal binary Operator");
                        break;
                    }
                    if (word.GetCharAt(0) == ':')
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(": expected");
                        break;
                    }
                    if (!parseExpressionPrimaries(word, nameSpace, Primaries, operatorStock, ref reference, false))
                    {
                        word.AddError("illegal binary Operator");
                        break;
                    }
                    Primaries.Add(TenaryOperator.Create());
                } while (false);
            }

            if(word.Text == "inside")
            {
                InsideOperator? insideOperator = InsideOperator.ParseCreate(word, nameSpace);
                if (insideOperator != null) Primaries.Add(insideOperator); 
            }

            BinaryOperator? binaryOperator = BinaryOperator.ParseCreate(word);
            if (binaryOperator != null)
            {
                addOperator(binaryOperator, Primaries, operatorStock);
            }
            else if(acceptAssignment)
            {
                AssignmentOperator? assignmentOperator = AssignmentOperator.ParseCreate(word);
                if (assignmentOperator != null)
                {
                    addOperator(assignmentOperator, Primaries, operatorStock);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            if (!parseExpressionPrimaries(word, nameSpace, Primaries, operatorStock, ref reference,false))
            {
                word.AddError("illegal binary Operator");
            }

            return true;
        }

        private static bool parseVariableLValue(WordScanner word, NameSpace nameSpace, List<Primary> Primaries, List<Operator> operatorStock)
        {
            Primary? primary = Primary.ParseCreateLValue(word, nameSpace);
            if (primary != null)
            {
                Primaries.Add(primary);
            }
            return true;
        }

        private static void addOperator(Operator newOperator, List<Primary> expressionItems, List<Operator> operatorStock)
        {
            while (operatorStock.Count != 0 && operatorStock.Last().Precedence <= newOperator.Precedence)
            {
                Operator popOperator = operatorStock.Last();
                operatorStock.RemoveAt(operatorStock.Count - 1);
                expressionItems.Add(popOperator);
            }
            operatorStock.Add(newOperator);
        }

    }

}
