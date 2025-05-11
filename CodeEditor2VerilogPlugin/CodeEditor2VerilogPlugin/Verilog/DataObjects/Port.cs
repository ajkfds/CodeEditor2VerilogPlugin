using Avalonia.Controls;
using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.ModuleItems;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects
{
    public class Port : Item, ICommentAnnotated
    {
        public DirectionEnum Direction = DirectionEnum.Undefined;
        public DataObjects.Arrays.PackedArray? Range
        {
            get
            {
                if (DataObject == null) return null;
                Net? net = DataObject as Net;
                if(net != null)
                {
                    return net.Range;
                }

                Variables.Variable? variable = DataObject as Variables.Variable;
                if(variable != null)
                {
                    Variables.IntegerVectorValueVariable? integerVectorValueVariable = DataObject as Variables.IntegerVectorValueVariable;
                    if(integerVectorValueVariable != null)
                    {
                        if (integerVectorValueVariable.PackedDimensions.Count < 1) return null;
                        return integerVectorValueVariable.PackedDimensions[0];
                    }

                }

                return null;
            }
        }

        public DataObject? DataObject { set; get; } = null;
//        public IInstantiation Instantiation{  set; get; } = null;
        public string Comment = "";
        public string? PortGroupName = null;

        private Dictionary<string, string> commentAnnotations = new Dictionary<string, string>();
        public Dictionary<string, string> CommentAnnotations { get { return commentAnnotations; } }
        public void AppendAnnotation(string key, string value)
        {
            if (commentAnnotations.ContainsKey(key))
            {
                string oldValue = commentAnnotations[key];
                string newValue = oldValue + "," + value;
                commentAnnotations[key] = newValue;
            }
            else
            {
                commentAnnotations.Add(key, value);
            }
        }

        public enum DirectionEnum
        {
            Undefined,
            Input,
            Output,
            Inout,
            Ref
        }

        public static Port? Create(WordScanner word, Attribute attribute)
        {
            if (!General.IsIdentifier(word.Text)) return null;
            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = word.Text, Project = word.Project };
            word.Color(CodeDrawStyle.ColorType.Net);
            word.MoveNext();
            return port;
        }

        public AjkAvaloniaLibs.Controls.ColorLabel GetLabel()
        {
            AjkAvaloniaLibs.Controls.ColorLabel label = new AjkAvaloniaLibs.Controls.ColorLabel();

            switch (Direction)
            {
                case DirectionEnum.Input:
                    label.AppendText("input ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DirectionEnum.Output:
                    label.AppendText("output ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DirectionEnum.Inout:
                    label.AppendText("inout ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                case DirectionEnum.Ref:
                    label.AppendText("ref ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
                    break;
                default:
                    break;
            }

            if (DataObject is Variables.Reg)
            {
                label.AppendText("reg ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Keyword));
            }


            if (Range != null)
            {
                label.AppendLabel(Range.GetLabel());
                label.AppendText(" ");
            }

            if (DataObject != null)
            {
                if (DataObject is Net)
                {
                    label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Net));
                }
                else if (DataObject is Variables.Reg)
                {
                    label.AppendText(Name, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.Register));
                }
                else
                {
                    label.AppendText(Name);
                }
            }

            return label;
        }


        /*
         * 



        interface_port_declaration ::=    interface_identifier list_of_interface_identifiers 
                                        | interface_identifier . modport_identifier list_of_interface_identifiers 
        ref_declaration ::=       ref variable_port_type list_of_variable_identifiers 

parameter_port_list ::= 
# ( list_of_param_assignments { , parameter_port_declaration } )
| # ( parameter_port_declaration { , parameter_port_declaration } )
| #( )
parameter_port_declaration ::= 
parameter_declaration 
| local_parameter_declaration 
| data_type list_of_param_assignments 
| type list_of_type_assignments 
list_of_ports ::= ( port { , port } )
list_of_port_declarations2 ::= 
( [ { attribute_instance} ansi_port_declaration { , { attribute_instance} ansi_port_declaration } ] 

port_declaration ::= 
{ attribute_instance } inout_declaration 
| { attribute_instance } input_declaration 
| { attribute_instance } output_declaration 
| { attribute_instance } ref_declaration 
| { attribute_instance } interface_port_declaration 
port ::= 
[ port_expression ] 
| . port_identifier ( [ port_expression ] )
port_expression ::= 
port_reference 
| { port_reference { , port_reference } }
port_reference ::= 
port_identifier constant_select 
port_direction ::= input | output | inout | ref
net_port_header ::= [ port_direction ] net_port_type 
variable_port_header ::= [ port_direction ] variable_port_type 
interface_port_header ::= 
interface_identifier [ . modport_identifier ] 
| interface [ . modport_identifier ] 
ansi_port_declaration ::= 
[ net_port_header | interface_port_header ] port_identifier { unpacked_dimension } 
[ = constant_expression ] 
| [ variable_port_header ] port_identifier { variable_dimension } [ = constant_expression ] 
| [ port_direction ] . port_identifier ( [ expression ] )         
         */


        // ## verilog 2001
        // port_declaration::= { attribute_instance} inout_declaration | { attribute_instance} input_declaration | { attribute_instance} output_declaration  

        // inout_declaration::= inout[net_type][signed][range] list_of_port_identifiers
        // input_declaration ::= input[net_type][signed][range] list_of_port_identifiers
        // output_declaration ::=   output[net_type][signed][range] list_of_port_identifiers
        //                          | output[reg][signed][range] list_of_port_identifiers
        //                          | output reg[signed][range] list_of_variable_port_identifiers
        //                          | output[output_variable_type] list_of_port_identifiers
        //                          | output output_variable_type list_of_variable_port_identifiers 
        // list_of_port_identifiers::= port_identifier { , port_identifier }
        // range ::= [ msb_constant_expression : lsb_constant_expression ]  

        public static void ParsePortDeclarations(WordScanner word,NameSpace nameSpace)
        {
            IDataType? prevDataType = null;
            Net.NetTypeEnum? prevNetType = null;
            DirectionEnum? prevDirection = null;
            string? portGroup = null;
            Port? definedPort;

            checkCommentAnnotation(word, nameSpace, null, ref portGroup);
            if (ParsePortDeclaration(word, nameSpace, true, ref prevDataType, ref prevNetType, ref prevDirection,out definedPort))
            {
                if (definedPort != null)
                {
                    definedPort.PortGroupName = portGroup;
                    checkCommentAnnotation(word, nameSpace, definedPort, ref portGroup);
                }
                if (word.Text != ",") return;
                word.MoveNext();
                if (definedPort != null) checkCommentAnnotation(word, nameSpace, definedPort, ref portGroup);
                while (!word.Eof)
                {
                    if (!ParsePortDeclaration(word, nameSpace, false, ref prevDataType, ref prevNetType, ref prevDirection, out definedPort)) return;
                    if (definedPort != null)
                    {
                        definedPort.PortGroupName = portGroup;
                        checkCommentAnnotation(word, nameSpace, definedPort, ref portGroup);
                    }
                    if (word.Text != ",") return;
                    word.MoveNext();
                    if (definedPort != null) checkCommentAnnotation(word, nameSpace, definedPort, ref portGroup);
                }
            }
            else if(!nameSpace.BuildingBlock.AnsiStylePortDefinition)
            {
                while (!word.Eof)
                {
                    if (!ParseNonAnsiPortDeclaration(word, nameSpace)) return;
                    if (word.Text != ",") return;
                    word.MoveNext();
                }
            }
        }



        /// <summary>
        /// Check for comment annotations
        /// </summary>
        private static void checkCommentAnnotation(WordScanner word, NameSpace nameSpace,Port? port,ref string? portGroup)
        {
            string commentText = word.GetFollowedComment();
            if (!commentText.Contains("@")) return;

            var comment = word.GetCommentScanner();
            while (!comment.EOC)
            {
                if (!comment.Text.StartsWith("@"))
                {
                    comment.MoveNext();
                    continue;
                }

                if (commentText.Contains(word.ProjectProperty.AnnotationCommands.PortGroup))
                {
                    pasePortGroup(comment, nameSpace, ref portGroup);
                }
                else
                {
                    if (port != null)
                    {
                        if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Synchronized))
                        {
                            parseSyncAnnotation(comment, nameSpace, port);
                        }else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Clock))
                        {
                            parseClockAnnotation(comment, nameSpace, port);
                        }else if (commentText.Contains(word.ProjectProperty.AnnotationCommands.Reset))
                        {
                            parseResetAnnotation(comment, nameSpace, port);
                        }
                        else
                        {
                            comment.MoveNext();
                        }
                    }
                    else
                    {
                        comment.MoveNext();
                    }
                }
            }

        }

        private static void pasePortGroup(CommentScanner comment, NameSpace nameSpace, ref string? portGroup)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNextUntilEol(); // :

                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                portGroup = comment.Text;
            }
        }

        private static void parseSyncAnnotation(CommentScanner comment, NameSpace nameSpace, Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext(); // :

                while (!comment.EOC)
                {
                    if(!nameSpace.BuildingBlock.NamedElements.ContainsDataObject(comment.Text))
                    {
                        break;
                    }
                    port.AppendAnnotation("sync", comment.Text);
                    comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                    comment.MoveNext();

                    if (comment.Text != ",") break;
                    comment.MoveNext(); // ,

                    if (comment.Text.StartsWith("@")) break;
                }
            }
        }

        private static void parseClockAnnotation(CommentScanner comment, NameSpace nameSpace, Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            if (comment.Text == ":")
            {
                comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                comment.MoveNext(); // :

                while (!comment.EOC)
                {
                    if(comment.Text == "posedge"|| comment.Text=="negedge")
                    {
                        port.AppendAnnotation("clock", comment.Text);
                        comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
                        comment.MoveNext();
                    }
                    else
                    {
                        break;
                    }

                    if (comment.Text != ",") break;
                    comment.MoveNext(); // ,

                    if (comment.Text.StartsWith("@")) break;
                }
            }
        }

        private static void parseResetAnnotation(CommentScanner comment, NameSpace nameSpace, Port port)
        {
            comment.Color(CodeDrawStyle.ColorType.CommentAnnotation);
            comment.MoveNext();
            port.AppendAnnotation("reset", "");
        }

        private static bool ParseNonAnsiPortDeclaration(WordScanner word, NameSpace nameSpace)
        {
            if (!General.IsIdentifier(word.Text)) return false;

            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = word.Text, Project = word.Project };
            port.Direction = DirectionEnum.Undefined;
            IModuleOrInterfaceOrProgram? block = nameSpace.BuildingBlock as IModuleOrInterfaceOrProgram;
            if (block == null) return true;

            if (!block.Ports.ContainsKey(port.Name))
            {
                block.Ports.Add(port.Name, port);
            }
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            return true;
        }

        private static bool ParsePortDeclaration(WordScanner word, NameSpace nameSpace, bool firstPort, ref IDataType? prevDataType, ref Net.NetTypeEnum? prevNetType, ref DirectionEnum? prevDirection,out Port? definedPort)
        {


            // # SystemVerilog 2012
            // A.2.1.2 Port declarations

            // Non-ANSI style port declaration

            // port_declaration     ::=   { attribute_instance } inout_declaration
            //                          | { attribute_instance } input_declaration
            //                          | { attribute_instance } output_declaration
            //                          | { attribute_instance } ref_declaration
            //                          | { attribute_instance } interface_port_declaration

            // inout_declaration ::=     "inout" net_port_type list_of_port_identifiers 
            // input_declaration ::=     "input" net_port_type list_of_port_identifiers 
            //                         | "input" variable_port_type list_of_variable_identifiers 
            // output_declaration ::=    "output" net_port_type list_of_port_identifiers 
            //                         | "output" variable_port_type list_of_variable_port_identifiers
            // ref_declaration ::= ref variable_port_type list_of_variable_identifiers

            // interface_port_declaration   ::=   interface_identifier list_of_interface_identifiers 
            //                                  | interface_identifier . modport_identifier list_of_interface_identifiers 

            // net_port_type				::=   [ net_type ] data_type_or_implicit 
            //								    | net_type_identifier 
            //								    | interconnect implicit_data_type
            // variable_port_type			::= var_data_type 
            // var_data_type				::=   data_type
            //								    | var data_type_or_implicit 

            // ANSI style list of port declarations

            // list_of_port_declarations	::=	( [ { attribute_instance} ansi_port_declaration { , { attribute_instance} ansi_port_declaration } ] )

            // ansi_port_declaration		::=   [ net_port_header | interface_port_header ] port_identifier { unpacked_dimension } [ = constant_expression ] 
            //								    | [ variable_port_header ]                    port_identifier { variable_dimension } [ = constant_expression ] 
            //								    | [ port_direction ] "." port_identifier "(" [ expression ] ")"

            // net_port_header				::= [ port_direction ] net_port_type 
            // variable_port_header			::= [ port_direction ] variable_port_type 
            // interface_port_header		::=   interface_identifier [ "." modport_identifier ] 
            //								    | "interface" [ "." modport_identifier ] 

            // port_direction				::= "input" | "output" | "inout" | "ref"

            // net_port_type				::=   [ net_type ] data_type_or_implicit 
            //								    | net_type_identifier 
            //								    | interconnect implicit_data_type
            // variable_port_type			::= var_data_type 
            // var_data_type				::=   data_type
            //								    | var data_type_or_implicit 


            /*
            23.2.2.3 Rules for determining port kind, data type, and direction
            
            in this subclause :

            port kinds = net_type keywords or "var"
            data type  = explicit and implicit data type declarations
                         and does not include unpacked dimensions.
             */
            definedPort = null;

            BuildingBlock buildingBlock = nameSpace.BuildingBlock;
            DirectionEnum? direction = null;
            switch (word.Text)
            {
                case "input":
                    direction = DirectionEnum.Input;
                    break;
                case "output":
                    direction = DirectionEnum.Output;
                    break;
                case "inout":
                    direction = DirectionEnum.Inout;
                    break;
                default:
                    break;
            }
            if(direction != null)
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            Net.NetTypeEnum? netType = Net.parseNetType(word, nameSpace);

            IDataType? dataType = null;
            if(netType == null)
            {
                dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
            }

            Interface? interface_ = null;
            if(direction == null && netType == null && dataType == null)
            {
                if (parseInterfacePort(word, nameSpace))
                {
                    prevDirection = null;
                    prevDataType = null;
                    prevNetType = null;
                    return true;
                }
            }

            if ( direction == null && netType == null && dataType == null && interface_ == null )
            {
                if (firstPort)
                {
                    if((word.NextText == "," || word.NextText == ")")) // Without this, a port of an undefined type will be recognized as a non-ANSI format.
                    {
                        //For the first port in the port list:
                        //  — If the direction, port kind, and data type are all omitted, then the port shall be assumed to be a
                        //    member of a non - ANSI style list_of_ports, and port direction and type declarations shall be declared
                        //    after the port list. 
                        nameSpace.BuildingBlock.AnsiStylePortDefinition = false;
                        return false;
                    }
                    else
                    { // non defined object
                        while (!word.Eof)
                        {
                            word.MoveNext();
                            if (word.Text == ".")
                            {
                                word.MoveNext();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    //For subsequent ports in the port list:
                    //    — If the direction, port kind and data type are all omitted, then they shall be inherited from the previous port.
                    //      If the previous port was an interconnect port, this port shall also be an interconnect port.
                    direction = prevDirection;
                    dataType = prevDataType;
                    netType = prevNetType;
                }
            }

            //    — If the direction is omitted, it shall default to inout. 
            if (direction == null) direction = DirectionEnum.Inout;

            //If the port kind is omitted: (port kinds = net_type keywords or "var")
            if (netType == null)
            {
                switch (direction)
                {
                    // — For input and inout ports, the port shall default to a net of default net type. 
                    case DirectionEnum.Inout:
                    case DirectionEnum.Input:
                        netType = buildingBlock.DefaultNetType;
                        break;
                    // — For output ports, the default port kind depends on how the data type is specified:
                    //      — If the data type is omitted or declared with the implicit_data_type syntax, the port kind shall
                    //        default to a net of default net type.
                    //      — If the data type is declared with the explicit data_type syntax, the port kind shall default to variable.
                    //      — A ref port is always a variable.
                    case DirectionEnum.Output:
                        if(dataType == null)
                        {
                            netType = buildingBlock.DefaultNetType;
                        }
                        break;
                }
            }

            //if (direction != DirectionEnum.Inout && netType == null)
            //{
            //    dataType = DataType.ParseCreate(word, nameSpace, null);
            //}

            // — If the data type is omitted, it shall default to logic except for interconnect ports which have no data type.
            // parse packed dimensions for net without explicit datatype
            List<DataObjects.Arrays.PackedArray> packedDimensions = new List<DataObjects.Arrays.PackedArray>();
            if(dataType == null)
            {
                while (word.Text=="[")
                {
                    DataObjects.Arrays.PackedArray? range = DataObjects.Arrays.PackedArray.ParseCreate(word, nameSpace);
                    if (range == null) break;
                    packedDimensions.Add(range);
                }
            }
            else
            {
                if (dataType.IsVector)
                {
                    DataTypes.IntegerVectorType? vector = dataType as DataTypes.IntegerVectorType;
                    if (vector == null) throw new Exception();
                    packedDimensions = vector.PackedDimensions;
                }
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal port identifier");
                return true;
            }

            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = word.Text, Project = word.Project };
            port.Direction = (DirectionEnum)direction;
            if(netType != null)
            {
                Net net = Net.Create(port.Name,(Net.NetTypeEnum)netType, dataType);
                net.PackedDimensions = packedDimensions;
                net.DefinedReference = word.CrateWordReference();
                port.DataObject = net;
            }
            else if(dataType != null)
            {
                Variables.Variable variable = Variables.Variable.Create(port.Name, dataType);
                variable.DefinedReference = word.CrateWordReference();
                port.DataObject = variable;
            }
            else if(interface_ != null)
            {
//                Interface interface_instance = interface_ .
            }


            addPort(word, nameSpace, port);
            definedPort = port;

            if(port.DataObject != null)
            {
                switch (port.Direction)
                {
                    case DirectionEnum.Inout:
                        port.DataObject.AssignedReferences.Add(word.GetReference());
                        port.DataObject.UsedReferences.Add(word.GetReference());
                        break;
                    case DirectionEnum.Input:
                        port.DataObject.AssignedReferences.Add(word.GetReference());
                        break;
                    case DirectionEnum.Undefined:
                        break;
                    case DirectionEnum.Ref:
//                        port.DataObject.AssignedReferences.Add(word.GetReference());
//                        port.DataObject.UsedReferences.Add(word.GetReference());
                        break;
                    case DirectionEnum.Output:
                        port.DataObject.UsedReferences.Add(word.GetReference());
                        break;
                }
            }

            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            // Unpacked dimensions shall not be inherited from the previous port declaration
            //  and must be repeated for each port with the same dimensions.
            // { dimension } 
            while (!word.Eof && word.Text == "[")
            {
                Verilog.DataObjects.Arrays.VariableArray? dimension = Verilog.DataObjects.Arrays.UnPackedArray.ParseCreate(word, nameSpace);
                if(dimension != null && port.DataObject != null)
                {
                    port.DataObject.Dimensions.Add(dimension);
                }
            }

            // [ = constant_expression ] 
            if (word.Text == "=")
            {
                word.MoveNext();
                Expressions.Expression? ex = Expressions.Expression.ParseCreate(word, nameSpace);
                if (ex == null) return true;
                if (!ex.Constant)
                {
                    ex.Reference.AddError("should be constant");
                }
                else
                {
                    // TODO constant value assignment
                }
            }

            prevDirection = direction;
            prevDataType = dataType;
            prevNetType = netType;

            return true;
        }

        private static void addInstantiation(WordScanner word, NameSpace nameSpace, INamedElement instantiation)
        {
            BuildingBlock block = nameSpace.BuildingBlock as BuildingBlock;
            if (block == null)
            {
                word.AddError("cannot add instantiation");
            }
            else
            {
                if (block.NamedElements.ContainsKey(instantiation.Name))
                {
                    if (word.Prototype)
                    {
                        word.AddError("port name duplicate");
                    }
                    else
                    {
                        block.NamedElements.Remove(instantiation.Name);
                        block.NamedElements.Add(instantiation.Name, instantiation);
                    }
                }
                else
                {
                    block.NamedElements.Add(instantiation.Name, instantiation);
                }
            }
        }

        private static void addPort(WordScanner word, NameSpace nameSpace,Port port)
        {
            IModuleOrInterfaceOrProgram? block = nameSpace.BuildingBlock as IModuleOrInterfaceOrProgram;
            if (block == null)
            {
                word.AddError("cannot add port");
            }
            else
            {
                if (block.Ports.ContainsKey(port.Name))
                {
                    if (word.Prototype)
                    {
                        word.AddError("port name duplicate");
                    }
                    else
                    {
                        block.Ports.Remove(port.Name);
                        block.Ports.Add(port.Name, port);
                        block.PortsList.Add(port);
                    }
                }
                else
                {
                    block.Ports.Add(port.Name, port);
                    block.PortsList.Add(port);
                }

                if (port.DataObject != null)
                {
                    DataObject? dataObject = block.NamedElements.GetDataObject(port.Name);

                    if(dataObject == null)
                    {
                        block.NamedElements.Add(port.Name, port.DataObject);
                    }
                    else
                    {
                        if (word.Prototype)
                        {
                            //                    word.AddError("port name duplicate");
                        }
                        else
                        {
                            block.NamedElements.Remove(port.Name);
                            block.NamedElements.Add(port.Name, port.DataObject);
                        }
                    }

                }
            }

        }

        private static bool parseInterfacePort(WordScanner word, NameSpace nameSpace)
        {
            string identifier = word.Text;

            Interface? interface_ = null;
            interface_ = word.ProjectProperty.GetBuildingBlock(identifier) as Interface;
            if (interface_ == null) return false;
//            string? modPortName = null;

            BuildingBlock buildingBlock = nameSpace.BuildingBlock;
            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();
            
            if (word.Text == ".")
            {
                word.MoveNext();

                word.Color(CodeDrawStyle.ColorType.Identifier);
                if(!interface_.NamedElements.ContainsModPort(word.Text))
                {
                    word.AddError("illegal modport name");
                    return true;
                }
                ModPort modPort = (ModPort)interface_.NamedElements[word.Text];
                word.MoveNext();
                return parseModPort(word, nameSpace, interface_, modPort);
            }


            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal identifier");
                return true;
            }
            InterfaceInstance iInst = InterfaceInstance.CreatePortInstance(word,interface_.Name);
//            iInst.ModPortName = modPortName;
            word.Color(CodeDrawStyle.ColorType.Variable);
            word.MoveNext();

            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = iInst.Name, Project = word.Project };
            port.DataObject = iInst;

            addPort(word, nameSpace, port);
            addInstantiation(word, nameSpace, iInst);

            return true;
        }
        private static bool parseModPort(WordScanner word, NameSpace nameSpace,Interface interface_, ModPort modPort)
        {
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal identifier");
                return true;
            }
            ModportInstance iModport = ModportInstance.Create(word.Text,interface_, modPort);
            word.Color(iModport.ColorType);
            word.MoveNext();

            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = iModport.Name, Project = word.Project };
            port.DataObject = iModport;

            addPort(word, nameSpace, port);
            addInstantiation(word, nameSpace, iModport);

            return true;
        }


        // tf_port_item         ::= { attribute_instance } [tf_port_direction] [var] data_type_or_implicit [port_identifier { variable_dimension } [ = expression] ] 

        // tf_port_direction    ::= port_direction | "const ref"
        // tf_port_declaration  ::= { attribute_instance } tf_port_direction [var] data_type_or_implicit list_of_tf_variable_identifiers;
        // task_prototype       ::= task task_identifier[( [tf_port_list])]


        // ### task port

        /*
        tf_item_declaration ::=   block_item_declaration 
                                | tf_port_declaration 

        tf_port_list ::=        tf_port_item { , tf_port_item }

        tf_port_item ::=    { attribute_instance } [ tf_port_direction ] [ var ] data_type_or_implicit [ port_identifier { variable_dimension } [ = expression ] ] 
        tf_port_direction ::=   port_direction | "const ref"

        tf_port_declaration ::= 
            { attribute_instance } tf_port_direction [ "var" ] data_type_or_implicit list_of_tf_variable_identifiers ";"


        // task / function port

        // tf_port_list / tf_port_item style ------------------------------

        tf_port_list::= tf_port_item { , tf_port_item }
        tf_port_item         ::= { attribute_instance } [ tf_port_direction ] [ var ] data_type_or_implicit [ port_identifier { variable_dimension } [ = expression ] ]


        // tf_item_declaration / tf_port_declaration style ----------------

        tf_item_declaration     ::=   block_item_declaration 
                                    | tf_port_declaration 
        tf_port_declaration     ::= { attribute_instance } tf_port_direction [ var ] data_type_or_implicit list_of_tf_variable_identifiers ;

        // common
        tf_port_direction::= port_direction | "const" "ref"

        data_type_or_implicit   ::=   data_type 
                                    | implicit_data_type 
        implicit_data_type ::=        [ signing ] { packed_dimension }          



        lifetime                ::= "static" | "automatic"
        signing                 ::= "signed" | "unsigned"
        */

        public static bool ParseTfPortDeclaration(WordScanner word, NameSpace nameSpace)
        {
            // tf_port_direction[var] data_type_or_implicit list_of_tf_variable_identifiers;
            BuildingBlock buildingBlock = nameSpace.BuildingBlock;
            DirectionEnum? direction = null;
            switch (word.Text)
            {
                case "input":
                    direction = DirectionEnum.Input;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "output":
                    direction = DirectionEnum.Output;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "inout":
                    direction = DirectionEnum.Inout;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "const":
                    direction = DirectionEnum.Ref;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if (word.Text == "ref")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("ref required");
                    }
                    break;
                default:
                    return false;
            }

            if (word.Text == "var")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            IDataType dataType = DataTypeFactory.ParseCreate(word, nameSpace, null);

            // Each formal argument has a data type that can be explicitly declared or inherited from the previous argument.
            // If the data type is not explicitly declared, then the default data type is logic
            //    if it is the first argument
            // or if the argument direction is explicitly specified.
            // Otherwise, the data type is inherited from the previous argument.

            if (dataType == null)
            {
                DataTypes.IntegerVectorType vectorType = new DataTypes.IntegerVectorType() { Type = DataTypeEnum.Logic };
                    if(word.Text == "[")
                    {
                    DataObjects.Arrays.PackedArray? range = DataObjects.Arrays.PackedArray.ParseCreate(word, nameSpace);
                    if (range != null) vectorType.PackedDimensions.Add(range);

                    }

                dataType = vectorType;
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal port name");
                return true;
            }

            while (!word.Eof)
            {
                if (!General.IsIdentifier(word.Text)) break;

                Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = word.Text, Project = word.Project };
                port.Direction = (DirectionEnum)direction;
                port.DataObject = Variables.Variable.Create(port.Name,dataType);

                if( nameSpace is Function)
                {
                    Function? function = nameSpace as Function;
                    if (function == null) throw new Exception();
                    if (!function.Ports.ContainsKey(port.Name))
                    {
                        function.Ports.Add(port.Name, port);
                        function.PortsList.Add(port);
                    }
                }else if(nameSpace is Task)
                {
                    Task? task = nameSpace as Task;
                    if (task == null) throw new Exception();
                    if (!task.Ports.ContainsKey(port.Name))
                    {
                        task.Ports.Add(port.Name, port);
                        task.PortsList.Add(port);
                    }
                }

                

                if (!nameSpace.NamedElements.ContainsKey(port.DataObject.Name))
                {
                    nameSpace.NamedElements.Add(port.DataObject.Name, port.DataObject);
                }

                word.Color(CodeDrawStyle.ColorType.Variable);
                word.MoveNext();

                if (word.Text != ",") return true;
                word.MoveNext();
            }
            return true;
        }

        public static void ParseTfPortItems(WordScanner word, NameSpace nameSpace, IPortNameSpace portNameSpace)
        {
            DirectionEnum? prevDirection = null;
            IDataType? prevDataType = null;

            bool firstPort = true;
            portNameSpace.Ports.Clear();
            portNameSpace.PortsList.Clear();


            while (!word.Eof && word.Text != ")" && word.Text != "end")
            {
                ParseTfPortItem(word, nameSpace, portNameSpace, firstPort, ref prevDirection, ref prevDataType);
                if(word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    return;
                }
            }

        }

        public static bool ParseTfPortItem(WordScanner word, NameSpace nameSpace, IPortNameSpace portNameSpace, bool first,ref DirectionEnum? prevDirection, ref IDataType? prevDataType)
        {
            // tf_port_item    ::= { attribute_instance } [ tf_port_direction ] [ var ] data_type_or_implicit [ port_identifier { variable_dimension } [ = expression ] ]

            BuildingBlock buildingBlock = nameSpace.BuildingBlock;
            DirectionEnum? direction = null;
            switch (word.Text)
            {
                case "input":
                    direction = DirectionEnum.Input;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "output":
                    direction = DirectionEnum.Output;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "inout":
                    direction = DirectionEnum.Inout;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "const":
                    direction = DirectionEnum.Ref;
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    if(word.Text == "ref")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                    }
                    else
                    {
                        word.AddError("ref required");
                    }
                    break;
                default:
                    break;
            }

            if(word.Text == "var")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
            }

            IDataType? dataType = DataTypeFactory.ParseCreate(word, nameSpace, null);

            // Each formal argument has a data type that can be explicitly declared or inherited from the previous argument.
            // If the data type is not explicitly declared, then the default data type is logic
            //    if it is the first argument
            // or if the argument direction is explicitly specified.
            // Otherwise, the data type is inherited from the previous argument.

            if(dataType == null)
            {
                if(first || direction != null)
                {
                    DataTypes.IntegerVectorType vectorType = new DataTypes.IntegerVectorType() { Type = DataTypeEnum.Logic };
                    if (word.Text == "[")
                    {
                        DataObjects.Arrays.PackedArray? range = DataObjects.Arrays.PackedArray.ParseCreate(word, nameSpace);
                        if (range != null)
                        {
                            vectorType.PackedDimensions.Add(range);
                        }
                    }
                    dataType = vectorType;
                }
                else
                {
                    dataType = prevDataType;
                }
            }
            if (dataType == null) throw new Exception();

            // There is a default direction of input if no direction has been specified. Once a direction is given,
            // subsequent formals default to the same direction. 
            if(direction == null)
            {
                direction = DirectionEnum.Input;
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal port name");
                word.SkipToKeyword(",");
            }

            Port port = new Port() { DefinitionReference = word.CrateWordReference(), Name = word.Text, Project = word.Project };
            port.Direction = (DirectionEnum)direction;
            port.DataObject = Variables.Variable.Create(port.Name,dataType);
            word.Color(CodeDrawStyle.ColorType.Variable);

            if (portNameSpace.Ports.ContainsKey(port.Name))
            {
                if (portNameSpace.Ports.ContainsKey(port.Name))
                {
                    word.AddError("port name duplicated");
                }
            }
            else
            {
                portNameSpace.Ports.Add(port.Name, port);
                portNameSpace.PortsList.Add(port);
            }

            if (portNameSpace.NamedElements.ContainsKey(port.DataObject.Name))
            {
                if (word.Prototype)
                {
                }
                else
                {
                    if (portNameSpace.NamedElements.ContainsKey(port.DataObject.Name)) portNameSpace.NamedElements.Remove(port.DataObject.Name);
                }
                portNameSpace.NamedElements.Add(port.DataObject.Name, port.DataObject);
            }
            else
            {
                portNameSpace.NamedElements.Add(port.DataObject.Name, port.DataObject);
            }

            word.MoveNext();


            if(word.Text == "=")
            {
                word.AddSystemVerilogError();
                word.MoveNext();
                Expressions.Expression? exp = Expressions.Expression.ParseCreate(word,nameSpace);
            }

            prevDirection = port.Direction;
            prevDataType = dataType;
            return true;
        }



    }
}