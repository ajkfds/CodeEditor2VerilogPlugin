using System;

namespace pluginVerilog.Verilog.Expressions
{


    public class TaskReference : Primary
    {
        public string TaskName { get; protected set; }
        public string ModuleName { get; protected set; }

        protected TaskReference()
        {
        }

        private System.WeakReference<IPortNameSpace> taskReferenceRef;
        public IPortNameSpace Task
        {
            get
            {
                IPortNameSpace ret;
                if (taskReferenceRef == null || !taskReferenceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                taskReferenceRef = new WeakReference<IPortNameSpace>(value);
            }
        }


        protected static new TaskReference ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            return ParseCreate(word, nameSpace, nameSpace);
        }
        public static TaskReference ParseCreate(WordScanner word, NameSpace nameSpace, NameSpace taskNameSpace)
        {
            TaskReference? ret = parseCreate(word, nameSpace, taskNameSpace);
            if(ret != null)
            {
                return ret;
            }

            ret = new TaskReference();
            ret.TaskName = word.Text;
            ret.ModuleName = nameSpace.BuildingBlock.Name;
            word.Color(CodeDrawStyle.ColorType.Keyword);

            if (!word.Prototype)
            {
                word.AddError("illegal task name");
            }
            word.MoveNext();
            return ret;
        }
        private static TaskReference? parseCreate(WordScanner word, NameSpace nameSpace, NameSpace taskNameSpace)
        {
            if (taskNameSpace.BuildingBlock.NamedElements.ContainsTask(word.Text))
            {
                TaskReference ret = new TaskReference();
                ret.TaskName = word.Text;
                ret.ModuleName = nameSpace.BuildingBlock.Name;
                word.Color(CodeDrawStyle.ColorType.Keyword);

                ret.Task = (Task)taskNameSpace.NamedElements[ret.TaskName];
                return ret;
            }
            else if (taskNameSpace.BuildingBlock.NamedElements.ContainsFunction(word.Text))
            {
                TaskReference ret = new TaskReference();
                ret.TaskName = word.Text;
                ret.ModuleName = nameSpace.BuildingBlock.Name;
                word.Color(CodeDrawStyle.ColorType.Keyword);

                Function function = (Function)taskNameSpace.NamedElements[ret.TaskName];
                if (function.ReturnVariable != null)
                {
                    word.AddError("illegal task name");
                }
                else
                {
                    ret.Task = function;
                }
                return ret;
            }

            if(taskNameSpace.Parent==null)
            {
                return null;
            }

            return parseCreate(word, nameSpace, taskNameSpace.Parent);
        }
    }

    public class NameSpaceReference : Primary
    {
        public string Name { get; protected set; }

        private System.WeakReference<NameSpace> nameSpaceRef;
        public NameSpace NameSpace
        {
            get
            {
                NameSpace ret;
                if (!nameSpaceRef.TryGetTarget(out ret)) return null;
                return ret;
            }
            protected set
            {
                nameSpaceRef = new WeakReference<NameSpace>(value);
            }
        }

        public NameSpaceReference(NameSpace nameSpace)
        {
            Name = nameSpace.Name;
            NameSpace = nameSpace;
        }
    }

    //public class ModuleInstanceReference : Primary
    //{
    //    IBuildingBlockInstantiation moduleInstantiation;
    //    public ModuleInstanceReference(IBuildingBlockInstantiation moduleInstantiation)
    //    {
    //        this.moduleInstantiation = moduleInstantiation;
    //    }
    //}

    //public class InterfaceReference : Primary
    //{
    //    public IBuildingBlockInstantiation interfaceInstantiation;
    //    public InterfaceReference(IBuildingBlockInstantiation interfaceInstantiation)
    //    {
    //        this.interfaceInstantiation = interfaceInstantiation;
    //    }
    //}


}
