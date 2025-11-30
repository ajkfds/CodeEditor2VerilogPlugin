using CodeEditor2.CodeEditor.CodeComplete;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace pluginVerilog.Verilog.Statements
{
    public class ParallelBlock : IStatement
    {
        protected ParallelBlock() { }

        public string Name { get; protected set; }
        public CodeDrawStyle.ColorType ColorType => CodeDrawStyle.ColorType.Identifier;
        public NamedElements NamedElements => new NamedElements();
        public void DisposeSubReference()
        {
            foreach(IStatement statement in Statements)
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
            while (!word.Eof && word.Text != "join")
            {
                IStatement? statement = await Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
                if (statement == null) break;
                sequentialBlock.Statements.Add(statement);
            }
            if (word.Text != "join")
            {
                word.AddError("illegal sequential block");
                return null;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            word.MoveNext(); // end

            return sequentialBlock;
        }

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
            
            while (!word.Eof && word.Text != "join")
            {
                IStatement? statement = null;
                statement = await Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
                if (statement == null) break;
                namedBlock.Statements.Add(statement);
            }

            if (word.Text != "join")
            {
                word.AddError("illegal sequential block");
                namedBlock.LastIndexReference = word.CreateIndexReference();
                return namedBlock;
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            namedBlock.LastIndexReference = word.CreateIndexReference();
            word.MoveNext(); // end

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
            foreach(IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }
        public NamedParallelBlock(BuildingBlock buildingBlock, NameSpace parent) : base(buildingBlock, parent)
        {
        }

        public List<IStatement> Statements = new List<IStatement>();
    }

}
