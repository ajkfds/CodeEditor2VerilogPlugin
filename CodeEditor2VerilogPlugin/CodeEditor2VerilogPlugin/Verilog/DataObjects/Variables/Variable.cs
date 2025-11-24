using pluginVerilog.Verilog.DataObjects.Arrays;
using pluginVerilog.Verilog.DataObjects.DataTypes;
using pluginVerilog.Verilog.Statements;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TextMateSharp.Model;

namespace pluginVerilog.Verilog.DataObjects.Variables
{
    // #SystemVerilog 2017
    //	variable	+ integer_vector_type	+ bit 		user-defined-size	2state	sv
    //										+ logic		user-defined-size	4state  sv
    //										+ reg		user-defined-size	4state	v
    //
    //				+ integer_atom_type		+ byte		8bit signed			2state  sv
    //										+ shortint	16bit signed		2state  sv
    //										+ int		32bit signed		2state  sv
    //										+ longint	64bit signed		2state  sv
    //										+ integer	32bit signed		4state	v
    //										+ time		64bit unsigned		        v
    //
    //            	+ non_integer_type		+ shortreal	                            sv
    //										+ real		                            v
    //										+ realtime	                            v
    //              + struct/union
    //              + enum
    //              + string
    //              + chandle
    //              + virtual(interface)
    //              + class/package
    //              + event
    //              + pos_covergroup
    //              + type_reference


    public class Variable : DataObject, ICommentAnnotated
    {
        protected Variable() { }

        /// <summary>
        /// create variable instance from DataType
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static Variable Create(string name,IDataType dataType)
        {
            /*
            6.8 Variable declarations 
             
             */

            /* TODO
                | struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                | "enum" [enum_base_type] { enum_name_declaration { , enum_name_declaration } { packed_dimension }
                | "chandle"
                | "virtual" ["interface"] interface_identifier[parameter_value_assignment][ . modport_identifier] 
                | [class_scope | package_scope] type_identifier { packed_dimension }
                | class_type
                | "event"
                | ps_covergroup_identifier 
                | type_reference
            */
            switch (dataType.Type)
            {
                //struct_union["packed"[signing]] { struct_union_member { struct_union_member } } { packed_dimension }
                case DataTypeEnum.Struct:
                    return Struct.Create(name, dataType);
                //integer_vector_type ::= "bit" | "logic" | "reg"
                case DataTypeEnum.Logic:
                case DataTypeEnum.Bit:
                case DataTypeEnum.Reg:
                    return IntegerVectorValueVariable.Create(name, dataType);
                //integer_atom_type   ::= "byte" | "shortint" | "int" | "longint" | "integer" | "time"
                case DataTypeEnum.Byte:
                case DataTypeEnum.Shortint:
                case DataTypeEnum.Int:
                case DataTypeEnum.Longint:
                case DataTypeEnum.Integer:
                case DataTypeEnum.Time:
                    return IntegerAtomVariable.Create(name, dataType);

                //non_integer_type    ::= "shortreal" | "real" | "realtime"
                case DataTypeEnum.Shortreal:
                    return Shortreal.Create(name, dataType);
                case DataTypeEnum.Real:
                    return Real.Create(name, dataType);
                case DataTypeEnum.Realtime:
                    return Realtime.Create(name, dataType);

                // "string"
                case DataTypeEnum.String:
                    return String.Create(name, dataType);
                case DataTypeEnum.Enum:
                    return Enum.Create(name, dataType);
                case DataTypeEnum.Class:
                    return Object.Create(name, dataType);

                case DataTypeEnum.Chandle:
                    return Chandle.Create(name, dataType);
                default:
                    throw new Exception();
                    break;
            }
        }



        public override void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            label.AppendText(Name);

            label.AppendText("@sync ", Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
            bool first = true;
            foreach (var sync in SyncInfos)
            {
                if (!first) label.AppendText(",");
                if (sync != null) label.AppendText(sync, Global.CodeDrawStyle.Color(CodeDrawStyle.ColorType.HighLightedComment));
                first = false;
            }
            label.AppendText("\r\n");
        }

        public override void AppendTypeLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {

        }

        public override Variable Clone()
        {
            Variable val = new Variable() { Name = Name };
            return val;
        }

        public override Variable Clone(string name)
        {
            Variable val = new Variable() { Name = name };
            return val;
        }

        public static bool ParseDeclaration(WordScanner word, NameSpace nameSpace)
        {
        // data_declaration::=    [ "const" ] ["var"] [lifetime] data_type_or_implicit list_of_variable_decl_assignments;
        //                      | type_declaration
        //                      | package_import_declaration11
        //                      | net_type_declaration
        // lifetime ::= static | automatic 

        // list_of_variable_decl_assignments ::= variable_decl_assignment { , variable_decl_assignment } 

        // variable_decl_assignment     ::=   variable_identifier                                   { variable_dimension }  [ = expression]
        //                                  | dynamic_array_variable_identifier unsized_dimension   { variable_dimension }  [ = dynamic_array_new]
        //                                  | class_variable_identifier                                                     [ = class_new]

        // variable_dimension::=
        //        unsized_dimension
        //      | unpacked_dimension
        //      | associative_dimension
        //      | queue_dimension

        // unsized_dimension::= "[" "]"

        // associative_dimension ::=
        //        "[" data_type "]"
        //      | "[" "*" "]"

        // unpacked_dimension ::=
        //        "[" constant_range "]"
        //      | "[" constant_expression "]"

        // queue_dimension::= "[" "$" [ ":" constant_expression] "]"

            bool pointerMoved= false;

            if (word.Text == "const")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            if (word.Text == "var")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            if (word.Text == "static" | word.Text=="automatic")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                word.MoveNext();
                pointerMoved = true;
            }

