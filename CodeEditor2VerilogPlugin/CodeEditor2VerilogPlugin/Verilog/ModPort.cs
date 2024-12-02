using DynamicData.Binding;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            ModPort modport = new ModPort(interface_) { BeginIndexReference = word.CreateIndexReference(), DefinitionReference = word.CrateWordReference(), Name = word.Text, Parent = nameSpace, Project = word.Project };
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            if (interface_ != null)
            {
                if (!interface_.NamedElements.ContainsKey(modport.Name))
                {
                    interface_.NamedElements.Add(modport.Name, modport);
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
                    return true;
                }
                word.MoveNext();

                Expressions.Expression? expression;
                if(direction == Port.DirectionEnum.Output)
                {
                    expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace);
                }
                else
                {
                    expression = Expressions.Expression.ParseCreate(word, nameSpace);
                }
                if (expression == null) return true;

                if (word.Text != ")")
                {
                    return true;
                }
                word.MoveNext();

                registerPort(name, direction, expression);
            }


            if (!General.IsIdentifier(word.Text)) return false;
            {
                string identifier = word.Text;

                Expressions.Expression? expression;
                if (direction == Port.DirectionEnum.Output)
                {
                    expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace);
                }
                else
                {
                    expression = Expressions.Expression.ParseCreate(word, nameSpace);
                }
                if (expression == null) return true;

                registerPort(identifier, direction, expression);
            }

            return false;
        }

        private void registerPort(string identifier,Port.DirectionEnum direction, Expression? expression)
        {
            Port port = new Port { Direction = direction, Name = identifier };
            port.Expression = expression;
            VariableReference? vRef = expression as VariableReference;
            IntegerVectorValueVariable? intVectorVar = vRef?.Variable as IntegerVectorValueVariable;

            if (!NamedElements.ContainsKey(port.Name))
            {
                if (direction == Port.DirectionEnum.Input)
                {
                    Net net = Net.Create(identifier, Net.NetTypeEnum.Wire, null);

                    if (intVectorVar != null)
                    {
                        foreach (PackedArray pa in intVectorVar.PackedDimensions)
                        {
                            net.PackedDimensions.Add(pa.Clone());
                        }
                    }
                    NamedElements.Add(net.Name, net);
                }
                else
                {
                    DataObjects.DataTypes.IntegerVectorType dType = Verilog.DataObjects.DataTypes.IntegerVectorType.Create(Verilog.DataObjects.DataTypes.DataTypeEnum.Logic, false, null);
                    Logic logic = Logic.Create(identifier, dType);

                    if (intVectorVar != null)
                    {
                        foreach (PackedArray pa in intVectorVar.PackedDimensions)
                        {
                            logic.PackedDimensions.Add(pa.Clone());
                        }
                    }

                    NamedElements.Add(logic.Name, logic);
                }
            }
            if (!Ports.ContainsKey(port.Name))
            {
                Ports.Add(port.Name, port);
            }

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
            public string Name { get; internal set; } = "";
            public DirectionEnum Direction { get; internal set; } = DirectionEnum.Undefined;
            public Expressions.Expression Expression { get; internal set; } = null;
        }
        



    }
}
