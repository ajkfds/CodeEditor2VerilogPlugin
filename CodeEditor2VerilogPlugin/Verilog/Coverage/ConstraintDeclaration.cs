using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Coverage
{
    /// <summary>
    /// Represents a constraint declaration
    /// IEEE 1800-2017
    /// 
    /// constraint_declaration ::=
    ///     [ "static" ] "constraint" constraint_identifier [ constraint_proto_block ] 
    /// 
    /// constraint_proto_block ::=
    ///     "{" { constraint_block_item } "}"
    /// 
    /// constraint_block_item ::=
    ///     expression_or_dist ; 
    ///   | unique_constraint 
    ///   | implict_expression_or_dist ;
    ///   | if ( expression ) constraint_body [ else constraint_body ]
    ///   | foreach ( array_identifier [ , loop_variables ] ) constraint_body
    ///   | disable iff ( expression ) constraint_body
    ///   | solve list_of_variables [ before list_of_variables ] ;
    /// 
    /// expression_or_dist ::=
    ///     expression [ "dist" { dist_list } ]
    /// 
    /// constraint_body ::=
    ///     constraint_block_item
    ///   | [ "soft" ] constraint_expression
    /// 
    /// constraint_expression ::=
    ///     [ "soft" ] ( expression_or_dist )
    /// 
    /// unique_constraint ::=
    ///     "unique" { { attribute_instance } constraint_set { , { attribute_instance } constraint_set }
    /// 
    /// constraint_set ::= expression_or_dist
    /// 
    /// implict_expression_or_dist ::=
    ///     "implicit-expression-or-dist" identifier = expression_or_dist
    /// </summary>
    public class ConstraintDeclaration : INamedElement
    {
        public string Name { get; set; } = "";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        /// <summary>
        /// Whether this is a static constraint
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Constraint items (expressions, if-else, foreach, etc.)
        /// </summary>
        public List<ConstraintItem> Items { get; set; } = new List<ConstraintItem>();

        /// <summary>
        /// Optional end label
        /// </summary>
        public string? EndLabel { get; set; }

        /// <summary>
        /// Base class reference (for constraint inheritance)
        /// </summary>
        public string? ExtendsConstraint { get; set; }

        public class ConstraintItem
        {
            public enum ItemType
            {
                Expression,
                Unique,
                IfElse,
                ForEach,
                DisableIff,
                SolveBefore
            }

            public ItemType Type { get; set; }

            /// <summary>
            /// For Expression type
            /// </summary>
            public Expression? Expression { get; set; }

            /// <summary>
            /// For Unique type - list of expressions
            /// </summary>
            public List<Expression> UniqueExpressions { get; set; } = new List<Expression>();

            /// <summary>
            /// For IfElse type
            /// </summary>
            public Expression? Condition { get; set; }
            public List<ConstraintItem> ThenItems { get; set; } = new List<ConstraintItem>();
            public List<ConstraintItem> ElseItems { get; set; } = new List<ConstraintItem>();

            /// <summary>
            /// For ForEach type
            /// </summary>
            public string? ArrayIdentifier { get; set; }
            public List<string> LoopVariables { get; set; } = new List<string>();
            public List<ConstraintItem> LoopItems { get; set; } = new List<ConstraintItem>();

            /// <summary>
            /// For DisableIff type
            /// </summary>
            public Expression? DisableCondition { get; set; }
            public List<ConstraintItem> DisableItems { get; set; } = new List<ConstraintItem>();

            /// <summary>
            /// For SolveBefore type
            /// </summary>
            public List<string> SolveVariables { get; set; } = new List<string>();
            public List<string> BeforeVariables { get; set; } = new List<string>();

            /// <summary>
            /// Whether this is a soft constraint
            /// </summary>
            public bool IsSoft { get; set; }
        }

        public static ConstraintDeclaration? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // Check for static
            bool isStatic = false;
            if (word.Text == "static")
            {
                isStatic = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (word.Text != "constraint")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // constraint_identifier
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("constraint identifier expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return null;
            }

            ConstraintDeclaration constraint = new ConstraintDeclaration
            {
                Name = word.Text,
                IsStatic = isStatic
            };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // Parse optional constraint_proto_block or single expression
            if (word.Text == "{")
            {
                // Multi-line constraint block
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Parse constraint items until closing brace
                while (!word.Eof && word.Text != "}")
                {
                    var item = ParseConstraintItem(word, nameSpace);
                    if (item != null)
                    {
                        constraint.Items.Add(item);
                    }
                    else
                    {
                        // Skip invalid token
                        word.MoveNext();
                    }
                }

                // Closing brace
                if (word.Text == "}")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
            }
            else
            {
                // Single expression constraint
                var item = new ConstraintItem
                {
                    Type = ConstraintItem.ItemType.Expression
                };

                // Check for soft
                if (word.Text == "soft")
                {
                    item.IsSoft = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }

                if (word.Text == "(")
                {
                    word.MoveNext();
                    item.Expression = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }
                else
                {
                    item.Expression = Expression.ParseCreate(word, nameSpace);
                }

                if (item.Expression != null)
                {
                    constraint.Items.Add(item);
                }

                // Optional semicolon
                if (word.Text == ";")
                {
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                }
            }

            return constraint;
        }

        private static ConstraintItem? ParseConstraintItem(WordScanner word, NameSpace nameSpace)
        {
            ConstraintItem item = new ConstraintItem();

            switch (word.Text)
            {
                case "soft":
                    item.IsSoft = true;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    // Fall through to expression parsing
                    break;

                case "unique":
                    item.Type = ConstraintItem.ItemType.Unique;
                    return ParseUniqueConstraint(word, nameSpace, item);

                case "if":
                    item.Type = ConstraintItem.ItemType.IfElse;
                    return ParseIfElseConstraint(word, nameSpace, item);

                case "foreach":
                    item.Type = ConstraintItem.ItemType.ForEach;
                    return ParseForEachConstraint(word, nameSpace, item);

                case "disable":
                    if (word.NextText == "iff")
                    {
                        item.Type = ConstraintItem.ItemType.DisableIff;
                        return ParseDisableIffConstraint(word, nameSpace, item);
                    }
                    // Fall through to expression
                    break;

                case "solve":
                    item.Type = ConstraintItem.ItemType.SolveBefore;
                    return ParseSolveBeforeConstraint(word, nameSpace, item);
            }

            // Expression or dist
            item.Type = ConstraintItem.ItemType.Expression;
            item.Expression = Expression.ParseCreate(word, nameSpace);

            if (item.Expression == null) return null;
            // Optional semicolon
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return item;
        }

        private static ConstraintItem ParseUniqueConstraint(WordScanner word, NameSpace nameSpace, ConstraintItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Parse list of expressions
            while (!word.Eof && word.Text != ";" && word.Text != "}")
            {
                Expression? expr = Expression.ParseCreate(word, nameSpace);
                if (expr != null)
                {
                    item.UniqueExpressions.Add(expr);
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }

            // Semicolon
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return item;
        }

        private static ConstraintItem ParseIfElseConstraint(WordScanner word, NameSpace nameSpace, ConstraintItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // if
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();
                item.Condition = Expression.ParseCreate(word, nameSpace);
                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            // Then items
            if (word.Text == "{")
            {
                word.MoveNext();
                while (!word.Eof && word.Text != "}" && word.Text != "else")
                {
                    var thenItem = ParseConstraintItem(word, nameSpace);
                    if (thenItem != null) item.ThenItems.Add(thenItem);
                }
                if (word.Text == "}") word.MoveNext();
            }
            else
            {
                var thenItem = ParseConstraintItem(word, nameSpace);
                if (thenItem != null) item.ThenItems.Add(thenItem);
            }

            // Else items
            if (word.Text == "else")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "if")
                {
                    // Else if
                    var elseIfItem = new ConstraintItem
                    {
                        Type = ConstraintItem.ItemType.IfElse
                    };
                    elseIfItem = ParseIfElseConstraint(word, nameSpace, elseIfItem);
                    item.ElseItems.Add(elseIfItem);
                }
                else if (word.Text == "{")
                {
                    word.MoveNext();
                    while (!word.Eof && word.Text != "}")
                    {
                        var elseItem = ParseConstraintItem(word, nameSpace);
                        if (elseItem != null) item.ElseItems.Add(elseItem);
                    }
                    if (word.Text == "}") word.MoveNext();
                }
                else
                {
                    var elseItem = ParseConstraintItem(word, nameSpace);
                    if (elseItem != null) item.ElseItems.Add(elseItem);
                }
            }

            return item;
        }

        private static ConstraintItem ParseForEachConstraint(WordScanner word, NameSpace nameSpace, ConstraintItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // foreach
            word.MoveNext();

            if (word.Text == "(")
            {
                word.MoveNext();

                // Array identifier
                if (General.IsIdentifier(word.Text))
                {
                    item.ArrayIdentifier = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }

                // Optional loop variables
                while (word.Text == "," && General.IsIdentifier(word.NextText))
                {
                    word.MoveNext(); // comma
                    if (General.IsIdentifier(word.Text))
                    {
                        item.LoopVariables.Add(word.Text);
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
            }

            // Loop items
            if (word.Text == "{")
            {
                word.MoveNext();
                while (!word.Eof && word.Text != "}")
                {
                    var loopItem = ParseConstraintItem(word, nameSpace);
                    if (loopItem != null) item.LoopItems.Add(loopItem);
                }
                if (word.Text == "}") word.MoveNext();
            }
            else
            {
                var loopItem = ParseConstraintItem(word, nameSpace);
                if (loopItem != null) item.LoopItems.Add(loopItem);
            }

            return item;
        }

        private static ConstraintItem ParseDisableIffConstraint(WordScanner word, NameSpace nameSpace, ConstraintItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // disable
            word.MoveNext();

            if (word.Text == "iff")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext();
                    item.DisableCondition = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                }
            }

            // Disable items
            if (word.Text == "{")
            {
                word.MoveNext();
                while (!word.Eof && word.Text != "}")
                {
                    var disableItem = ParseConstraintItem(word, nameSpace);
                    if (disableItem != null) item.DisableItems.Add(disableItem);
                }
                if (word.Text == "}") word.MoveNext();
            }
            else
            {
                var disableItem = ParseConstraintItem(word, nameSpace);
                if (disableItem != null) item.DisableItems.Add(disableItem);
            }

            return item;
        }

        private static ConstraintItem ParseSolveBeforeConstraint(WordScanner word, NameSpace nameSpace, ConstraintItem item)
        {
            word.Color(CodeDrawStyle.ColorType.Keyword); // solve
            word.MoveNext();

            // Parse solve list
            while (General.IsIdentifier(word.Text))
            {
                item.SolveVariables.Add(word.Text);
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }

            // Optional "before" clause
            if (word.Text == "before")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                while (General.IsIdentifier(word.Text))
                {
                    item.BeforeVariables.Add(word.Text);
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();

                    if (word.Text == ",")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Semicolon
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            return item;
        }

        public CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }

        public void DisposeSubReference()
        {
        }
    }
}
