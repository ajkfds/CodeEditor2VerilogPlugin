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

        public virtual void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            throw new NotImplementedException();
        }
        public virtual AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            throw new NotImplementedException();
        }
        public virtual bool CheckIndexRangeError(Expressions.Expression indexExpression)
        {
            throw new NotImplementedException();
        }

        // Constructors
        public static IArray? ParseCreate(DataObject dataObject,WordScanner word, NameSpace nameSpace)
        {
            word.MoveNext(); // [

            if (word.Text == "]")
            {
                word.MoveNext();
                return DynamicArray.Create(dataObject);
            }

            if (word.Text == "$")
            {   // [$]
                return Queue.ParseCreate(dataObject, word, nameSpace);
            }

            // associative_dimension    ::= [data_type] | [ * ]
            if (word.Text == "*")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // *

                if (word.Text == "]")
                {
                    word.MoveNext();
                    return AssociativeArray.Create(dataObject,null);
                }
                else
                {
                    word.AddError("] expected");
                    return null;
                }
            }

            DataTypes.IDataType? indexDataType = DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
            if (indexDataType != null)
            {
                if (word.Text == "]")
                {
                    word.MoveNext();
                    return AssociativeArray.Create(dataObject, indexDataType);
                }
                else
                {
                    word.AddError("] expected");
                }
            }

            Expressions.Expression? expression = Expressions.Expression.ParseCreate(word, nameSpace, false);
            if(expression != null)
            {
                return UnPackedArray.ParseCreate(word, nameSpace, expression);
            }

            return null;
        }


    }
}
