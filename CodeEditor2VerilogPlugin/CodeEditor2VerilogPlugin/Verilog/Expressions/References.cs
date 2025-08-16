using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pluginVerilog.Verilog.ModuleItems;

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
                if(taskReferenceRef == null || !taskReferenceRef.TryGetTarget(out ret)) return null;
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
            TaskReference ret = new TaskReference();
            ret.TaskName = word.Text;
            ret.ModuleName = nameSpace.BuildingBlock.Name;
            word.Color(CodeDrawStyle.ColorType.Keyword);
            if (taskNameSpace.BuildingBlock.NamedElements.ContainsTask(ret.TaskName))
            {
                ret.Task = (Task)taskNameSpace.BuildingBlock.NamedElements[ret.TaskName];
            }else if (taskNameSpace.BuildingBlock.NamedElements.ContainsFunction(ret.TaskName))
            {
                Function function = (Function)taskNameSpace.BuildingBlock.NamedElements[ret.TaskName];
                if(function.ReturnVariable != null)
                {
                    word.AddError("illegal task name");
                }
                else
                {
                    ret.Task = function;
                }
            }
            else if(!word.Prototype)
            {
                word.AddError("illegal task name");
            }
            word.MoveNext();

            return ret;
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
