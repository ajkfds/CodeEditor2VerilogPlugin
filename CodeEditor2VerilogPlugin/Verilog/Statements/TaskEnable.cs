using CodeEditor2.CodeEditor.CodeComplete;

namespace pluginVerilog.Verilog.Statements
{
    public class TaskEnable : IStatement
    {
        /*
            SystemVerilog IEEE1800-2017
        subroutine_call_statement ::=     subroutine_call ;
                                        | void ' ( function_subroutine_call ) ;
        subroutine_call ::=               tf_call
                                        | system_tf_call
                                        | method_call
                                        | [ std :: ] randomize_call
        tf_call ::=                     ps_or_hierarchical_tf_identifier { attribute_instance } [ ( list_of_arguments ) ]
        list_of_arguments ::=             [ expression ] { , [ expression ] } { , . identifier ( [ expression ] ) }
                                        | .identifier ( [ expression ] ) { , . identifier ( [ expression ] ) }
        ps_or_hierarchical_tf_identifier ::=      [ package_scope ] tf_identifier
                                                | hierarchical_tf_identifier
         
         */


        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        // task_enable ::= (From Annex A - A.6.9) hierarchical_task_identifier [ ( expression { , expression } ) ] ;
        public AutocompleteItem CreateAutoCompleteItem()
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
        public static TaskEnable? ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, nameSpace, null);
        }
        public static TaskEnable? ParseCreate(WordScanner word, NameSpace nameSpace, NameSpace taskNameSpace)
        {
            return ParseCreate(word, nameSpace, taskNameSpace, null);
        }
        public static TaskEnable? ParseCreate(WordScanner word, NameSpace nameSpace, NameSpace taskNameSpace, CompletionContext? completionContext)
        {
            Expressions.TaskReference taskReference = Verilog.Expressions.TaskReference.ParseCreate(word, nameSpace, taskNameSpace);
            return ParseCreate(taskReference, word, nameSpace, completionContext);
        }

        public static TaskEnable? ParseCreate(Expressions.TaskReference taskReference, WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(taskReference, word, nameSpace, null);
        }

        public static TaskEnable? ParseCreate(Expressions.TaskReference taskReference, WordScanner word, NameSpace nameSpace, CompletionContext? completionContext)
        {

            IPortNameSpace task = taskReference.Task;
            return parseCreate(task, word, nameSpace, completionContext);
        }

        private static TaskEnable? parseCreate(IPortNameSpace task, WordScanner word, NameSpace nameSpace, CompletionContext? completionContext = null)
        {
            TaskEnable taskEnable = new TaskEnable();
            int portCount = 0;

            if (word.Text == "(")
            {
                word.MoveNext();

                // (EOF just after "("): hint for the first argument
                if (completionContext != null && word.Eof)
                {
                    Expressions.ListOfArguments.AppendArgumentPopupItems(completionContext, task, 0);
                    return taskEnable;
                }

                while (!word.Eof)
                {
                    Expressions.Expression? expression = null;
                    if (task == null)
                    {   // undefined task
                        expression = Expressions.Expression.ParseCreate(word, nameSpace, completionContext);
                    }
                    else if (portCount == 0 && task.PortsList.Count == 0 && word.Text == ")")
                    {   // blank ()
                        if (!word.SystemVerilog)
                        {
                            word.AddError("blank () is not acceptable for verilog");
                        }
                        break;
                    }
                    else if (portCount == task.PortsList.Count)
                    {   // next of last portcount
                        word.AddError("too many expressions");
                        return null;
                    }
                    else if (portCount > task.PortsList.Count)
                    {
                        expression = Expressions.Expression.ParseCreate(word, nameSpace, completionContext);
                    }
                    else
                    {
                        Verilog.DataObjects.Port port = task.PortsList[portCount];
                        if (port.Direction == DataObjects.Port.DirectionEnum.Input)
                        {
                            expression = Expressions.Expression.ParseCreate(word, nameSpace, completionContext);
                        }
                        else
                        {
                            expression = Expressions.Expression.ParseCreateVariableLValue(word, nameSpace, false, completionContext);
                        }
                        if (expression == null)
                        {
                            word.AddError("missed expression");
                            word.SkipToKeyword(";");
                            return null;
                        }
                    }

                    // (EOF just after an argument expression): hint for the current argument
                    if (completionContext != null && word.Eof)
                    {
                        Expressions.ListOfArguments.AppendArgumentPopupItems(completionContext, task, portCount);
                        return taskEnable;
                    }

                    if (word.Text == ")")
                    {
                        if (task != null && task.Ports.Count != portCount + 1) word.AddError("missing ports.");
                        break;
                    }
                    if (word.Text == ",")
                    {
                        // (EOF just after ","): hint for the next argument
                        if (completionContext != null && word.Eof)
                        {
                            Expressions.ListOfArguments.AppendArgumentPopupItems(completionContext, task, portCount + 1);
                            return taskEnable;
                        }
                        word.MoveNext();
                        portCount++;
                        continue;
                    }
                    else
                    {
                        word.AddError("illegal expression");
                        return null;
                    }
                }
                if (word.Text == ")") word.MoveNext();
                else word.AddError(") required");
            }
            else
            {
                if (task != null && task.Ports.Count != 0) word.AddError("missing ports.");
            }

            if (word.Text == ";")
            {
                word.MoveNext();
            }
            else word.AddError("; required");

            return taskEnable;

        }


    }
}
