using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.BuildingBlocks
{
    /// <summary>
    /// SystemVerilog Clocking Declaration
    /// IEEE 1800-2017
    /// 
    /// clocking_declaration ::=
    ///     [ default ] clocking [ clocking_identifier ] clocking_event ; { clocking_item } endclocking [ : clocking_identifier ]
    ///   | global clocking [ clocking_identifier ] clocking_event ; endclocking [ : clocking_identifier ]
    /// 
    /// clocking_event ::= @( event_control )
    /// clocking_item ::=
    ///     { attribute_instance } clocking_direction list_of_clocking_decl_assign ;
    ///   | { attribute_instance } delay_control ;
    /// 
    /// clocking_direction ::=
    ///     input [ clocking_direction ]
    ///   | output [ clocking_direction ]
    ///   | inout
    /// </summary>
    public class Clocking : NameSpace
    {
        public IndexReference BeginIndexReference { get; set; }
        public IndexReference? BlockBeginIndexReference { get; set; }
        public IndexReference? LastIndexReference { get; set; }
        public WordReference? DefinitionReference { get; set; }

        public string Name { get; set; } = "";
        public bool IsDefault { get; set; } = false;
        public bool IsGlobal { get; set; } = false;

        /// <summary>
        /// Clocking event (e.g., @(posedge clk))
        /// </summary>
        public Expression? ClockingEvent { get; set; }

        /// <summary>
        /// Clocking signals: direction -> list of (name, expression)
        /// </summary>
        public List<ClockingSignal> Signals { get; set; } = new List<ClockingSignal>();

        public class ClockingSignal
        {
            public enum DirectionEnum
            {
                Input,
                Output,
                Inout
            }

            public DirectionEnum Direction { get; set; }
            public string Name { get; set; } = "";
            public Expression? Expression { get; set; }
            public Expression? Delay { get; set; }
        }

        protected Clocking(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }

        public static Clocking? ParseCreate(WordScanner word, NameSpace nameSpace, Attribute? attribute)
        {
            Clocking clocking = new Clocking(nameSpace.BuildingBlock, nameSpace)
            {
                BeginIndexReference = word.CreateIndexReference()
            };

            // Check for default or global
            if (word.Text == "default")
            {
                clocking.IsDefault = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else if (word.Text == "global")
            {
                clocking.IsGlobal = true;
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            if (word.Text != "clocking")
            {
                return null;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            // Optional clocking_identifier
            if (General.IsIdentifier(word.Text))
            {
                clocking.Name = word.Text;
                clocking.DefinitionReference = word.CrateWordReference();
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }
            else
            {
                // Anonymous clocking block
                clocking.Name = "";
                clocking.DefinitionReference = word.CrateWordReference();
            }

            // Clocking event: @( ...)
            if (word.Text == "@")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                if (word.Text == "(")
                {
                    word.MoveNext();
                    clocking.ClockingEvent = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ")")
                    {
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError(") expected");
                    }
                }
                else
                {
                    word.AddError("( expected");
                }
            }
            else
            {
                word.AddError("@expected");
            }

            // Semicolon
            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
            }

            clocking.BlockBeginIndexReference = word.CreateIndexReference();

            // Parse clocking items until endclocking
            while (!word.Eof && word.Text != "endclocking")
            {
                if (word.Eof)
                {
                    word.AddError("endclocking expected");
                    break;
                }

                // clocking_item ::=
                //     { attribute_instance } clocking_direction list_of_clocking_decl_assign ;
                //   | { attribute_instance } delay_control ;

                // Check for delay control (##delay or #delay)
                if (word.Text == "##")
                {
                    // Cycle delay control
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    Expression? delayExpr = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ";")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    continue;
                }
                else if (word.Text == "#")
                {
                    // Delay control
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    Expression? delayExpr = Expression.ParseCreate(word, nameSpace);
                    if (word.Text == ";")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    continue;
                }

                // clocking_direction: input, output, or inout
                ClockingSignal.DirectionEnum direction = ClockingSignal.DirectionEnum.Input;

                switch (word.Text)
                {
                    case "input":
                        direction = ClockingSignal.DirectionEnum.Input;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();

                        // Check for optional sub-direction (input input, input output, etc.)
                        if (word.Text == "input" || word.Text == "output" || word.Text == "inout")
                        {
                            // Already handled at outer level, this is a nested direction
                            // Continue to parse the signals
                        }
                        break;

                    case "output":
                        direction = ClockingSignal.DirectionEnum.Output;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        break;

                    case "inout":
                        direction = ClockingSignal.DirectionEnum.Inout;
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        break;

                    default:
                        // Not a clocking direction, skip to next
                        word.MoveNext();
                        continue;
                }

                // Parse list_of_clocking_decl_assign
                // Each item is: signal_identifier [ = expression ] [ with [ expression ] ]
                while (!word.Eof && word.Text != ";")
                {
                    if (word.Text == ",")
                    {
                        word.MoveNext();
                        continue;
                    }

                    // Check for endclocking
                    if (word.Text == "endclocking")
                    {
                        break;
                    }

                    ClockingSignal signal = new ClockingSignal
                    {
                        Direction = direction
                    };

                    // signal_identifier
                    if (!General.IsIdentifier(word.Text))
                    {
                        word.AddError("illegal identifier");
                        word.MoveNext();
                        continue;
                    }

                    signal.Name = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Variable);
                    word.MoveNext();

                    // Optional = expression
                    if (word.Text == "=")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        signal.Expression = Expression.ParseCreate(word, nameSpace);
                    }

                    // Optional with expression (for queues, etc.)
                    if (word.Text == "with")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        // Skip for now - not commonly used in clocking blocks
                        word.MoveNext();
                    }

                    // Add signal
                    clocking.Signals.Add(signal);

                    // Register the clocking signal in namespace
                    if (!word.Prototype)
                    {
                        // Create a variable for the clocking signal
                        Variable? var = null;
                        switch (direction)
                        {
                            case ClockingSignal.DirectionEnum.Input:
                                var = Variables.Input.Create(signal.Name, DataObjects.DataTypes.LogicType.Create(false, null));
                                break;
                            case ClockingSignal.DirectionEnum.Output:
                                var = Variables.Output.Create(signal.Name, DataObjects.DataTypes.LogicType.Create(false, null));
                                break;
                            case ClockingSignal.DirectionEnum.Inout:
                                var = Variables.Inout.Create(signal.Name, DataObjects.DataTypes.LogicType.Create(false, null));
                                break;
                        }
                        if (var != null && !clocking.NamedElements.ContainsKey(signal.Name))
                        {
                            clocking.NamedElements.Add(signal.Name, var);
                        }
                    }

                    if (word.Text == ",")
                    {
                        continue;
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
                else if (word.Text != "endclocking")
                {
                    word.AddError("; expected");
                    word.SkipToKeyword(";");
                    if (word.Text == ";") word.MoveNext();
                }
            }

            clocking.LastIndexReference = word.CreateIndexReference();

            // endclocking keyword
            if (word.Text == "endclocking")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();

                // Optional : clocking_identifier
                if (word.Text == ":")
                {
                    word.MoveNext();
                    if (word.Text == clocking.Name || string.IsNullOrEmpty(clocking.Name))
                    {
                        word.Color(CodeDrawStyle.ColorType.Identifier);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("clocking name mismatch");
                        word.MoveNext();
                    }
                }
            }
            else
            {
                word.AddError("endclocking expected");
            }

            return clocking;
        }

        /// <summary>
        /// Parse default clocking statement
        /// default clocking clocking_identifier ;
        /// </summary>
        public static bool ParseDefaultClocking(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "default") return false;

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Text != "clocking")
            {
                word.AddError("clocking expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal clocking identifier");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }

            // Store the default clocking reference
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            if (word.Text == ";")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }
            else
            {
                word.AddError("; expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
            }

            return true;
        }
    }
}
