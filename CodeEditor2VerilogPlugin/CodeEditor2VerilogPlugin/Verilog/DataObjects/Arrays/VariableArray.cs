using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class VariableArray
    {

        /*
        variable_dimension  : Dynamic Array         : unsized_dimension     : "[]"
                            : Unpacked Array        : unpacked_dimension    : "[" constant_range "]"
                                                                            : "[" constant_expression "]"
                            : AssociativeArray      : associative_dimension : "[" data_type "]"
                                                    : "[" "*" "]"
                            : Queue                 : queue_dimension       : "[" "$" [ : constant_expression ] "]" 
        */
        public int? Size { get; protected set; } = null;
        public bool Constant { get; protected set; } = false;

        public virtual string CreateString()
        {
            throw new NotImplementedException();
        }

        public virtual void AppendLabel(AjkAvaloniaLibs.Contorls.ColorLabel label)
        {
            throw new NotImplementedException();
        }
        public virtual AjkAvaloniaLibs.Contorls.ColorLabel GetLabel()
        {
            throw new NotImplementedException();
        }
        public virtual bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            throw new NotImplementedException();
        }

        // Constructors
        public static VariableArray? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            word.MoveNext(); // [

            if (word.Text == "$")
            {
                return Queue.ParseCreate(word, nameSpace);
            }

            // associative_dimension    ::= [data_type] | [ * ]
            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // *

                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new AssociativeArray(null);
                }
                else
                {
                    word.AddError("] expected");
                    return null;
                }
            }

            Expressions.Expression? expression = Expressions.Expression.parseCreate(word, nameSpace, false);
            if(expression != null)
            {
                return parseCreateUnpackedArray(word, nameSpace, expression);
            }

            DataTypes.IDataType? indexDataType = DataTypes.DataType.ParseCreate(word, nameSpace, null);
            if(indexDataType != null)
            {
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return new AssociativeArray(indexDataType);
                }
                else
                {
                    word.AddError("] expected");
                }
            }
            return null;
        }

        private static UnPackedArray? parseCreateUnpackedArray(WordScanner word, NameSpace nameSpace,Expressions.Expression expression)
        {
            // unpacked_dimension   ::= [constant_range] | [constant_expression]
            // constant_range       ::= constant_expression : constant_expression

            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression);
            }

            if (word.Text != ":")
            {
                word.AddError(": expected");
                return null;
            }
            word.MoveNext();

            Expressions.Expression? expression1 = Expressions.Expression.parseCreate(word, nameSpace, false);
            if(expression1 == null)
            {
                word.AddError("expression expected");
                return null;
            }
            if (word.Text == "]")
            {
                word.MoveNext();
                return new UnPackedArray(expression, expression1);
            }

            return null;
        }

    }
}
