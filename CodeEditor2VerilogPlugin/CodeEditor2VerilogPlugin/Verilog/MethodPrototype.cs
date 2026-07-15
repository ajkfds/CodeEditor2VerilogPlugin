using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog
{
    public static class MethodPrototype
    {
        public static bool ParseCreateWithPureVirtual(WordScanner word, NameSpace nameSpace)
        {
            /*
            interface_class_method ::=
                "pure" "virtual" method_prototype ;

            method_prototype ::=
                  task_prototype
                | function_prototype

            task_prototype ::= task task_identifier [ ( [ tf_port_list ] ) ]
            function_prototype ::= function data_type_or_void function_identifier [ ( [ tf_port_list ] ) ]
             */
            if (word.Eof | word.Text != "pure") return false;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (word.Eof | word.Text != "virtual") return true;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (ParseCreate(word, nameSpace))
            {
                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            /*
            method_prototype ::=
                  task_prototype
                | function_prototype

            task_prototype ::= task task_identifier [ ( [ tf_port_list ] ) ]
            function_prototype ::= function data_type_or_void function_identifier [ ( [ tf_port_list ] ) ]

            class_constructor_prototype ::= 
                "extern" ["method_qualifier"] "function" ["class_scope"] new [ ( [ tf_port_list ] ) ] ;
             */
            switch (word.Text)
            {
                case "task":
                    Task.ParsePrototype(word, nameSpace);
                    return true;
                case "function":
                    Function.ParsePrototype(word, nameSpace);
                    return true;
                case "new":
                    // class_constructor_prototype: extern new [ ( [ tf_port_list ] ) ] ;
                    ParseConstructorPrototype(word, nameSpace);
                    return true;
                default:
                    word.AddError("illegal method prototype");
                    return false;
            }

        }

        /// <summary>
        /// Parse class constructor prototype for extern new declaration
        /// IEEE 1800-2017
        /// 
        /// class_constructor_prototype ::= 
        ///     "extern" ["method_qualifier"] "function" ["class_scope"] new [ ( [ tf_port_list ] ) ] ;
        /// 
        /// Note: The "extern" keyword is already consumed before calling this method
        /// when parsing class items. This method handles both:
        /// - "extern new (...)" in class items
        /// - "new (...)" as constructor prototype in method_qualifier context
        /// </summary>
        public static void ParseConstructorPrototype(WordScanner word, NameSpace nameSpace)
        {
            /*
            class_constructor_declaration ::= 
                function [ class_scope ] new [ ( [ tf_port_list ] ) ] ;
                { block_item_declaration }  
                [ super . new [ ( list_of_arguments ) ] ; ]  
                { function_statement_or_null } 
                endfunction [ : new ] 

            class_constructor_prototype ::= 
                extern [ method_qualifier ] function [ class_scope ] new [ ( [ tf_port_list ] ) ] ;
             */
            if (word.Text != "new") return;

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // new

            // Create constructor function
            Function constructor = new Function(nameSpace)
            {
                Name = "new",
                Project = word.Project,
                BeginIndexReference = beginReference
            };

            // Return type is void for constructor
            constructor.ReturnVariable = null;

            // Add to namespace
            if (nameSpace.BuildingBlock.NamedElements.ContainsKey(constructor.Name))
            {
                nameSpace.BuildingBlock.NamedElements.Replace(constructor.Name, constructor);
            }
            else
            {
                nameSpace.BuildingBlock.NamedElements.Add(constructor.Name, constructor);
            }

            // Parse optional port list: ( [ tf_port_list ] )
            if (word.Text == "(")
            {
                word.MoveNext(); // (

                // Parse port list
                if (word.Text != ")")
                {
                    // tf_port_item ::= { attribute_instance } [ tf_port_direction ] [ var ] data_type_or_implicit [ port_identifier { variable_dimension } [ = expression ] ]
                    while (!word.Eof && word.Text != ")" && word.Text != ";")
                    {
                        // Parse attribute if present
                        if (word.Text == "(*)")
                        {
                            Attribute.ParseCreate(word, nameSpace);
                            continue;
                        }

                        // Parse port direction
                        DataObjects.Port.DirectionEnum direction = DataObjects.Port.DirectionEnum.Input;
                        if (word.Text == "input")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            direction = DataObjects.Port.DirectionEnum.Input;
                        }
                        else if (word.Text == "output")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            direction = DataObjects.Port.DirectionEnum.Output;
                        }
                        else if (word.Text == "inout")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            direction = DataObjects.Port.DirectionEnum.Inout;
                        }
                        else if (word.Text == "ref")
                        {
                            word.Color(CodeDrawStyle.ColorType.Keyword);
                            word.MoveNext();
                            direction = DataObjects.Port.DirectionEnum.Ref;
                        }

                        // Parse data type
                        IDataType? dataType = DataTypeFactory.ParseCreate(word, nameSpace, null);
                        if (dataType == null)
                        {
                            dataType = LogicType.Create(false, null);
                        }

                        // Parse port identifier
                        if (General.IsIdentifier(word.Text))
                        {
                            string portName = word.Text;
                            word.Color(CodeDrawStyle.ColorType.Variable);
                            word.MoveNext();

                            // Create port
                            Variable? var = Variable.Create(portName, dataType);
                            DataObjects.Port? port = DataObjects.Port.Create(portName, word.Project, direction, var);
                            if (port != null)
                            {
                                constructor.Ports.Add(portName, port);
                                constructor.PortsList.Add(port);
                                if (port.DataObject != null)
                                {
                                    constructor.NamedElements.Add(portName, port.DataObject);
                                }
                            }

                            // Handle variable dimensions
                            while (word.Text == "[" && !word.Eof)
                            {
                                // Variable dimensions are handled by the parser
                                word.MoveNext();
                                // Skip to matching ]
                                int depth = 1;
                                while (!word.Eof && depth > 0)
                                {
                                    if (word.Text == "[") depth++;
                                    else if (word.Text == "]") depth--;
                                    word.MoveNext();
                                }
                            }

                            // Handle default value assignment
                            if (word.Text == "=")
                            {
                                word.MoveNext();
                                // Skip expression
                                while (!word.Eof && word.Text != "," && word.Text != ")" && word.Text != ";")
                                {
                                    word.MoveNext();
                                }
                            }
                        }

                        if (word.Text == ",")
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
                            if (!word.Eof) word.AddError(", or ) expected");
                            break;
                        }
                    }
                }

                if (word.Text == ")")
                {
                    word.MoveNext();
                }
                else
                {
                    word.AddError(") expected");
                }
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
            }

            // Mark as prototype (extern)
            constructor.LastIndexReference = word.CreateIndexReference();
        }

    }
}
