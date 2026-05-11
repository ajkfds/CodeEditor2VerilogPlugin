using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.ModuleItems
{
    /// <summary>
    /// Represents a Net Alias declaration
    /// IEEE 1800-2017 SystemVerilog
    /// 
    /// net_alias ::= "alias" net_lvalue "=" net_lvalue { "=" net_lvalue } ;
    /// 
    /// list_of_net_aliases ::= net_alias_item { , net_alias_item }
    /// net_alias_item ::= net_lvalue
    /// </summary>
    public class NetAlias
    {
        protected NetAlias() { }

//        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// List of net lvalues being aliased
        /// </summary>
        public List<Expression> NetLvalues { get; set; } = new List<Expression>();

        /// <summary>
        /// The expression being aliased to
        /// </summary>
        public Expression? Expression { get; set; }

        public IndexReference BeginIndexReference { get; set; }
        public IndexReference? LastIndexReference { get; set; }

        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "alias")
            {
                return false;
            }

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // alias


            // Parse list_of_net_aliases
            // net_alias_item ::= net_lvalue


            // Parse net_lvalue
            Expression? lvalue = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, true);
            if (lvalue != null)
            {
            }
            else
            {
                word.AddError("illegal net lvalue");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }

            while(!word.Eof && word.Text == "=")
            {
                // Expect = 
                if (word.Text != "=")
                {
                    word.AddError("= expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Parse expression
                Expressions.Expression? aliasExpression = Expression.ParseCreate(word, nameSpace);
                if (aliasExpression == null)
                {
                    word.AddError("illegal expression");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }
            }

            // Semicolon
            if (word.Text != ";")
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            return true;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return null;
        }

        public void DisposeSubReference()
        {
            foreach (var expr in NetLvalues)
            {
                expr.DisposeSubReference(true);
            }
            Expression?.DisposeSubReference(true);
        }
    }
}
