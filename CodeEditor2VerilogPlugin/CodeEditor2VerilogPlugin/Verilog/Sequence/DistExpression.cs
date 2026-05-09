using pluginVerilog.Verilog.Expressions;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Sequence
{
    /// <summary>
    /// Represents a distribution expression: expression [ dist { dist_list } ]
    /// 
    /// dist_list ::= dist_item { , dist_item }
    /// dist_item ::= value_range [ : weight ]
    /// weight ::= expression | expression : expression
    /// value_range ::= expression | range_expression
    /// 
    /// Examples:
    ///     var dist { [1:3] := 5, [4:7] := 2, 8 := 1 }
    ///     var dist { 0 := 1, 1 := 2 }
    ///     var dist { [0:$] := 1 }  // uniform distribution
    /// </summary>
    public class DistExpression
    {
        public Expression? BaseExpression { get; set; }
        public List<DistItem> DistItems { get; set; } = new List<DistItem>();

        /// <summary>
        /// Parse dist expression: expression [ dist { dist_list } ]
        /// </summary>
        public static DistExpression? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            // First parse the base expression
            Expression? baseExpr = Expression.ParseCreate(word, nameSpace);
            if (baseExpr == null) return null;

            DistExpression distExpr = new DistExpression { BaseExpression = baseExpr };

            // Check for dist keyword
            if (word.Text != "dist")
            {
                // No distribution, just return the base expression wrapped
                return distExpr;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // dist

            if (word.Text != "{")
            {
                word.AddError("{ expected after dist");
                return distExpr;
            }
            word.MoveNext(); // {

            // Parse dist items
            while (!word.Eof && word.Text != "}")
            {
                DistItem? item = DistItem.ParseCreate(word, nameSpace);
                if (item != null)
                {
                    distExpr.DistItems.Add(item);
                }
                else
                {
                    break;
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
                else if (word.Text == "}")
                {
                    break;
                }
                else
                {
                    // Skip to next comma or closing brace
                    while (!word.Eof && word.Text != "," && word.Text != "}")
                    {
                        word.MoveNext();
                    }
                    if (word.Text == ",") word.MoveNext();
                }
            }

            if (word.Text == "}")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext(); // }
            }

            return distExpr;
        }
    }

    /// <summary>
    /// Represents a single dist item: value_range [ : weight ]
    /// 
    /// weight ::= expression | expression : expression
    /// 
    /// Examples:
    ///     [1:3] := 5      // range with weight 5
    ///     4 := 2          // single value with weight 2
    ///     [0:7] := 1 : 2  // range with weight 1:2 (min:max)
    /// </summary>
    public class DistItem
    {
        public Expression? RangeStart { get; set; }
        public Expression? RangeEnd { get; set; }
        public bool IsRange { get; set; } = false;
        public Expression? Weight { get; set; }
        public Expression? WeightMin { get; set; }
        public Expression? WeightMax { get; set; }
        public bool IsWeightRange { get; set; } = false;
        public bool IsInclusive { get; set; } = true; // := is inclusive (default), :/ is exclusive

        /// <summary>
        /// Parse a dist item: value_range [ := | :/ weight ]
        /// </summary>
        public static DistItem? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            DistItem item = new DistItem();

            // Parse value_range: expression [ : expression ] or just expression
            Expression? startExpr = Expression.ParseCreate(word, nameSpace);
            if (startExpr == null) return null;
            item.RangeStart = startExpr;

            // Check for range
            if (word.Text == ":")
            {
                word.MoveNext();
                Expression? endExpr = Expression.ParseCreate(word, nameSpace);
                if (endExpr != null)
                {
                    item.RangeEnd = endExpr;
                    item.IsRange = true;
                }
            }

            // Check for weight operator := or :/
            if (word.Text == ":")
            {
                word.MoveNext();
                if (word.Text == "/" || word.Text == ":")
                {
                    // :/ means exclusive distribution
                    if (word.Text == "/")
                    {
                        item.IsInclusive = false;
                        word.MoveNext();
                    }
                    else if (word.Text == ":")
                    {
                        // Could be weight range like 1:2
                        word.MoveNext();
                        Expression? weightMin = Expression.ParseCreate(word, nameSpace);
                        if (weightMin != null && word.Text == ":")
                        {
                            word.MoveNext();
                            Expression? weightMax = Expression.ParseCreate(word, nameSpace);
                            if (weightMax != null)
                            {
                                item.WeightMin = weightMin;
                                item.WeightMax = weightMax;
                                item.IsWeightRange = true;
                            }
                        }
                        else
                        {
                            item.Weight = weightMin;
                        }
                    }
                }
                else
                {
                    // Just := operator
                    if (word.Text == "=")
                    {
                        word.MoveNext();
                        Expression? weight = Expression.ParseCreate(word, nameSpace);
                        item.Weight = weight;
                    }
                    else
                    {
                        // Just weight expression
                        Expression? weight = Expression.ParseCreate(word, nameSpace);
                        item.Weight = weight;
                    }
                }
            }
            else if (word.Text == "=")
            {
                word.MoveNext();
                if (word.Text == ":")
                {
                    word.MoveNext();
                    item.IsInclusive = false; // :/ operator
                }
                else if (word.Text == "=")
                {
                    word.MoveNext();
                    // := operator, already inclusive
                }
                Expression? weight2 = Expression.ParseCreate(word, nameSpace);
                if (weight2 != null && word.Text == ":")
                {
                    word.MoveNext();
                    Expression? weightMax2 = Expression.ParseCreate(word, nameSpace);
                    if (weightMax2 != null)
                    {
                        item.WeightMin = weight2;
                        item.WeightMax = weightMax2;
                        item.IsWeightRange = true;
                    }
                }
                else
                {
                    item.Weight = weight2;
                }
            }

            return item;
        }
    }
}
