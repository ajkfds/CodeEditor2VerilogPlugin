using pluginVerilog.Verilog.BuildingBlocks;
using pluginVerilog.Verilog.DataObjects.Nets;
using pluginVerilog.Verilog.DataObjects;
using pluginVerilog.Verilog.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.DataObjects.Variables;
//using Microsoft.CodeAnalysis.Emit;

namespace pluginVerilog.Verilog
{
    public class Task : NameSpace,IPortNameSpace
    {
        /*
        task_declaration ::=    (From Annex A - A.2.7)  
                                task    [ automatic ] task_identifier ;                     { task_item_declaration }   statement   endtask 
                                | task  [ automatic ] task_identifier ( task_port_list ) ;  { block_item_declaration }  statement   endtask
        task_item_declaration ::=   block_item_declaration 
                                    | { attribute_instance } tf_ input_declaration ; 
                                    | { attribute_instance } tf_output_declaration ; 
                                    | { attribute_instance } tf_inout_declaration ; 
        task_port_list ::= task_port_item { , task_port_item }  
        task_port_item ::=  { attribute_instance } tf_input_declaration 
                            | { attribute_instance } tf_output_declaration 
                            | { attribute_instance } tf_inout_declaration
        tf_input_declaration    ::= input [ reg ] [ signed ] [ range ] list_of_port_identifiers 
                                    | input [ task_port_type ] list_of_port_identifiers
        tf_output_declaration   ::= output [ reg ] [ signed ] [ range ] list_of_port_identifiers 
                                    | output [ task_port_type ] list_of_port_identifiers 
        tf_inout_declaration    ::= inout [ reg ] [ signed ] [ range ] list_of_port_identifiers 
                                    | inout [ task_port_type ] list_of_port_identifiers
        task_port_type          ::= time 
                                    | real 
                                    | realtime 
                                    | integer
        block_item_declaration  ::= (From Annex A - A.2.8)
                                    { attribute_instance } block_reg_declaration 
                                    | { attribute_instance } event_declaration 
                                    | { attribute_instance } integer_declaration 
                                    | { attribute_instance } local_parameter_declaration 
                                    | { attribute_instance } parameter_declaration 
                                    | { attribute_instance } real_declaration 
                                    | { attribute_instance } realtime_declaration 
                                    | { attribute_instance } time_declaration
        block_reg_declaration               ::= reg [ signed ] [ range ]   list_of_block_variable_identifiers ;  
        list_of_block_variable_identifiers  ::= block_variable_type { , block_variable_type } 
        block_variable_type                 ::= variable_identifier | variable_identifier dimension { dimension }
         */


        /*
        // # SystemVerilog2017
        // task_declaration         ::=   task [ lifetime ] task_body_declaration
        // task_body_declaration    ::=   [ interface_identifier . | class_scope ] task_identifier ; { tf_item_declaration } { statement_or_null } endtask [ : task_identifier ] 
        //                              | [ interface_identifier . | class_scope ] task_identifier ( [ tf_port_list ] ) ; { block_item_declaration } { statement_or_null } endtask [ : task_identifier ]
         
         */
        protected Task(NameSpace parent) : base(parent.BuildingBlock , parent)
        {
        }

        private Dictionary<string, DataObjects.Port> ports = new Dictionary<string, DataObjects.Port>();
        public Dictionary<string, DataObjects.Port> Ports { get { return ports; } }
        private List<DataObjects.Port> portsList = new List<DataObjects.Port>();
        public List<DataObjects.Port> PortsList { get { return portsList; } }

        public Statements.IStatement Statement;
        public virtual void AppendLabel(AjkAvaloniaLibs.Controls.ColorLabel label)
        {
            //            if(ReturnVariable != null && ReturnVariable.DataType != null) ReturnVariable.DataType.

            label.AppendText(Name);
        }

        public static async System.Threading.Tasks.Task Parse(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "task")
            {
                System.Diagnostics.Debugger.Break();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginReference = word.CreateIndexReference();
            word.MoveNext();

            // [lifetime]
            switch (word.Text)
            {
                case "static":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
                case "automatic":
                    word.Color(CodeDrawStyle.ColorType.Keyword);
                    word.MoveNext();
                    break;
            }

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal identifier name");
                return;
            }

            Task task = new Task(nameSpace) { 
                BeginIndexReference = beginReference, 
                DefinitionReference = word.CrateWordReference(), 
                Name = word.Text, 
                Parent = nameSpace, 
                Project = word.Project 
            };
            task.BuildingBlock = nameSpace.BuildingBlock;

            if (!word.Active)
            {
                // skip
            }
            else
            {
                if (nameSpace.BuildingBlock.NamedElements.ContainsTask(task.Name))
                {
                    nameSpace.BuildingBlock.NamedElements.Replace(task.Name,task);
                }
                else
                {
                    nameSpace.BuildingBlock.NamedElements.Add(task.Name, task);
                }
            }

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();


            /*            A.2.8 Block item declarations
                        block_item_declaration ::=            { attribute_instance }
                        block_reg_declaration | { attribute_instance }
                        event_declaration | { attribute_instance }
                        integer_declaration | { attribute_instance }
                        local_parameter_declaration | { attribute_instance }
                        parameter_declaration | { attribute_instance }
                        real_declaration | { attribute_instance }
                        realtime_declaration | { attribute_instance }
                        time_declaration block_reg_declaration ::= reg[signed][range]                    list_of_block_variable_identifiers;
            */

            int x=0;
            x++;

