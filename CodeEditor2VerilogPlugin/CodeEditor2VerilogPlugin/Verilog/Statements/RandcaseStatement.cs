using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    /// <summary>
    /// randcase_statement ::=
    ///     "randcase" [ item_case { item_case } ] "endcase"
    /// 
    /// item_case ::=
    ///     expression [:] statement_or_null
    ///   | "default" [ :] statement_or_null
    /// </summary>
    public class RandcaseStatement : IStatement
    {
        protected RandcaseStatement() { }
        public string Name { get; protected set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Case items in randcase
        /// </summary>
        public List<RandcaseItem> Items { get; set; } = new List<RandcaseItem>();

        public class RandcaseItem
        {
            /// <summary>
            /// Optional weight expression (null for default)
            /// </summary>
            public Expression? Weight { get; set; }

            /// <summary>
            /// Statement to execute
            /// </summary>
            public IStatement? Statement { get; set; }

            /// <summary>
            /// Whether this is a default case
            /// </summary>
            public bool IsDefault { get; set; }
        }

        public static RandcaseStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "randcase") return null;

            RandcaseStatement statement = new RandcaseStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse case items
            while (!word.Eof && word.Text != "endcase")
            {
                RandcaseItem item = new RandcaseItem();

                if (word.Text == "default")
                {
                    item.IsDefault = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    // Parse weight expression
                    item.Weight = Expression.ParseCreate(word, nameSpace);
                }

                // Optional colon
                if (word.Text == ":")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }

                // Parse statement
                item.Statement = Statements.ParseCreateStatementOrNull(word, nameSpace);

                statement.Items.Add(item);
            }

            // endcase
            if (word.Text == "endcase")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return statement;
        }

        public void DisposeSubReference()
        {
            //if (Statement != null)
            //{
            //    Statement.DisposeSubReference();
            //}
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }
    }
}