            IDataType? dataType = DataObjects.DataTypes.DataTypeFactory.ParseCreate(word, nameSpace, null);
            if (dataType == null) return pointerMoved;

            List<DataObject> vars = new List<DataObject>();

            while (!word.Eof && word.Text !=";")
            {
                string name = word.Text;
                if (!General.IsIdentifier(name))
                {
                    word.AddError("illegal identifier");
                    break;
                }

                DataObject variable = Variable.Create(word.Text,dataType);
                variable.DefinedNameSpace = nameSpace;
                if (variable == null) return true;
                variable.DefinedReference = word.GetReference();


                word.Color(variable.ColorType);
                word.MoveNext();

                // { variable_dimension }
                while(word.Text == "[" && !word.Eof)
                {
                    IArray? array = DataObjects.Arrays.VariableArray.ParseCreate(variable,word, nameSpace);
                    if(array is UnPackedArray)
                    {
                        UnPackedArray unPackedArray = (UnPackedArray)array;
                        variable.UnpackedArrays.Add(unPackedArray);
                    }else if(array is Queue)
                    {
                        Queue queue = (Queue)array;
                        variable = queue;
                    }else if(array is AssociativeArray)
                    {
                        AssociativeArray associativeArray = (AssociativeArray)array;
                        variable = associativeArray;
                    }
                }


                if (word.Text == "=") 
                {
                    word.MoveNext();    // =
                    if (word.Text == "new")
                    {
                        word.Color(CodeDrawStyle.ColorType.Keyword);
                        word.MoveNext();
                        if(word.Text == "[") // dynamic array size
                        {
                            word.MoveNext();

                            Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);

                            if (word.Text != "]")
                            {
                                word.AddError("] required");
                            }
                            else
                            {
                                word.MoveNext();
                            }
                        }

                        if(word.Text == "(")
                        {
                            word.MoveNext();
                            while (!word.Eof && word.Text !=")")
                            {
                                Expressions.Expression? exp = Expressions.Expression.ParseCreate(word, nameSpace);
                                if (word.Text == ",")
                                {
                                    word.MoveNext();
                                    continue;
                                }else if(word.Text == ")")
                                {
                                    break;
                                }
                                else
                                {
                                    word.AddError("illegal arguments");
                                    break;
                                }
                            }
                            if(word.Text ==")") word.MoveNext();
                        }
                    }
                    else
                    {
                        Expressions.Expression? exp;
                        if (word.Text == "'" && word.NextText == "{")
                        {
                            exp = AssignmentPattern.ParseCreate(word, nameSpace, false);
                            if (exp == null)
                            {
                                word.AddError("illegal assignment pattern.");
                                return true;
                            }
                        }
                        else
                        {
                            exp = Expressions.Expression.ParseCreate(word, nameSpace);
                            if (exp == null)
                            {
                                word.AddError("expression required.");
                                return true;
                            }
                            else
                            {
                                variable.AssignedReferences.Add(variable.DefinedReference);
                                if(variable.BitWidth != exp.BitWidth && !word.Prototype && exp.Reference != null && !word.Prototype)
                                {
                                    exp.Reference.AddWarning("Bitwidth mismatch "+variable.BitWidth.ToString()+" = "+exp.BitWidth.ToString());
                                }
                            }
                        }
                    }
                }

                if (word.Prototype)
                {
                    if (!nameSpace.NamedElements.ContainsKey(variable.Name))
                    {   // new variable
                        nameSpace.NamedElements.Add(variable.Name, variable);
                    }
                    else
                    {   // duplicated name
                        INamedElement oldNamedElect = nameSpace.NamedElements[variable.Name];
                        DataObject? oldDataObject = oldNamedElect as DataObject;
                        
                        if(oldDataObject != null && oldDataObject.DefinedNameSpace?.BuildingBlock != nameSpace.BuildingBlock)
                        { // override base class

                        }else if (nameSpace.BuildingBlock.AnsiStylePortDefinition)
                        {   
                            variable.DefinedReference.AddError("duplicate");
                        }
                        else
                        {
                            BuildingBlocks.IModuleOrInterfaceOrProgram? buildingBlockWithPorts = nameSpace.BuildingBlock as BuildingBlocks.IModuleOrInterfaceOrProgram;
                            if(buildingBlockWithPorts == null)
                            {
                                variable.DefinedReference.AddError("duplicate");
                            }
                            else
                            {
                                if (buildingBlockWithPorts.Ports.ContainsKey(variable.Name))
                                {
                                    Port port = buildingBlockWithPorts.Ports[variable.Name];
                                    port.DataObject = variable;
                                }
                                else
                                {
                                    variable.DefinedReference.AddError("duplicate");
                                }
                            }

                        }
//                        word.AddError("duplicate");
                    }
                }
                else
                {
                    if (!nameSpace.NamedElements.ContainsKey(variable.Name))
                    {
                        nameSpace.NamedElements.Add(variable.Name, variable);
                    }
                }
                vars.Add(variable);

                if (word.Text == ",")
                {
                    word.MoveNext();
                }
                else
                {
                    break;
                }
            }
            if(word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }

            string comment = word.GetNextComment();

            if(comment != "")
            {
                foreach (DataObject variable in vars)
                {
                    variable.Comment = comment;
                }
            }

            return true;
        }


        // comment annotation


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

    }

}
