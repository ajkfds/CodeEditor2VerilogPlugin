﻿using pluginVerilog.Verilog.BuildingBlocks;
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

        public void DisposeSubReference()
        {
            foreach(IStatement statement in Statements)
            {
                statement.DisposeSubReference();
            }
        }

        public List<IStatement> Statements = new List<IStatement>();

        /*
        A.6.3 Parallel and sequential blocks
        function_seq_block      ::= begin[ : block_identifier { block_item_declaration } ] { function_statement }
        end variable_assignment ::= variable_lvalue = expression
        par_block               ::= fork [ : block_identifier { block_item_declaration } ] { statement } join
        seq_block          ::= begin[ : block_identifier { block_item_declaration } ] { statement } end  
        */
        public static IStatement ParseCreate(WordScanner word, NameSpace nameSpace)
        {
            if (word.Text != "fork")
            {
                System.Diagnostics.Debugger.Break();
            }
            word.Color(CodeDrawStyle.ColorType.Keyword);
            IndexReference beginIndex = word.CreateIndexReference();
            word.MoveNext(); // begin

            if (word.GetCharAt(0) == ':')
            {
                NamedParallelBlock? namedBlock = null;
                string name = "";

                word.MoveNext(); // :
                if (!General.IsIdentifier(word.Text))
                {
                    word.AddError("illegal ifdentifier name");
                }
                else
                {
                    if (word.Prototype)
                    { // protptype
                        if (nameSpace.NamedElements.ContainsKey(word.Text))
                        {
                            word.AddError("duplicated name");
                            name = word.Text;
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
                            namedBlock = nameSpace.NamedElements[word.Text] as NamedParallelBlock;

                        }
                    }
                    word.MoveNext();
                }
                while (!word.Eof && word.Text != "join")
                {
                    IStatement? statement = null;
                    if(namedBlock == null)
                    {
                        statement = Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
                        if (statement == null) break;
                    }
                    else
                    {
                        statement = Verilog.Statements.Statements.ParseCreateStatement(word, namedBlock);
                        if (statement == null) break;
                        namedBlock.Statements.Add(statement);
                    }
                }
                if (word.Text != "join")
                {
                    word.AddError("illegal sequential block");
                    return null;
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
            else
            {
                ParallelBlock sequentialBlock = new ParallelBlock();
                while (!word.Eof && word.Text != "join")
                {
                    IStatement statement = Verilog.Statements.Statements.ParseCreateStatement(word, nameSpace);
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
