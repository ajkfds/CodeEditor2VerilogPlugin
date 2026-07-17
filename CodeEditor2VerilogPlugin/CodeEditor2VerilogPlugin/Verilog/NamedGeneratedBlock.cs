using System.Collections.Generic;

namespace pluginVerilog.Verilog
{
    public class NamedGeneratedBlock : NameSpace
    {
        private Dictionary<string, Function> functions = new Dictionary<string, Function>();
        private Dictionary<string, Task_> tasks = new Dictionary<string, Task_>();
        private Dictionary<string, Items.ModuleInstantiation> moduleInstantiations = new Dictionary<string, Items.ModuleInstantiation>();
        public Dictionary<string, Function> Functions { get { return functions; } }
        public Dictionary<string, Task_> Tasks { get { return tasks; } }
        public Dictionary<string, Items.ModuleInstantiation> ModuleInstantiations { get { return moduleInstantiations; } }

        protected NamedGeneratedBlock(NameSpace parent) : base(parent.BuildingBlock, parent)
        {
        }

        public static NamedGeneratedBlock Create(WordScanner word, NameSpace parent, IndexReference beginReference)
        {
            NamedGeneratedBlock block = new NamedGeneratedBlock(parent)
            {
                BeginIndexReference = beginReference,
                DefinitionReference = word.CrateWordReference(),
                Name = word.Text,
                Parent = parent,
                Project = word.Project
            };
            return block;
        }

    }
}
