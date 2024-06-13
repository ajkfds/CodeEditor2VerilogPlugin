using DynamicData.Binding;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Nets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public class ModPort : NameSpace
    {
        protected ModPort(NameSpace parent) : base(parent.BuildingBlock, parent)
        {
        }

        /*
            modport_declaration     ::= modport modport_item { , modport_item } ; 
            modport_item            ::= modport_identifier ( modport_ports_declaration { , modport_ports_declaration } ) 
            modport_ports_declaration ::=
                                  { attribute_instance } modport_simple_ports_declaration 
                                | { attribute_instance } modport_tf_ports_declaration 
                                | { attribute_instance } modport_clocking_declaration 
            modport_clocking_declaration ::= "clocking" clocking_identifier 
            modport_simple_ports_declaration ::= 
                                port_direction modport_simple_port { , modport_simple_port } 
            modport_simple_port ::= 
                                  port_identifier 
                                | "." port_identifier ( [ expression ] ) 
            modport_tf_ports_declaration ::= 
                                import_export modport_tf_port { , modport_tf_port }
            modport_tf_port ::=  method_prototype 
                                | tf_identifier 
            import_export ::= import | export 
         */
        //        modport_declaration::= modport modport_item { , modport_item };
        
        public static bool Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "modport") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (!parse_modport_item(word, nameSpace)) break;
                if (word.Text == ";")
                {
                    word.MoveNext();
                    break;
                }
                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError("illegal modport_item");
                }
            }
            return true;
        }

        //        modport_item::= modport_identifier(modport_ports_declaration { , modport_ports_declaration } ) 
        //        modport_ports_declaration::=  { attribute_instance } modport_simple_ports_declaration 
        //                                    | { attribute_instance } modport_tf_ports_declaration
        //                                    | { attribute_instance } modport_clocking_declaration
        private static bool parse_modport_item(WordScanner word, NameSpace nameSpace)
        {
            if (!General.IsIdentifier(word.Text)) return false;
            Interface? interface_ = nameSpace.BuildingBlock as Interface;

            if (interface_ == null) return false;

            ModPort modport = new ModPort(interface_);
            modport.Name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (interface_ != null)
            {
                if (!interface_.ModPorts.ContainsKey(modport.Name))
                {
                    interface_.ModPorts.Add(modport.Name, modport);
                }
                if (!interface_.NameSpaces.ContainsKey(modport.Name))
                {
                    interface_.NameSpaces.Add(modport.Name, modport);
                }
            }

            if (word.Text != "(") return false;
            word.MoveNext();

            while (!word.Eof)
            {
                switch (word.Text)
                {
                    case "clocking":
                        if (!modport.parse_modport_clocking_declaration(word, nameSpace)) return false;
                        break;
                    case "input":
                    case "output":
                    case "inout":
                    case "ref":
                        if (!modport.parse_modport_simple_ports_declaration(word, nameSpace)) return false;
                        break;
                    default:
                        if (!modport.parse_modport_simple_port(word, nameSpace, Port.DirectionEnum.Undefined)) return false;
                        break;
                }

                if (word.Text != ",") break;
                word.MoveNext();
            }
            if (word.Text != ")") return false;
            word.MoveNext();

            return false;
        }

        //modport_clocking_declaration::= "clocking" clocking_identifier
        internal bool parse_modport_clocking_declaration(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "clocking") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            return true;
        }
        //modport_simple_ports_declaration ::=  port_direction modport_simple_port { , modport_simple_port }
        internal bool parse_modport_simple_ports_declaration(WordScanner word, NameSpace nameSpace)
        {
            Port.DirectionEnum direction = Port.DirectionEnum.Undefined;

            switch (word.Text)
            {
                case "input":
                    direction = Port.DirectionEnum.Input;
                    break;
                case "output":
                    direction = Port.DirectionEnum.Output;
                    break;
                case "inout":
                    direction = Port.DirectionEnum.Inout;
                    break;
                case "ref":
                    break;
                default:
                    throw new Exception();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            while (!word.Eof)
            {
                if (!parse_modport_simple_port(word, nameSpace, direction)) break;
                if (word.Text != ",") break;
                if (word.NextText == "input" || word.NextText == "output" || word.NextText == "inout" || word.NextText == "ref") break;
                if (word.NextText == "clocking") break;

                word.MoveNext();
            }

            return true;
        }
        //modport_simple_port::= port_identifier | "." port_identifier([expression] )
        internal bool parse_modport_simple_port(WordScanner word, NameSpace nameSpace, Port.DirectionEnum direction)
        {
            if (word.Text == ".") // modport expression
            {
                word.MoveNext();

                if (!General.IsIdentifier(word.Text))
                {
                    return false;
                }
                string name = word.Text;
                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();

                if (word.Text != "(")
                {
                    return false;
                }
                word.MoveNext();

                Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression == null) return false;

                if (word.Text != ")")
                {
                    return false;
                }
                word.MoveNext();

                Port port = new Port { Direction = direction, Name = name };
                port.Expression = expression;

                if (!DataObjects.ContainsKey(port.Name))
                {
                    if (direction == Port.DirectionEnum.Input)
                    {
                        Verilog.DataObjects.Nets.Net net = Verilog.DataObjects.Nets.Net.Create(Verilog.DataObjects.Nets.Net.NetTypeEnum.Wire, null);
                        net.Name = name;

                        if(expression!=null && expression.BitWidth > 1)
                        {
                            net.PackedDimensions.Add(Verilog.DataObjects.Range.CreateTempRange((int)expression.BitWidth - 1, 0));
                        }
                        DataObjects.Add(net.Name, net);
                    }
                    else
                    {
                        DataObjects.DataTypes.IntegerVectorType dType = Verilog.DataObjects.DataTypes.IntegerVectorType.Create(Verilog.DataObjects.DataTypes.DataTypeEnum.Logic, false, null);
                        Verilog.DataObjects.Variables.Logic logic = Verilog.DataObjects.Variables.Logic.Create(dType);
                        logic.Name = name;
                        if (expression != null && expression.BitWidth > 1)
                        {
                            logic.PackedDimensions = new List<DataObjects.Range>();
                            logic.PackedDimensions.Add(Verilog.DataObjects.Range.CreateTempRange((int)expression.BitWidth - 1, 0));
                        }

                        DataObjects.Add(logic.Name, logic);
                    }
                }
                if (!Ports.ContainsKey(port.Name))
                {
                    Ports.Add(port.Name, port);
                    return true;
                }
            }


            if (!General.IsIdentifier(word.Text)) return false;
            {
                Port port = new Port { Direction = direction, Name = word.Text };

                Expressions.Expression expression = Expressions.Expression.ParseCreate(word, nameSpace);
                if (expression == null) return false;

                port.Expression = expression;
                //word.Color(CodeDrawStyle.ColorType.Variable);
                //word.MoveNext();

                if (!Ports.ContainsKey(port.Name))
                {
                    Ports.Add(port.Name, port);
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, Port> Ports = new Dictionary<string, Port>();

        public class Port
        {
            public enum DirectionEnum
            {
                Undefined,
                Input,
                Output,
                Inout,
                Ref
            }

            public string Name { get; internal set; }
            public DirectionEnum Direction { get; internal set; } = DirectionEnum.Undefined;
            public Expressions.Expression Expression { get; internal set; } = null;
        }
        



    }
}
