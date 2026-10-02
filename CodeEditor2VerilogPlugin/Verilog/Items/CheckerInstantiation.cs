using pluginVerilog.Verilog.BuildingBlocks;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.Items
{
    /// <summary>
    /// checker_instantiation ::=
    ///     ps_checker_identifier name_of_instance ( [list_of_checker_port_connections] ) ;
    ///
    /// list_of_checker_port_connections ::=
    ///       ordered_checker_port_connection { , ordered_checker_port_connection }
    ///     | named_checker_port_connection { , named_checker_port_connection }
    /// ordered_checker_port_connection ::= { attribute_instance } [ property_actual_arg ]
    /// named_checker_port_connection ::= { attribute_instance } . checker_port_identifier ( [ property_actual_arg ] )
    /// </summary>
    public class CheckerInstantiation
    {
        public class Instance
        {
            public string? Name { get; set; }
            public BuildingBlocks.Checker? Checker { get; set; }
            public Dictionary<string, Expressions.Expression?> PortConnections { get; } = new Dictionary<string, Expressions.Expression?>();
            public List<Expressions.Expression?> OrderedConnections { get; } = new List<Expressions.Expression?>();
            public Verilog.WordReference? DefinitionReference { get; set; }
        }

        public Instance InstanceItem { get; } = new Instance();

        public static bool Parse(WordScanner word, NameSpace nameSpace, BuildingBlocks.Checker checker)
        {
            if (!General.IsSimpleIdentifier(word.Text)) return false;

            CheckerInstantiation instantiation = new CheckerInstantiation();
            instantiation.InstanceItem.Checker = checker;
            instantiation.InstanceItem.DefinitionReference = word.CrateWordReference();

            // ps_checker_identifier
            instantiation.InstanceItem.Name = word.Text;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            // name_of_instance : instance identifier
            if (General.IsSimpleIdentifier(word.Text))
            {
                instantiation.InstanceItem.Name = word.Text;
                word.Color(CodeDrawStyle.ColorType.Identifier);
                word.MoveNext();
            }

            if (word.Text != "(")
            {
                word.AddError("( expected");
                word.SkipToKeyword(";");
                if (word.Text == ";") word.MoveNext();
                return true;
            }
            word.MoveNext();

            bool named = false;
            while (!word.Eof && word.Text != ")")
            {
                if (word.Text == "(*")
                {
                    Attribute.ParseCreate(word, nameSpace);
                    continue;
                }
                if (word.Text == ".")
                {
                    // named_checker_port_connection
                    named = true;
                    word.MoveNext();
                    if (!General.IsSimpleIdentifier(word.Text))
                    {
                        word.AddError("checker port identifier expected");
                        break;
                    }
                    string portName = word.Text;
                    word.Color(CodeDrawStyle.ColorType.Variable);
                    word.MoveNext();
                    if (word.Text != "(")
                    {
                        instantiation.InstanceItem.PortConnections[portName] = null;
                        if (word.Text == ",") { word.MoveNext(); continue; }
                        break;
                    }
                    word.MoveNext();
                    if (word.Text == ")")
                    {
                        instantiation.InstanceItem.PortConnections[portName] = null;
                    }
                    else
                    {
                        Expressions.Expression? arg = Expressions.Expression.ParseCreate(word, nameSpace);
                        instantiation.InstanceItem.PortConnections[portName] = arg;
                    }
                    if (word.Text == ")") word.MoveNext();
                    else word.AddError(") expected");
                }
                else
                {
                    // ordered_checker_port_connection
                    if (named)
                    {
                        word.AddError("named port connection started; ordered connection not allowed");
                        break;
                    }
                    if (word.Text == ")")
                    {
                        instantiation.InstanceItem.OrderedConnections.Add(null);
                        break;
                    }
                    Expressions.Expression? arg = Expressions.Expression.ParseCreate(word, nameSpace);
                    instantiation.InstanceItem.OrderedConnections.Add(arg);
                }

                if (word.Text == ",")
                {
                    word.MoveNext();
                    continue;
                }
                break;
            }

            if (word.Text == ")") word.MoveNext();
            else word.AddError(") expected");

            if (word.Text == ";")
            {
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
