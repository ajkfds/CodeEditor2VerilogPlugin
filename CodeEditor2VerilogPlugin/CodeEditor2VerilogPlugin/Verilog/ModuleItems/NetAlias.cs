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
    /// net_alias ::= alias ( list_of_net_aliases ) = expression ;
    /// 
    /// list_of_net_aliases ::= net_alias_item { , net_alias_item }
    /// net_alias_item ::= net_lvalue
    /// </summary>
    public class NetAlias : Item
    {
        protected NetAlias() { }

        public string Name { get; protected set; } = "";
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

            NetAlias netAlias = new NetAlias
            {
                BeginIndexReference = beginReference
            };

            // Expect opening parenthesis
            if (word.Text != "(")
            {
                word.AddError("( expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            word.MoveNext(); // (

            // Parse list_of_net_aliases
            // net_alias_item ::= net_lvalue
            bool first = true;
            while (!word.Eof && word.Text != ")")
            {
                if (first)
                {
                    first = false;
                }
                else if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
                else if (word.Text == ")")
                {
                    break;
                }
                else
                {
                    word.AddError(") or , expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }

                // Parse net_lvalue
                Expression? lvalue = Expression.ParseCreateNetLvalue(word, nameSpace);
                if (lvalue != null)
                {
                    netAlias.NetLvalues.Add(lvalue);
                }
                else
                {
                    word.AddError("illegal net lvalue");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                    return true;
                }
            }

            // Closing parenthesis
            if (word.Text == ")")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError(") expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }

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
            netAlias.Expression = Expression.ParseCreate(word, nameSpace);
            if (netAlias.Expression == null)
            {
                word.AddError("illegal expression");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
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

            netAlias.LastIndexReference = word.CreateIndexReference();

            return true;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
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
