using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.Statements
{
    /// <summary>
    /// Join variant types for parallel blocks
    /// </summary>
    public enum JoinVariant
    {
        Join,       // join - wait for all processes to complete
        JoinAny,    // join_any - wait for at least one process to complete
        JoinNone    // join_none - don't wait, continue immediately
    }

    public class ParallelBlock : IStatement
    {
        protected ParallelBlock() { }

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            foreach (IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }
        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
                );
        }

        public List<IStatement> Statements = new List<IStatement>();

        /// <summary>
        /// Join variant type
        /// </summary>
        public JoinVariant JoinType { get; set; } = JoinVariant.Join;

        /*
        A.6.3 Parallel and sequential blocks
        function_seq_block      ::= begin[ : block_identifier { block_item_declaration } ] { function_statement }
        end variable_assignment ::= variable_lvalue = expression
        par_block               ::= fork [ : block_identifier { block_item_declaration } ] { statement } join
        seq_block          ::= begin[ : block_identifier { block_item_declaration } ] { statement } end  
        */
        public static async Task<IStatement?> ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "fork") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginIndex = word.CreateIndexReference();
            word.MoveNext(); // begin

            if (word.Text == ":")
            {
                return await parseNamedParallelBlock(word, nameSpace, beginIndex);
            }
            else
            {
                return await parseParallelBlock(word, nameSpace, beginIndex);
            }
        }

        private static async Task<ParallelBlock> parseParallelBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex)
        {
            ParallelBlock sequentialBlock = new ParallelBlock();

            while (!word.Eof && !join_families.Contains(word.Text))
            {
                IStatement? statement = await Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
                if (statement == null) break;
                sequentialBlock.Statements.Add(statement);
            }
            if (!join_families.Contains(word.Text))
            {
                word.AddError("illegal sequential block");
                return null;
            }

            // Store the join variant type
            switch (word.Text)
            {
                case "join":
                    sequentialBlock.JoinType = JoinVariant.Join;
                    break;
                case "join_any":
                    sequentialBlock.JoinType = JoinVariant.JoinAny;
                    break;
                case "join_none":
                    sequentialBlock.JoinType = JoinVariant.JoinNone;
                    break;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // end

            return sequentialBlock;
        }

        private static List<string> endKeyword = new List<string> { "endmodule", "endtask", "endtask", "endinterface", "endfunction" };
        private static List<string> join_families = new List<string> { "join", "join_any", "join_none" };

        private static async Task<IStatement> parseNamedParallelBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex)
        {
            NamedParallelBlock namedBlock;
            string name = "";

            word.MoveNext(); // :
            if (!General.IsIdentifier(word.Text))
            {
                word.AddError("illegal ifdentifier name");
                return await parseParallelBlock(word, nameSpace, beginIndex);
            }

            if (word.Prototype)
            { // protptype
                if (nameSpace.NamedElements.ContainsKey(word.Text))
                {
                    word.AddError("duplicated name");
                    name = word.Text;
                    word.MoveNext();
                    return await parseParallelBlock(word, nameSpace, beginIndex);
                }
                else
                {
                    namedBlock = new NamedParallelBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = word.Text,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            else
            { // implementation
                if (nameSpace.NamedElements.ContainsKey(word.Text) && nameSpace.NamedElements[word.Text] is NamedParallelBlock)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    namedBlock = (NamedParallelBlock)nameSpace.NamedElements[word.Text];
                }
                else
                {
                    namedBlock = new NamedParallelBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = word.Text,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            word.MoveNext();

            while (!word.Eof && !join_families.Contains(word.Text))
            {
                IStatement? statement = null;
                statement = await Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
                if (statement == null) break;
                namedBlock.Statements.Add(statement);
            }

            if (!join_families.Contains(word.Text))
            {
                word.AddError("illegal sequential block");
                namedBlock.LastIndexReference = word.CreateIndexReference();
                return namedBlock;
            }

            // Store the join variant type
            switch (word.Text)
            {
                case "join":
                    namedBlock.JoinType = JoinVariant.Join;
                    break;
                case "join_any":
                    namedBlock.JoinType = JoinVariant.JoinAny;
                    break;
                case "join_none":
                    namedBlock.JoinType = JoinVariant.JoinNone;
                    break;
            }

            word.Color(CodeDrawStyle.ColorType.Keyword);
            namedBlock.LastIndexReference = word.CreateIndexReference();
            word.MoveNext(); // join/join_any/join_none

            if (word.Text == ":")
            {
                word.MoveNext();
                if (endKeyword.Contains(word.Text) || word.Text == "end")
                {
                    word.AddError("block name required");
                }
                else if (namedBlock.Name != word.Text)
                {
                    word.AddError("illegal block name");
                }
                else
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    word.MoveNext();
                }
            }

            if (word.Active && namedBlock.Name != null && !nameSpace.NamedElements.ContainsKey(namedBlock.Name))
            {
                nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
            }

            return namedBlock;

        }
    }

    public class NamedParallelBlock : Verilog.NameSpace, IStatement
    {
        public void DisposeSubReference()
        {
            foreach (IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }
        public NamedParallelBlock(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }

        public List<IStatement> Statements = new List<IStatement>();

        /// <summary>
        /// Join variant type
        /// </summary>
        public JoinVariant JoinType { get; set; } = JoinVariant.Join;
    }

    /// <summary>
    /// Disable Fork Statement
    /// disable_statement ::= disable hierarchical_task_identifier ;
    ///                      | disable fork ;
    /// 
    /// The disable fork statement terminates all active processes that were spawned by 
    /// fork...join_none blocks in the current thread's context.
    /// </summary>
    public class DisableForkStatement : IStatement
    {
        protected DisableForkStatement() { }

        public string Name { get; protected set; } = "disable fork";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public static DisableForkStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "disable") return null;
            if (word.NextText != "fork") return null;

            DisableForkStatement statement = new DisableForkStatement();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // disable

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // fork

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

            return statement;
        }

        public void DisposeSubReference()
        {
        }

        public AutocompleteItem CreateAutoCompleteItem()
        {
            return new AutocompleteItem(
                Name,
                CodeDrawStyle.ColorIndex(ColorType),
                Global.CodeDrawStyle.Color(ColorType),
                "CodeEditor2/Assets/Icons/tag.svg"
            );
        }
    }
}
