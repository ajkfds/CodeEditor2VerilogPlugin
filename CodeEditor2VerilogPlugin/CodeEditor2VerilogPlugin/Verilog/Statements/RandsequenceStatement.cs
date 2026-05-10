using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    /// <summary>
    /// randsequence_statement ::=
    ///     "randsequence" [ ( list_of_randselectors ) ] production
    /// 
    /// list_of_randselectors ::= randselectors { , randselectors }
    /// randselectors ::= [ data_type ] tf_identifier
    /// 
    /// production ::=
    ///     production_identifier { production_item } "endsequence" [ : production_identifier ]
    /// 
    /// production_item ::=
    ///     production_rule
    ///   | [ production_identifier : ] statement_or_null
    /// 
    /// production_rule ::=
    ///     randselect [ list_of_item_details { , list_of_item_details } ] production_body
    /// 
    /// randselect ::=
    ///     "rand_join" [ ( expression ) ] production_item { production_item } "join"
    ///   | list_of_randselects
    /// 
    /// list_of_randselects ::= randselect { , randselect } [ : statement_or_null ]
    /// randselect ::= expression [ : weight ]
    /// 
    /// production_body ::=
    ///     list_of_production_items
    ///   | "unique" { list_of_production_items } { , unique { list_of_production_items } }
    ///   | "if" ( expression ) [ production_body ]
    ///     { "else" "if" ( expression ) [ production_body ] }
    ///     [ "else" [ production_body ] ]
    ///   | "case" ( expression ) { case_item } "endcase"
    /// 
    /// case_item ::=
    ///     expression_or_range { , expression_or_range } : production_body
    ///   | "default" [ : ] production_body
    /// 
    /// expression_or_range ::=
    ///     expression
    ///   | expression : expression
    /// 
    /// list_of_item_details ::= item_detail { , item_detail }
    /// item_detail ::=
    ///     production_body
    ///   | "if" ( expression ) [ production_body ]
    ///   | "randcase" item_case { item_case } "endcase"
    /// 
    /// item_case ::=
    ///     expression : production_body
    ///   | "default" [ : ] production_body
    /// </summary>
    public class RandsequenceStatement : IStatement
    {
        protected RandsequenceStatement() { }
        public string Name { get; protected set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Optional list of randselectors
        /// </summary>
        public List<RandSelector> Selectors { get; set; } = new List<RandSelector>();

        /// <summary>
        /// Productions in this randsequence
        /// </summary>
        public List<Production> Productions { get; set; } = new List<Production>();

        public class RandSelector
        {
            /// <summary>
            /// Optional data type
            /// </summary>
            public DataObjects.DataTypes.IDataType? DataType { get; set; }

            /// <summary>
            /// Task/function identifier
            /// </summary>
            public string Identifier { get; set; } = "";
        }

        public class Production
        {
            public string Name { get; set; } = "";
            public List<ProductionItem> Items { get; set; } = new List<ProductionItem>();
        }

        public class ProductionItem
        {
            public enum ItemType
            {
                RandJoin,
                RandSelect,
                Statement,
                IfElse,
                Case
            }

            public ItemType Type { get; set; }

            /// <summary>
            /// For RandSelect and RandJoin
            /// </summary>
            public List<RandSelectItem> SelectItems { get; set; } = new List<RandSelectItem>();
            public Expression? RandJoinCondition { get; set; }
            public bool IsRandJoin { get; set; }

            /// <summary>
            /// For Statement type
            /// </summary>
            public IStatement? Statement { get; set; }

            /// <summary>
            /// For IfElse type
            /// </summary>
            public Expression? Condition { get; set; }
            public List<ProductionItem> ThenItems { get; set; } = new List<ProductionItem>();
            public List<ProductionItem> ElseItems { get; set; } = new List<ProductionItem>();

            /// <summary>
            /// For Case type
            /// </summary>
            public Expression? CaseExpression { get; set; }
            public List<CaseItem> CaseItems { get; set; } = new List<CaseItem>();

            /// <summary>
            /// Unique constraint flag
            /// </summary>
            public bool IsUnique { get; set; }
        }

        public class RandSelectItem
        {
            public Expression? Expression { get; set; }
            public Expression? Weight { get; set; }
            public IStatement? Statement { get; set; }
        }

        public class CaseItem
        {
            public List<Expression> Expressions { get; set; } = new List<Expression>();
            public Expression? RangeStart { get; set; }
            public Expression? RangeEnd { get; set; }
            public List<ProductionItem> Items { get; set; } = new List<ProductionItem>();
            public bool IsDefault { get; set; }
        }

        public static async Task<RandsequenceStatement?> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "randsequence") return null;

            RandsequenceStatement statement = new RandsequenceStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Optional list of randselectors
            if (word.Text == "(")
            {
                word.MoveNext();
                while (!word.Eof && word.Text != ")")
                {
                    RandSelector selector = new RandSelector();

                    // Check for optional data type
                    selector.DataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace,null);

                    // tf_identifier
                    if (General.IsIdentifier(word.Text))
                    {
                        selector.Identifier = word.Text;
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }

                    statement.Selectors.Add(selector);

                    if (word.Text == ",")
                    {
                        word.MoveNext();
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            // Parse productions
            while (!word.Eof && word.Text != "endsequence")
            {
                Production production = new Production();

                // Production identifier
                if (General.IsIdentifier(word.Text) && word.NextText != "(")
                {
                    production.Name = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                // Parse production items
                while (!word.Eof && word.Text != "endsequence" && word.Text != ":")
                {
                    var item = ParseProductionItem(word, nameSpace);
                    if (item != null)
                    {
                        production.Items.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }

                // Check for production label (: production_identifier)
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (General.IsIdentifier(word.Text))
                    {
                        if (string.IsNullOrEmpty(production.Name))
                        {
                            production.Name = word.Text;
                        }
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }

                statement.Productions.Add(production);
            }

            // endsequence
            if (word.Text == "endsequence")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Optional : production_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (General.IsIdentifier(word.Text))
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }
            }

            return statement;
        }

        private static ProductionItem? ParseProductionItem(WordScanner word, NameSpace nameSpace)
        {
            ProductionItem item = new ProductionItem();

            switch (word.Text)
            {
                case "rand_join":
                    item.IsRandJoin = true;
                    item.Type = ProductionItem.ItemType.RandJoin;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();

                    // Optional expression
                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        item.RandJoinCondition = Expression.ParseCreate(word, nameSpace);
                        if (word.Text == ")") word.MoveNext();
                    }
                    break;

                case "unique":
                    item.IsUnique = true;
                    item.Type = ProductionItem.ItemType.RandSelect;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;

                case "if":
                    item.Type = ProductionItem.ItemType.IfElse;
                    return ParseIfElseProduction(word, nameSpace, item);

                case "case":
                    item.Type = ProductionItem.ItemType.Case;
                    return ParseCaseProduction(word, nameSpace, item);

                case "randcase":
                    // Inline randcase
                    return ParseRandcaseProduction(word, nameSpace);
            }

            // Parse rand selects
            while (!word.Eof && word.Text != "endsequence" && word.Text != ":")
            {
                if (word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }

                if (word.Text == "join")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                }

                RandSelectItem selectItem = new RandSelectItem();
                selectItem.Expression = Expression.ParseCreate(word, nameSpace);

                // Optional weight
                if (word.Text == ":")
                {
                    word.MoveNext();
                    selectItem.Weight = Expression.ParseCreate(word, nameSpace);
                }

                item.SelectItems.Add(selectItem);

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
            }

            return item;
        }

        private static ProductionItem ParseIfElseProduction(WordScanner word, NameSpace nameSpace, ProductionItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // if
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                item.Condition = Expression.ParseCreate(word, nameSpace);
                if (word.Text == ")") word.MoveNext();
            }

            // Parse then items (production body)
            ParseProductionBody(word, nameSpace, item.ThenItems);

            // Else if / else
            while (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "if")
                {
                    word.MoveNext();
                    ProductionItem elseIfItem = new ProductionItem
                    {
                        Type = ProductionItem.ItemType.IfElse
                    };

                    if (word.Text == "(")
                    {
                        word.MoveNext();
                        elseIfItem.Condition = Expression.ParseCreate(word, nameSpace);
                        if (word.Text == ")") word.MoveNext();
                    }

                    ParseProductionBody(word, nameSpace, elseIfItem.ThenItems);
                    item.ElseItems.Add(elseIfItem);
                }
                else
                {
                    ParseProductionBody(word, nameSpace, item.ElseItems);
                    break;
                }
            }

            return item;
        }

        private static ProductionItem ParseCaseProduction(WordScanner word, NameSpace nameSpace, ProductionItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // case
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                item.CaseExpression = Expression.ParseCreate(word, nameSpace);
                if (word.Text == ")") word.MoveNext();
            }

            // Parse case items
            while (!word.Eof && word.Text != "endcase")
            {
                CaseItem caseItem = new CaseItem();

                if (word.Text == "default")
                {
                    caseItem.IsDefault = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    while (!word.Eof && word.Text != ":" && word.Text != "endcase")
                    {
                        Expression? expr = Expression.ParseCreate(word, nameSpace);
                        if (expr != null) caseItem.Expressions.Add(expr);

                        if (word.Text == ":")
                        {
                            word.MoveNext();
                            break;
                        }

                        if (word.Text == ",")
                        {
                            word.MoveNext();
                        }
                    }
                }

                // Optional : before production body
                if (word.Text == ":")
                {
                    word.MoveNext();
                }

                ParseProductionBody(word, nameSpace, caseItem.Items);
                item.CaseItems.Add(caseItem);
            }

            if (word.Text == "endcase")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return item;
        }

        private static ProductionItem ParseRandcaseProduction(WordScanner word, NameSpace nameSpace)
        {
            ProductionItem item = new ProductionItem
            {
                Type = ProductionItem.ItemType.Case,
                CaseExpression = null
            };

            word.Color(CodeDrawStyle.ColorType.Keyword); // randcase
            word.MoveNext();

            // Parse case items
            while (!word.Eof && word.Text != "endcase")
            {
                CaseItem caseItem = new CaseItem();

                if (word.Text == "default")
                {
                    caseItem.IsDefault = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else
                {
                    Expression? weightExpr = Expression.ParseCreate(word, nameSpace);
                    if (weightExpr != null) caseItem.Expressions.Add(weightExpr);
                }

                if (word.Text == ":")
                {
                    word.MoveNext();
                }

                ParseProductionBody(word, nameSpace, caseItem.Items);
                item.CaseItems.Add(caseItem);
            }

            if (word.Text == "endcase")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return item;
        }

        private static void ParseProductionBody(WordScanner word, NameSpace nameSpace, List<ProductionItem> items)
        {
            while (!word.Eof && word.Text != "endsequence" && word.Text != "else" && word.Text != ":")
            {
                if (word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }

                var item = ParseProductionItem(word, nameSpace);
                if (item != null)
                {
                    items.Add(item);
                }
                else
                {
                    break;
                }
            }
        }

        public void DisposeSubReference()
        {
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
