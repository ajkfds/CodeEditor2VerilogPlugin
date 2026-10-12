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
        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
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
            /// <summary>
            /// Optional formal arguments (tf_port_list) of this production.
            /// </summary>
            public List<DataObjects.Port>? Ports { get; set; } = null;
            /// <summary>
            /// Optional return data type ("void" or data_type). Null = implicit (no type declared).
            /// </summary>
            public DataObjects.DataTypes.IDataType? ReturnType { get; set; } = null;
            /// <summary>
            /// True if the production declares "void" return type.
            /// </summary>
            public bool IsVoid { get; set; } = false;
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

        /// <summary>
        /// Built-in data type keywords accepted as a production return type.
        /// </summary>
        private static readonly HashSet<string> productionTypeKeywords = new HashSet<string>()
        {
            "bit", "logic", "reg",
            "byte", "shortint", "int", "longint", "integer", "time",
            "shortreal", "real", "realtime", "string"
        };

        /// <summary>
        /// Register a production as a Function element in the namespace so that
        /// production calls (e.g. add(10)) resolve through the existing function
        /// call path. Formal arguments are parsed with ParseTfPortItems into the
        /// Function's own namespace so that they are visible in the production body.
        /// </summary>
        private static Verilog.Function? registerProduction(WordScanner word, NameSpace nameSpace, Production production)
        {
            if (word.CompletionContext != null) return null;
            if (word.Prototype) return null;

            // create the Function with Project set (Function.Create does not set
            // NamedItem.Project, which FunctionCall resolution requires)
            Verilog.Function function = new Verilog.Function(nameSpace)
            {
                Name = production.Name,
                Project = word.Project,
                DefinitionReference = word.CrateWordReference()
            };
            if (production.ReturnType != null && !production.IsVoid)
            {
                // record the return type so that call sites can resolve the value
                function.ReturnVariable = DataObjects.DataObject.Create(production.Name, production.ReturnType);
            }
            if (!nameSpace.NamedElements.ContainsKey(production.Name))
            {
                nameSpace.NamedElements.Add(production.Name, function);
                return function;
            }
            // already registered (re-parse): reuse the existing element
            INamedElement? existing = nameSpace.NamedElements[production.Name];
            return existing as Verilog.Function;
        }

        /// <summary>
        /// <summary>
        /// Pre-scan the whole randsequence body and register production identifiers
        /// before parsing the bodies, so that forward production references resolve.
        /// Detection rules (heuristic, safe against common expressions):
        ///   identifier ":" ...                      (no-argument production)
        ///   typeKeyword identifier ( ...            (production with formal arguments)
        /// </summary>
        private static void preRegisterProductions(WordScanner word, NameSpace nameSpace)
        {
            if (word.CompletionContext != null) return;
            if (word.Prototype) return;

            WordScanner probe = word.Clone(true);
            try
            {
                string prev = "";
                while (!probe.Eof && probe.Text != "endsequence")
                {
                    string cur = probe.Text;
                    string next = probe.NextText;
                    if (General.IsIdentifier(cur) && !General.ListOfKeywords.Contains(cur) &&
                        !nameSpace.NamedElements.ContainsKey(cur) &&
                        (next == ":" || (next == "(" && (prev == "void" || productionTypeKeywords.Contains(prev)))))
                    {
                        Verilog.Function function = new Verilog.Function(nameSpace)
                        {
                            Name = cur,
                            Project = word.Project,
                            DefinitionReference = probe.CrateWordReference()
                        };
                        nameSpace.NamedElements.Add(cur, function);
                    }
                    prev = cur;
                    probe.MoveNext();
                }
            }
            finally
            {
                probe.Dispose();
            }
        }

        /// <summary>
        /// Fallback consumption of "( tf_port_list )" when the production cannot be
        /// registered (prototype / completion context mode).
        /// </summary>
        private static void skipPortList(WordScanner word, NameSpace nameSpace)
        {
            while (!word.Eof && word.Text != ")" && word.Text != "endsequence")
            {
                if (word.Text == "(")
                {
                    // nested default value parentheses: skip balanced
                    int depth = 1;
                    word.MoveNext();
                    while (!word.Eof && depth > 0)
                    {
                        if (word.Text == "(") depth++;
                        else if (word.Text == ")") depth--;
                        word.MoveNext();
                    }
                    continue;
                }
                word.MoveNext();
            }
        }

        public static RandsequenceStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "randsequence") return null;

            RandsequenceStatement statement = new RandsequenceStatement() { BeginIndexReference = word.CreateIndexReference() };
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // pre-register production identifiers so that forward references
            // (e.g. "main : first second;" before "first : ...;" is parsed)
            // resolve within a single parse pass
            preRegisterProductions(word, nameSpace);

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

                // [ data_type_or_void ] : optional return type of the production
                // production ::= [ data_type_or_void ] production_identifier [ ( tf_port_list ) ] [ : ] production_body
                // Only built-in type keywords are accepted here to avoid consuming
                // adjacent production references (e.g. "first second" in a body).
                DataObjects.DataTypes.IDataType? productionReturnType = null;
                bool productionIsVoid = false;
                if (word.Text == "void")
                {
                    productionIsVoid = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
                else if (productionTypeKeywords.Contains(word.Text) && General.IsIdentifier(word.NextText))
                {
                    productionReturnType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
                }

                // Production identifier
                if (!General.IsIdentifier(word.Text)) break;

                bool hasFormalArguments = word.NextText == "(";
                production.Name = word.Text;
                production.ReturnType = productionReturnType;
                production.IsVoid = productionIsVoid;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                // register the production so that production calls (e.g. add(10))
                // resolve through the existing function call path
                // (forward references are resolved by re-parse)
                Verilog.Function? prodFunc = registerProduction(word, nameSpace, production);

                // ( tf_port_list ) : formal arguments of the production
                if (hasFormalArguments && word.Text == "(")
                {
                    word.MoveNext();
                    if (prodFunc != null)
                    {
                        DataObjects.Port.ParseTfPortItems(word, nameSpace, prodFunc);
                        production.Ports = prodFunc.PortsList;
                    }
                    else
                    {
                        // fallback: consume the port list without registration
                        skipPortList(word, nameSpace);
                    }
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(") required");
                    }
                }

                // optional [ : ] before production_body
                if (word.Text == ":")
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                // Parse production items
                // when the production has formal arguments, parse the body inside
                // the production's own namespace so that the arguments are visible
                // (e.g. void add(int y) : { x = x + y; })
                NameSpace productionBodyNameSpace = (NameSpace?)prodFunc ?? nameSpace;
                while (!word.Eof && word.Text != "endsequence" && word.Text != ":")
                {
                    var item = ParseProductionItem(word, productionBodyNameSpace);
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
            statement.LastIndexReference = word.CreateIndexReference();
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

                // { statement block } : production body containing statements
                // (e.g. first : { x = x + 20; }; / add(int y) : { x = x + y; };)
                if (word.Text == "{")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    while (!word.Eof && word.Text != "}")
                    {
                        if (word.Text == ";")
                        {
                            word.MoveNext();
                            continue;
                        }
                        var statement = Statements.ParseCreateStatementOrNull(word, nameSpace);
                        if (statement == null) word.MoveNext();
                    }
                    if (word.Text == "}")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    // optional trailing ";" after the block
                    if (word.Text == ";") word.MoveNext();
                    continue;
                }

                // production call / production rule reference (e.g. "first", "add(10)")
                // an identifier followed by end-of-item tokens is a production reference,
                // not an expression
                if (General.IsIdentifier(word.Text) && !General.ListOfKeywords.Contains(word.Text) &&
                    (word.NextText == "(" || word.NextText == ";" || word.NextText == "," || word.NextText == "}" ||
                     word.NextText == "endsequence" || word.NextText == ":" || General.IsIdentifier(word.NextText)))
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    if (word.NextText == "(")
                    {
                        // production call with arguments: consume "(" expressions ")".
                        // port checking against the production's formal arguments is not
                        // performed because the formal arguments may not be parsed yet
                        // (forward reference within the same randsequence)
                        word.MoveNext();
                        while (!word.Eof && word.Text != ")")
                        {
                            if (word.Text == ",")
                            {
                                word.MoveNext();
                                continue;
                            }
                            var arg = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (arg == null) word.MoveNext();
                        }
                        if (word.Text == ")") word.MoveNext();
                    }
                    else
                    {
                        word.MoveNext();
                    }
                    continue;
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
                if(selectItem.Expression == null)
                {
                    return null;
                }

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
