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

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
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
        // statement_label: "name : fork" form (label consumed by Statements.ParseCreateStatement)
        // blockIdentifier: optional pre-consumed block identifier ("name : fork" via Statements.ParseCreateStatement blockIdentifier path)
        public static IStatement? ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label, string? blockIdentifier = null)
        {
            if (word.Text != "fork") throw new Exception();
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginIndex = word.CreateIndexReference();
            word.MoveNext(); // fork

            // statement_label is treated as the block identifier (same rule as SequentialBlock)
            if (blockIdentifier == null && statement_label != null)
            {
                blockIdentifier = statement_label;
            }

            if (word.Text == ":")
            {
                return parseNamedParallelBlock(word, nameSpace, beginIndex, blockIdentifier);
            }
            else
            {
                if (blockIdentifier != null)
                {
                    return parseNamedParallelBlock(word, nameSpace, beginIndex, blockIdentifier);
                }
                else
                {
                    return parseParallelBlock(word, nameSpace, beginIndex);
                }
            }
        }

        private static ParallelBlock parseParallelBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex)
        {
            ParallelBlock sequentialBlock = new ParallelBlock() { BeginIndexReference = beginIndex };

            while (!word.Eof && !join_families.Contains(word.Text))
            {
                IStatement? statement = Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
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
            sequentialBlock.LastIndexReference = word.CreateIndexReference();
            word.MoveNext(); // end

            return sequentialBlock;
        }

        private static List<string> endKeyword = new List<string> { "endmodule", "endtask", "endinterface", "endfunction", "endclass", "endcase", "endprimitive", "else", "endpackage", "endprogram" };
        private static List<string> join_families = new List<string> { "join", "join_any", "join_none" };

        // blockIdentifier is not null for the "name : fork" form (identifier already consumed by Statements.ParseCreateStatement)
        // and null for the "fork : name" form (identifier is consumed here)
        private static IStatement parseNamedParallelBlock(WordScanner word, NameSpace nameSpace, IndexReference beginIndex, string? blockIdentifier)
        {
            NamedParallelBlock namedBlock;
            string name;

            if (blockIdentifier != null)
            { // "name : fork" form: identifier already consumed
                name = blockIdentifier;
            }
            else
            { // "fork : name" form: consume the identifier here
                word.MoveNext(); // :
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal ifdentifier name");
                    return parseParallelBlock(word, nameSpace, beginIndex);
                }
                name = word.Text;
            }

            if (word.Prototype)
            { // protptype
                if (nameSpace.NamedElements.ContainsKey(name))
                {
                    word.AddError("duplicated name");
                    if (blockIdentifier == null) word.MoveNext();
                    return parseParallelBlock(word, nameSpace, beginIndex);
                }
                else
                {
                    namedBlock = new NamedParallelBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = name,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            else
            { // implementation
                if (nameSpace.NamedElements.ContainsKey(name) && nameSpace.NamedElements[name] is NamedParallelBlock)
                {
                    word.Color(CodeDrawStyle.ColorType.Identifier);
                    namedBlock = (NamedParallelBlock)nameSpace.NamedElements[name];
                }
                else
                {
                    namedBlock = new NamedParallelBlock(nameSpace.BuildingBlock, nameSpace)
                    {
                        BeginIndexReference = beginIndex,
                        DefinitionReference = word.CrateWordReference(),
                        Name = name,
                        Parent = nameSpace,
                        Project = word.Project
                    };
                    nameSpace.NamedElements.Add(namedBlock.Name, namedBlock);
                }
            }
            if (blockIdentifier == null) word.MoveNext();

            while (!word.Eof && !join_families.Contains(word.Text))
            {
                IStatement? statement = null;
                statement = Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
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

            // register as document region for autocomplete / hint partial parse (same rule as NamedSequentialBlock)
            if (!word.Prototype && word.CompletionContext == null) nameSpace.DocumentRegions.Add(namedBlock);

            return namedBlock;

        }
    }

    public class NamedParallelBlock : Verilog.NameSpace, IStatement, Items.IDocumentRegeion
    {
        // BeginIndexReference / LastIndexReference are inherited from NameSpace,
        // which satisfies IStatement / Items.IDocumentRegeion requirements
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

        public required IndexReference BeginIndexReference { get; init; }
        public IndexReference? LastIndexReference { get; set; } = null;
        public string Name { get; protected set; } = "disable fork";
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();

        public static DisableForkStatement ParseCreate(WordScanner word, NameSpace nameSpace, string? statement_label)
        {
            if (word.Text != "disable") return null;
            if (word.NextText != "fork") return null;

            DisableForkStatement statement = new DisableForkStatement() { BeginIndexReference = word.CreateIndexReference() };
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // disable

            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // fork

            // Semicolon
            statement.LastIndexReference = word.CreateIndexReferenceBefore();
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