            if (word.Text == ";")
            {
                parse_task_items_non_ansi(word, nameSpace, task);
            }
            else if(word.Text =="(")
            {
                parse_task_items_ansi(word, nameSpace, task);
            }
            else
            {
                word.AddError("illegal task definition");
            }

            if (!word.SystemVerilog && word.Text == "endtask")
            {
                word.AddError("statement required");
            }
            else
            {
                if (word.Prototype)
                {
                    while (!word.Eof)
                    {
                        if (word.Text == "endtask") break;
                        switch (word.Text)
                        {
                            case "endmodule":
                            case "endclass":
                            case "endfunction":
                            case "always":
                            case "initial":
                                word.AddError("missed endfunction");
                                return;
                            default:
                                break;
                        }
                        word.MoveNext();
                    }
                }
                else
                {
                    if (word.SystemVerilog)
                    {
                        while(!word.Eof && word.Text != "endtask")
                        {
                            switch (word.Text)
                            {
                                case "endmodule":
                                case "endclass":
                                case "endfunction":
                                case "always":
                                case "initial":
                                    return;
                                default:
                                    break;
                            }
                            var index =  word.CreateIndexReference();
                            Statements.IStatement statement = await Statements.Statements.ParseCreateFunctionStatement(word, task);
                            if (word.CreateIndexReference().IsSameAs(index))
                            {
                                word.MoveNext();
                            }
                        }
                    }
                    else
                    {
                        Statements.IStatement statement = await Statements.Statements.ParseCreateFunctionStatement(word, task);
                    }
                }
            }

            if (word.Text == "endtask")
            {
                word.Color(CodeDrawStyle.ColorType.Keyword);
                task.LastIndexReference = word.CreateIndexReference();
                word.AppendBlock(task.BeginIndexReference, task.LastIndexReference);
                word.MoveNext();
            }
            else
            {
                word.AddError("endtask expected");
                return;
            }

            if (word.Text == ":")
            {
                word.MoveNext();
                if (word.Text == task.Name)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
                else
                {
                    word.AddError("task name mismatch");
                    word.MoveNext();
                }
            }

            return;

        }

        // task_prototype::= task task_identifier[([tf_port_list])]
        public static void ParsePrototype(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "task") throw new Exception();

            IndexReference beginReference = word.CreateIndexReference();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext();

            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal identifier name");
                return;
            }

            Task task = new Task(nameSpace) { 
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference() ,
                Name = word.Text, 
                Parent = nameSpace,
                Project = word.Project 
            };
            task.BuildingBlock = nameSpace.BuildingBlock;

            if (!word.Active)
            {
                // skip
            }
            else
            {
                if (nameSpace.BuildingBlock.NamedElements.ContainsTask(task.Name))
                {
                    nameSpace.BuildingBlock.NamedElements.Replace(task.Name,task);
                }
                else
                {
                    nameSpace.BuildingBlock.NamedElements.Add(task.Name, task);
                }
            }

            word.Color(CodeDrawStyle.ColorType.Identifier);
            word.MoveNext();

            parse_task_items_ansi(word, nameSpace, task);
        }

        // function_body_declaration    ::=   function_data_type_or_implicit [interface_identifier. | class_scope] function_identifier;


        // ; { tf_item_declaration }
        private static void parse_task_items_non_ansi(WordScanner word, NameSpace nameSpace, Task function)
        {
            if (word.Text != ";") System.Diagnostics.Debugger.Break();
            word.MoveNext();

            while (!word.Eof)
            {
                switch (word.Text)
                {
                    case "input": // tf_input_declaration
                    case "output": // tf_output_declaration
                    case "inout":
                    case "ref":

                        Verilog.DataObjects.Port.ParseTfPortDeclaration(word, function);
                        if (word.Text != ";")
                        {
                            word.AddError("; expected");
                        }
                        else
                        {
                            word.MoveNext();
                        }
                        continue;
                    case "reg": // block_reg_declaration
                        Verilog.DataObjects.Variables.Reg.ParseDeclaration(word, function);
                        continue;
                    // event_declaration
                    case "integer": // integer_declaration
                        Verilog.DataObjects.Variables.Integer.ParseDeclaration(word, function);
                        continue;
                    case "localparameter": // local_parameter_declaration
                    case " parameter":  // parameter_declaration
                        Verilog.DataObjects.Constants.Parameter.ParseCreateDeclaration(word, function, null);
                        continue;
                    case "real": // real_declaration
                        Verilog.DataObjects.Variables.Real.ParseDeclaration(word, function);
                        continue;
                    case "realtime": // realtime_declaration
                        Verilog.DataObjects.Variables.Realtime.ParseDeclaration(word, function);
                        continue;
                    case "time": // time_declaration
                        Verilog.DataObjects.Variables.Time.ParseDeclaration(word, function);
                        continue;
                    case "wire": // illegal format for Verilog 2001
                        word.AddError("not supported(Verilog2001)");
                        Net.ParseDeclaration(word, function);
                        continue;
                    default:
                        break;
                }
                break;
            }
        }

        // ( [ tf_port_list ] ) ; { block_item_declaration }
        private static async System.Threading.Tasks.Task parse_task_items_ansi(WordScanner word, NameSpace nameSpace, Task task)
        {
            if (word.Text != "(") throw new Exception(); //System.Diagnostics.Debugger.Break();
            word.MoveNext();

            Port.ParseTfPortItems(word, nameSpace, task);

            if (word.Text == ")")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError(") required");
            }
            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else
            {
                word.AddError("; required");
            }

            while (!word.Eof)
            {
                if (!await BlockItemDeclaration.Parse(word, task)) break;
            }

        }
    }
}
