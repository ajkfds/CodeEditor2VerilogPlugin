using pluginVerilog.Verilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog
{
    public class Attribute
    {
        /*
        A.9.1 Attributes
        attribute_instance ::= (* attr_spec { , attr_spec }  *)
        attr_spec ::=            attr_name = constant_expression          | attr_name
        attr_name ::= identifier 
            */
        protected Attribute() { }

        public Dictionary<string, Verilog.Expressions.Expression?> AttributeSpecs = new Dictionary<string, Verilog.Expressions.Expression?>();

        public static Attribute ParseCreate(Verilog.WordScanner word,NameSpace nameSpace)
        {
            if (word.Text != "(*") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            Attribute attr = new Attribute();
            while ( !word.Eof )
            {
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal attr_name");
                    break;
                }
                string name = word.Text;
                word.MoveNext();

                Verilog.Expressions.Expression? expression = null;

                if ( word.Text == "=")
                {
                    word.MoveNext();    // =

                    expression = Verilog.Expressions.Expression.ParseCreate(word, nameSpace);
                    if(expression != null && !expression.Constant)
                    {
                        expression.Reference.AddError("must be constant");
                        expression = null;
                    }
                }
                attr.AttributeSpecs.Add(name, expression);

                if (word.Text == "*)") break;
                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }
            
            while(word.Text != "*)" && !word.Eof)
            {
                if (General.ListOfKeywords.Contains(word.Text)) break;
                word.MoveNext();
            }

            if (word.Text == "*)")
            {
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            return attr;
        }

    }


}
